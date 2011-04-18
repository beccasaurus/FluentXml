using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>The FluentXml namespace.  Use this to get the FluentXml extension methods.</summary>
namespace FluentXml {

	public delegate bool XmlNodeMatcher(XmlNode node);

	/// <summary>Non-stupid XmlResolver.  Doesn't try to auto parse URIs found in the XML and EXPLODE if the parse fails.</summary>
	/// <remarks>
	/// See StackOverflow post:
	///   http://stackoverflow.com/questions/2899254/xdocument-parse-fails-due-to-resolution-error-how-to-disable-resolution
	/// </remarks>
	class UriFluentXmlResolver : XmlResolver {
		public override Uri ResolveUri (Uri baseUri, string relativeUri){ return baseUri; }
		public override object GetEntity (Uri absoluteUri, string role, Type type){ return null; }
		public override ICredentials Credentials { set {} }
	}   

	/// <summary>Helper methods for getting an XmlDocument from a string or from a file.</summary>
	/// <remarks>
	/// There is NOTHING special about getting an XmlDocument from FluentXmlDocument versus 
	/// instantiating and loading an XmlDocument yourself.
	///
	/// The only difference is that we use our own XmlResolver, by default, that doesn't 
	/// try to resolve URIs because it's pretty darned annoying with your XML parser explodes 
	/// because it tried to parse something that it thinks *should* be a URI.
	/// </remarks>
	public static class FluentXmlDocument {

		/// <summary>Get an XmlDocument from the given XML string.  By default, we use our UriFluentXmlResolver.</summary>
		/// <remarks>If the given string is null, we return null</remarks>
		public static XmlDocument FromString(string xml) {
			return FromString(xml, false);
		}

		/// <summary>Get an XmlDocument from the given XML string.  Specify whether or not you want to resolve URIs.</summary>
		public static XmlDocument FromString(string xml, bool resolveUris) {
			if (string.IsNullOrEmpty(xml)) return null;
			var doc    = new XmlDocument();
			var reader = new XmlTextReader(new StringReader(xml));

			if (! resolveUris)
				reader.XmlResolver = new UriFluentXmlResolver();

			doc.Load(reader);
			return doc;
		}

		/// <summary>Get an XmlDocument from the given XML file path.  By default, we use our UriFluentXmlResolver.</summary>
		/// <remarks>If the given path is null or does not exist, we return null</remarks>
		public static XmlDocument FromFile(string path) {
			return FromFile(path, false);
		}

		/// <summary>Get an XmlDocument from the given XML file path.  Specify whether or not you want to resolve URIs.</summary>
		public static XmlDocument FromFile(string path, bool resolveUris) {
			if (string.IsNullOrEmpty(path)) return null;
			if (! File.Exists(path))        return null;

			return FromString(File.ReadAllText(path), resolveUris);
		}
	}

	/// <summary>The main Xml extensions that make up FluentXml.  Mostly extensions on XmlDocument and XmlNode.</summary>
	public static class XmlExtensions {

		/// <summary>Returns the first XmlNode found with the given tag name</summary>
		/// <remarks>
		/// If the provided tag name has spaces, we process each part as a Node.
		///
		/// Node("foo bar") is the same as Node("foo").Node("bar")
		/// </remarks>
		public static XmlNode Node(this XmlNode node, string tag) {
			if (node == null) return null;

			if (tag.Contains(" "))
				return node.Node(tag.Split(' '));
			else
				return node.Nodes(tag).FirstOrDefault(); // TODO update this do it doesn't have to find ALL nodes!
		}

		/// <summary>Node("foo", "bar") returns the first "bar" found under a "foo"</summary>
		public static XmlNode Node(this XmlNode node, params string[] tags) {
			if (node == null)     return null;
			if (tags.Length == 0) return null;
			if (tags.Length == 1) return node.Nodes(tags).FirstOrDefault();
			var tagList = tags.ToList();
			var theTag  = tagList.Last();
			tagList.RemoveAt(tagList.Count - 1);
			return node.Nodes(theTag).Where(n => n.HasParentNodes(tagList.ToArray())).FirstOrDefault();
		}

