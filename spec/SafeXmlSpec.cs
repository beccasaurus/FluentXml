using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SafeXml;

namespace SafeXml.Specs {

	// little extension for fixing the xml we use to verify in our tests ... fixes the indentation etc ...
	public static class FixXmlExtension {
		public static string FixXml(this string str) {
			return Regex.Replace(str, @"^\t\t\t\t", "", RegexOptions.Multiline).TrimStart('\n').Replace("'", "\"");
		}
	}

	// If we need to split this into separate specs, we will, but let's just start with 1 basic spec ...
	[TestFixture]
	public class SafeXmlSpec : Spec {

		XmlDocument ExampleCsprojDoc = SafeXmlDocument.FromFile(Example("ConsoleApp.csproj"));

		[Test]
		public void can_easily_get_an_XmlDocument_for_a_given_string() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander' /><dog name='Murdoch' /></dogs>");
			doc.GetElementsByTagName("dogs").Count.ShouldEqual(1);
			doc.GetElementsByTagName("dog").Count.ShouldEqual(2);
		}

		[Test]
		public void can_easily_get_an_XmlDocument_for_a_given_file() {
			var doc = SafeXmlDocument.FromFile(Example("ConsoleApp.csproj"));
			doc.GetElementsByTagName("Project").Count.ShouldEqual(1);
			doc.GetElementsByTagName("PropertyGroup").Count.ShouldEqual(7);
		}

		[Test]
		public void can_get_Node_under_XmlDocument() {
			var doc = ExampleCsprojDoc;
			doc.Node("DoesntExist").Should(Be.Null);

			var project = doc.Node("Project");
			project.ShouldNot(Be.Null);
			// project.Attrs().ShouldEqual(new Dictionary<string,string> {
			// 	{},
			// 	{},
			// 	{}		
			// });

			var group = doc.Node("PropertyGroup");
			group.ShouldNot(Be.Null);
			// group.Attrs().Should(Be.Empty); // grabbed the first node, which has no attributes
		}

		[Test]
		public void can_get_Nodes_under_XmlDocument() {
			var doc = ExampleCsprojDoc;
			doc.Nodes("DoesntExist").Should(Be.Empty);
			doc.Nodes("Project").Count.ShouldEqual(1);
			doc.Nodes("PropertyGroup").Count.ShouldEqual(7);
		}

		[Test]
		public void can_get_Node_under_XmlNode() {
			var doc = ExampleCsprojDoc;
			var group = doc.Node("PropertyGroup");
			group.Node("RootNamespace").Text().ShouldEqual("ConsoleApplication2");
		}

		[Test]
		public void can_get_Nodes_under_XmlNode() {
			var doc = ExampleCsprojDoc;
			doc.Node("Project").Nodes("PropertyGroup").Count.ShouldEqual(7);
			doc.Node("PropertyGroup").Nodes().Count.ShouldEqual(12);
		}

		[Test]
		public void can_get_attribute_value() {
			(null as XmlNode).Attr("Foo").Should(Be.Null);

			ExampleCsprojDoc.Node("Project").Attr("ToolsVersion").ShouldEqual("4.0");
			ExampleCsprojDoc.Node("Project").Attr("DefaultTargets").ShouldEqual("Build");

			ExampleCsprojDoc.Nodes("PropertyGroup")[0].Attr("Condition").Should(Be.Null);
			ExampleCsprojDoc.Nodes("PropertyGroup")[1].Attr("Condition").ShouldEqual(" '$(Configuration)|$(Platform)' == 'Debug|x86' ");
		}

		[Test]
		public void can_get_a_dictionary_of_all_attribute_values() {
			(null as XmlNode).Attrs().Should(Be.Empty);

			ExampleCsprojDoc.Node("Project").Attrs().ShouldEqual(new Dictionary<string,string>{
				{ "ToolsVersion",   "4.0"   },
				{ "DefaultTargets", "Build" },
				{ "xmlns",          "http://schemas.microsoft.com/developer/msbuild/2003" }
			});
		}

		[Test]
		public void can_set_attribute_values() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander'>My Text</dog></dogs>");

			// Modify existing attribute
			doc.Node("dog").Attr("name").ShouldEqual("Lander");
			doc.Node("dog").Attr("name", "Different name");
			doc.Node("dog").Attr("name").ShouldEqual("Different name");
			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Different name'>My Text</dog>
				</dogs>".FixXml());

			// Add new attribute
			doc.Node("dog").Attr("breed").Should(Be.Null);
			doc.Node("dog").Attr("breed", "Golden Retriever");
			doc.Node("dog").Attr("breed").ShouldEqual("Golden Retriever");
			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Different name' breed='Golden Retriever'>My Text</dog>
				</dogs>".FixXml());
		}

		[Test]
		public void can_get_node_text() {
			ExampleCsprojDoc.Node("PropertyGroup RootNamespace").Text().ShouldEqual("ConsoleApplication2");
		}

		[Test]
		public void can_set_node_text() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander'>My Text</dog></dogs>");
			doc.Node("dog").Text().ShouldEqual("My Text");
			doc.Node("dog").Text("changed!");
			doc.Node("dog").Text().ShouldEqual("changed!");

			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Lander'>changed!</dog>
				</dogs>".FixXml());
		}

		[Test]
		public void can_find_or_create_a_node_by_tag_name() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander' /></dogs>");

			doc.Node("dog").Node("breed").Should(Be.Null);

			// Node doesn't exist, so it'll use NewNode
			doc.Node("dog").NodeOrNew("breed").Text("hello world");
			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Lander'>
				    <breed>hello world</breed>
				  </dog>
				</dogs>".FixXml());

			// Node doesn't exist, so we just return it
			doc.Node("dog").NodeOrNew("breed").Text("changed!");
			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Lander'>
				    <breed>changed!</breed>
				  </dog>
				</dogs>".FixXml());
		}

		[Test]
		public void can_call_ToXml_to_get_XML_text_for_XmlDocument() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander'>My Text</dog></dogs>");

			doc.ToXml().ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Lander'>My Text</dog>
				</dogs>".FixXml());
		}

		[Test]
		public void can_save_to_file() {
			var doc = SafeXmlDocument.FromString("<dogs><dog name='Lander'>My Text</dog></dogs>");

			File.Exists(Temp("Foo.xml")).Should(Be.False);
			doc.SaveToFile(Temp("Foo.xml"));
			File.Exists(Temp("Foo.xml")).Should(Be.True);

			File.ReadAllText(Temp("Foo.xml")).ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Lander'>My Text</dog>
				</dogs>".FixXml());

			// can overwrite ...
			doc.Node("dog").Attr("name", "Different");
			doc.SaveToFile(Temp("Foo.xml"));
			File.ReadAllText(Temp("Foo.xml")).ShouldEqual(@"
				<?xml version='1.0' encoding='utf-8'?>
				<dogs>
				  <dog name='Different'>My Text</dog>
				</dogs>".FixXml());
		}

	// TODO

		[Test][Ignore]
		public void can_get_a_new_XmlDocument_that_doesnt_try_to_parse_URIs() {
			// TODO reproduce an exception as described in:
			//      http://stackoverflow.com/questions/2899254/xdocument-parse-fails-due-to-resolution-error-how-to-disable-resolution
			//
			// I used to get this exception constantly, but I can't remember how ... 
			//
			// Once we can figure it out, we'll write this test!
		}
	}
}
