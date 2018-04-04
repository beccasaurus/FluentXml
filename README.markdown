FluentXml
=========

This is just a tiny set of extension methods that I've been copy/pasting into lots of my projects lately.

I decided that it was finally time to separate these extension methods out into their own file/library and write specs for them.

Now that I have this tiny library, I will add more tests and functionality to it, as needed.

What is it?
-----------

I write lots of jQuery.  I'm used to being able to say things like `$('ul#nav li a').attr('foo', 'bar')` 
to set the `foo` attribute equal to `bar` for every link inside of an `<li>` inside of the `<ul id="nav">` element.

When I work with a `System.Xml.XmlDocument`, I want to be able to use a similar chainable (fluent) interface.

How to use it
-------------

Sample XML:

```xml
<?xml version="1.0" encoding="utf-8"?>
<dogs>
  <dog name="Rover" breed="Golden Retriever">
    <toys>
      <toy>Tennis Ball</toy>
      <toy>Kong</toy>
    </toys>
  </dog>
  <dog name="Snoopy" breed="Beagle">
    <toys>
      <toy>Charlie Brown's Football</toy>
    </toys>
  </dog>
</dogs>
```

Sample usage:

```cs
using FluentXml;

// Note, you do NOT need to use FluentXmlDocument ... this all works with a regular XmlDocument.
var doc = FluentXmlDocument.FromFile("dogs.xml");

>> doc.Node("dog").Attr("name");
"Rover"

>> doc.Node("dog toy").Text();      
"Tennis Ball"

>> doc.Nodes("dog")[0].Attrs();     
{{ "name", "Rover" }, { "breed", "Golden Retriever" }}

>> doc.Nodes("dog")[1].Attrs(); 
{{ "name", "Snoopy" }, { "breed", "Beagle" }}

>> doc.ToXml();
"<?xml version=\"1.0\" encoding=\"utf-8\"?>
<dogs>
  <dog name=\"Rover\" breed=\"Golden Retriever\">
    <toys>
      <toy>Tennis Ball</toy>
      <toy>Kong</toy>
    </toys>
  </dog>
  <dog name=\"Snoopy\" breed=\"Beagle\">
    <toys>
      <toy>Charlie Brown's Football</toy>
    </toys>
  </dog>
</dogs>"

// Passing a second argument to Attr() sets the attribute value (just like with jQuery)
doc.Node("dog").Attr("name", "Changed!");

>> doc.ToXml();                              
"<?xml version=\"1.0\" encoding=\"utf-8\"?>
<dogs>
  <dog name=\"Changed!\" breed=\"Golden Retriever\">
    <toys>
      <toy>Tennis Ball</toy>
      <toy>Kong</toy>
    </toys>
  </dog>
  <dog name=\"Snoopy\" breed=\"Beagle\">
    <toys>
      <toy>Charlie Brown's Football</toy>
    </toys>
  </dog>
</dogs>"

// Passing an argument to Text() sets the text of this node (just like with jQuery)
doc.Node("toy").Text("new toy text");

>> doc.ToXml();                          
"<?xml version=\"1.0\" encoding=\"utf-8\"?>
<dogs>
  <dog name=\"Changed!\" breed=\"Golden Retriever\">
    <toys>
      <toy>new toy text</toy>
      <toy>Kong</toy>
    </toys>
  </dog>
  <dog name=\"Snoopy\" breed=\"Beagle\">
    <toys>
      <toy>Charlie Brown's Football</toy>
    </toys>
  </dog>
</dogs>"

// You can also pass in a lambda to Node() or Nodes() to find a node that matches some arbitrary condition
>> doc.Node(n => n.Attr("name") != null && n.Attr("name").StartsWith("S")).Attr("name");
"Snoopy"
```

That's all?
-----------

Pretty much, yeah.  It has lots of safe null handling so I can safely say `Node("foo").Node("bar").Node("whatever")` and not worry about nulls.

To see more, just have a look at the source.  It's a tiny thing, but ... it works for me!

License
-------

FluentXml is released under the MIT license.