		/// <summary>Returns whether or not the given node has parents with the tags provided (currently, ORDER MATTERS!)</summary>
		public static bool HasParentNodes(this XmlNode node, params string[] tags) {
			if (node == null) return false;
			var actualParentTags = new List<string>();
			var parent = node.ParentNode;
			while (parent != null) {
				actualParentTags.Insert(0, parent.Name);
				parent = parent.ParentNode;
			}
			var tagsAsString = string.Join(" ", actualParentTags.ToArray()).ToLower(); // gives us "first second third ..."
			var regexToMatch = string.Join(".*", tags).ToLower();                      // gives us "first.*third"
			return Regex.IsMatch(tagsAsString, regexToMatch);
		}

		/// <summary>Returns first node that match the given matcher (Func that should return true if it matches)</summary>
		public static XmlNode Node(this XmlNode node, XmlNodeMatcher matcher) {
			if (node == null) return null;
			return node.Nodes(matcher).FirstOrDefault();
		}

		/// <summary>Returns all of the ChildNodes under this node</summary>
		public static List<XmlNode> Nodes(this XmlNode node) {
			var nodes = new List<XmlNode>();
			if (node == null) return nodes;
			foreach (XmlNode child in node.ChildNodes)
				nodes.Add(child);
			return nodes;
		}

		/// <summary>Returns all of the nodes under this node that match the given tag.  Recursively searches children.</summary>
		public static List<XmlNode> Nodes(this XmlNode node, string tag) {
			if (tag.Contains(" "))
				return node.Nodes(tag.Split(' '));
			else
				return node.Nodes(n => n.Name.ToLower() == tag.ToLower());
		} 	

		/// <summary>Get all of the nodes with the given tag, underneath each previous tag ... eg. Nodes("body", "ul", "li", "a")</summary>
		public static List<XmlNode> Nodes(this XmlNode node, params string[] tags) {
			var nodes = new List<XmlNode>();
			if (node == null || tags.Length == 0) return nodes;

			// Get the nodes for the first tag
			nodes = node.Nodes(tags[0]);

			// If only 1 tag was passed, don't keep searching, just return the nodes for the first tag
			if (tags.Length == 1) return nodes;

			// Start loop at 1, because we already looked for the first tag
			for (int i = 1; i < tags.Length; i++) {
				var innerResults = new List<XmlNode>();

				// for each of the nodes that we've found so far, look under it for nodes that match the next tag
				foreach (var previouslyFoundNode in nodes)
					innerResults.AddRange(previouslyFoundNode.Nodes(tags[i]));

				// overwrite our existing results with the nodes we just found
				nodes = innerResults;
			}

			return nodes;
		}

		/// <summary>Returns all nodes (searches recursively) that match the given matcher (Func that should return true if it matches)</summary>
		public static List<XmlNode> Nodes(this XmlNode node, XmlNodeMatcher matcher) {
			return node.Nodes(matcher, false);
		}

		/// <summary>Returns a List of XmlNode matching the given matcher.  If onlyFindOne is true, we stop looking after we find 1 match.</summary>
		public static List<XmlNode> Nodes(this XmlNode node, XmlNodeMatcher matcher, bool onlyFindOne) {
			var nodes = new List<XmlNode>();
			if (node == null) return nodes;
			foreach (XmlNode child in node.ChildNodes) {
				if (matcher.Invoke(child)) {
					nodes.Add(child);
					if (onlyFindOne) break;
				}

				nodes.AddRange(child.Nodes(matcher));

				if (onlyFindOne && nodes.Count > 0) break;
			}
			
			if (onlyFindOne && nodes.Count > 1)
				return new List<XmlNode> { nodes.First() };
			else
				return nodes;
		}

		/// <summary>If a node with this tag exists, we return it, else we create a new node with this tag and create it</summary>
		public static XmlNode NodeOrNew(this XmlNode node, string tag) {
			if (node == null) return null;
			return node.Node(tag) ?? node.NewNode(tag);
		}

