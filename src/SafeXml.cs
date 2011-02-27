using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Linq;
using System.Collections.Generic;

/// <summary>The SafeXml namespace.  Use this to get the SafeXml extension methods.</summary>
namespace SafeXml {

	/// <summary>Non-stupid XmlResolver.  Doesn't try to auto parse URIs found in the XML and EXPLODE if the parse fails.</summary>
	/// <remarks>
	/// See StackOverflow post:
	///   http://stackoverflow.com/questions/2899254/xdocument-parse-fails-due-to-resolution-error-how-to-disable-resolution
	/// </remarks>
	class UriSafeXmlResolver : XmlResolver {
		public override Uri ResolveUri (Uri baseUri, string relativeUri){ return baseUri; }
		public override object GetEntity (Uri absoluteUri, string role, Type type){ return null; }
		public override ICredentials Credentials { set {} }
	}   

	/// <summary>Helper methods for getting an XmlDocument from a string or from a file.</summary>
	/// <remarks>
	/// There is NOTHING special about getting an XmlDocument from SafeXmlDocument versus 
	/// instantiating and loading an XmlDocument yourself.
	///
	/// The only difference is that we use our own XmlResolver, by default, that doesn't 
	/// try to resolve URIs because it's pretty darned annoying with your XML parser explodes 
	/// because it tried to parse something that it thinks *should* be a URI.
	/// </remarks>
	public static class SafeXmlDocument {

		/// <summary>Get an XmlDocument from the given XML string.  By default, we use our UriSafeXmlResolver.</summary>
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
				reader.XmlResolver = new UriSafeXmlResolver();

			doc.Load(reader);
			return doc;
		}

		/// <summary>Get an XmlDocument from the given XML file path.  By default, we use our UriSafeXmlResolver.</summary>
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

	/// <summary>The main Xml extensions that make up SafeXml.  Mostly extensions on XmlDocument and XmlNode.</summary>
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

			var tags = node.Nodes(tag);
			return (tags != null && tags.Count > 0) ? tags[0] : null;
		}

		/// <summary>Node("foo", "bar") is a shortcut for calling Node("foo").Node("bar")</summary>
		public static XmlNode Node(this XmlNode node, params string[] tags) {
			if (node == null) return null;
			XmlNode result = node;
			foreach (var tag in tags)
				result = result.Node(tag);
			return result;
		}

		/// <summary>Returns all of the ChildNodes under this node</summary>
        public static List<XmlNode> Nodes(this XmlNode node) {
            var nodes = new List<XmlNode>();
			if (node == null) return nodes;
            foreach (XmlNode child in node.ChildNodes)
				nodes.Add(child);
			return nodes;
		}

		// TODO provide a way to pass an arbitrary matcher lambda
		/// <summary>Returns all of the nodes under this node that match the given tag.  Recursively searches children.</summary>
        public static List<XmlNode> Nodes(this XmlNode node, string tag) {
            var nodes = new List<XmlNode>();
			if (node == null) return nodes;
            foreach (XmlNode child in node.ChildNodes) {
                nodes.AddRange(child.Nodes(tag));
                if (child.Name.ToLower() == tag.ToLower())
                    nodes.Add(child);
            }
            return nodes;
        } 	

		// // Will find or create the tag
		// public static XmlNode NodeOrNew(this XmlNode node, string tag) {
		// 	if (node == null) return null;
		// 	return node.Node(tag) ?? node.NewNode(tag);
		// }

		// public static XmlNode NewNode(this XmlNode node, string tag) {
		// 	if (node == null) return null;
		// 	var child = node.OwnerDocument.CreateElement(tag);
		// 	node.AppendChild(child);
		// 	return child;
		// }

		public static string Text(this XmlNode node) {
			if (node == null) return null;
			return node.InnerText;
		}

		// public static XmlNode Text(this XmlNode node, string value) {
		// 	if (node != null) node.InnerText = value;
		// 	return node;
		// }

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

		public static string Attr(this XmlNode node, string attr) {
			if (node == null)                       return null;
			if (node.Attributes[attr] == null) return null;
			return node.Attributes[attr].Value;
		}

		// public static XmlNode Attr(this XmlNode node, string attr, string value) {
		// 	if (node == null) return null;
		// 	var attribute = node.Attributes[attr];
		// 	if (attribute == null) {
		// 		attribute = node.OwnerDocument.CreateAttribute(attr);
		// 		node.Attributes.Append(attribute);
		// 	}
		// 	attribute.Value = value;
		// 	return node;
		// }

		// public static string ToXml(this XmlDocument doc) {
		// 	if (doc == null) return null;

		// 	var stream = new MemoryStream();
		// 	var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true });
		// 	doc.WriteTo(writer);
		// 	writer.Flush();
		// 	var buffer = stream.ToArray();
		// 	var xml    = System.Text.Encoding.UTF8.GetString(buffer).Trim();

		// 	return xml;
		// }
	}
}
