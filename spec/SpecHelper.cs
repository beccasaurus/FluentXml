using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;

namespace SafeXml.Specs {

	/// <summary>Global Before and After Hooks for all SafeXml.Specs</summary>
	[SetUpFixture]
	public class SpecsSetup {

		[SetUp]
		public void BeforeAll() {
			// ...
		}

		[TearDown]
		public void AfterAll() {
			// ...
		}
	}

	/// <summary>Base class for our specs ... for helper methods and whatnot</summary>
	public class Spec {

		[SetUp]
		public void BeforeEach() {
			if (Directory.Exists(TempRoot)) Directory.Delete(TempRoot, true);
			Directory.CreateDirectory(TempRoot);
		}

		public string ProjectRoot  { get { return Path.Combine(Directory.GetCurrentDirectory(), "..", ".."); } }
		public string ExamplesRoot { get { return Path.Combine(ProjectRoot, "spec", "content", "examples");  } }
		public string TempRoot     { get { return Path.Combine(ProjectRoot, "spec", "content", "tmp");       } }

		public string Example(params string[] parts) {
			var allParts = new List<string>(parts);
			allParts.Insert(0, ExamplesRoot);
			return Path.GetFullPath(Path.Combine(allParts.ToArray()));
		}

		public string Temp(params string[] parts) {
			var allParts = new List<string>(parts);
			allParts.Insert(0, TempRoot);
			return Path.GetFullPath(Path.Combine(allParts.ToArray()));
		}
	}
}