		/// <summary>Creates and returns a new node with the given tag name as a child of the XmlNode we call this on</summary>
		public static XmlNode NewNode(this XmlNode node, string tag) {
			if (node == null) return null;

			// look for a NamespaceURI ... if you CreateElement without setting this properly, 
			// you can end up with ugly blank xmlns attributes ...
			string xmlns = null;
			var parent = node;
			while (parent != null) {
				if (! string.IsNullOrEmpty(parent.NamespaceURI)) {
					xmlns = parent.NamespaceURI;
					break;
				} else
					parent = parent.ParentNode;
			}

			XmlNode child;

			if (xmlns == null)
				child = node.OwnerDocument.CreateElement(tag);
			else
				child = node.OwnerDocument.CreateElement(tag, xmlns);

			node.AppendChild(child);
			return child;
		}

		/// <summary>Returns the InnerText of this node (or null)</summary>
		public static string Text(this XmlNode node) {
			if (node == null) return null;
			return node.InnerText;
		}

		/// <summary>If this XmlNode exists, sets its InnerText to the provided value</summary>
		public static XmlNode Text(this XmlNode node, string value) {
			if (node != null) node.InnerText = value;
			return node;
		}

		/// <summary>Returns a dictionary of this node's attribute names and values</summary>
		/// <remarks>
		/// The returned dictionary can NOT be used to modify these attributes.
		///
		/// See Attr(string, string) to modify an attribute.
		/// </remarks>
		public static IDictionary<string,string> Attrs(this XmlNode node) {
			var attrs = new Dictionary<string,string>();
			if (node == null) return attrs;
			foreach (XmlAttribute attr in node.Attributes)
				attrs.Add(attr.Name, attr.Value);
			return attrs;
		}

		/// <summary>If this node exists and has an attribute with the given name, returns the value of the attribute</summary>
		public static string Attr(this XmlNode node, string attr) {
			if (node == null)                  return null;
			if (node.Attributes == null)       return null;
			if (node.Attributes[attr] == null) return null;
			return node.Attributes[attr].Value;
		}

		/// <summary>Sets the value of an attribute on this node.  If the attribute exists, we update it, else we create a new attribute.</summary>
		public static XmlNode Attr(this XmlNode node, string attr, string value) {
			if (node == null) return null;
			var attribute = node.Attributes[attr];
			if (attribute == null) {
				attribute = node.OwnerDocument.CreateAttribute(attr);
				node.Attributes.Append(attribute);
			}
			attribute.Value = value;
			return node;
		}

		/// <summary>Writes the current XmlDocument out to a string using our default settings (eg. indented)</summary>
		public static string ToXml(this XmlDocument doc) {
			return doc.ToXml(true);
		}

		/// <summary>The most common option that we want to toggle is indentation.  This lets you do that easily.</summary>
		public static string ToXml(this XmlDocument doc, bool indent) {
			return doc.ToXml(new XmlWriterSettings { Indent = true });
		}

		/// <summary>This ToXml takes the most low-level options ... if this doesn't work for you, then do it yourself!</summary>
		public static string ToXml(this XmlDocument doc, XmlWriterSettings settings) {
			if (doc == null) return null;

			// TODO move all of this into a common method that SaveToFile() can use too
			var stream = new MemoryStream();
			var writer = XmlWriter.Create(stream, settings);
			doc.WriteTo(writer);
			writer.Flush();
			var buffer = stream.ToArray();
			var xml    = System.Text.Encoding.UTF8.GetString(buffer).Trim();

			return xml;
		}

		/// <summary>Saves this XmlDocument to a file.  You can just use XmlDocument.Save, but this setup up indentation and whatnot.</summary>
		/// <remarks>
		/// Right now this is stupid and inefficient ... we just write doc.ToXml() to our file.  But ... meh ... it works.
		/// </remarks>
		public static void SaveToFile(this XmlDocument doc, string path) {
			if (doc == null) return;
			using (var writer = new StreamWriter(path)) writer.Write(doc.ToXml());
		}
	}
}
