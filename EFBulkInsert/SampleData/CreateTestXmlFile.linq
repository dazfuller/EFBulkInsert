<Query Kind="Program">
  <Namespace>System.Xml.Serialization</Namespace>
</Query>

void Main()
{
	const int size = 10000;
	var range = Enumerable.Range(0, size);
	var list = new List<Example>(size);
	foreach (var i in range)
	{
		list.Add(new Example { Id = i, Description = String.Format("This is record {0}", i), LastModified = DateTime.UtcNow });
	}
	
	using (var textWriter = new XmlTextWriter("C:\\test.xml", Encoding.UTF8))
	{
		textWriter.Formatting = Formatting.Indented;
		textWriter.Indentation = 2;
		
		var serializer = new XmlSerializer(typeof (List<Example>));
		serializer.Serialize(textWriter, list);
	}
}

// Define other methods and classes here
[Serializable]
public class Example
{
	public int Id { get; set; }
	public string Description { get; set; }
	public DateTime LastModified { get; set; }
	
	public override string ToString()
	{
		return String.Format("{0}: \"{1}\": Last Modified {2}", Id, Description, LastModified);
	}
}