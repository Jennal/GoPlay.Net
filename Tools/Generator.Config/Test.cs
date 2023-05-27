using DotLiquid;

namespace GoPlay.Generators.Config;

[LiquidType("Name")]
public class MyLiquidTypeWithAllowedMember
{
    public string Name { get; set; }
}

[LiquidType("*")]
public class MyLiquidTypeWithGlobalMemberAllowance
{
    public string Name { get; set; }
}

[LiquidType("*")]
public class MyLiquidTypeWithGlobalMemberAllowanceAndExposedChild
{
    public string Name { get; set; }
    public MyLiquidTypeWithAllowedMember Child { get; set; }

    public List<MyLiquidTypeWithGlobalMemberAllowance> Children { get; set; }
}

public class Test
{
    public static void DoTest()
    {
        Template template = Template.Parse(@"|{{context.Name}}|{{context.Child.Name}}|
{%for child in context.Children -%}{{child.Name}}{%endfor -%}");
        var output = template.Render(Hash.FromAnonymousObject(new { context = new MyLiquidTypeWithGlobalMemberAllowanceAndExposedChild() { Name = "worked_parent", Child = new MyLiquidTypeWithAllowedMember() { Name = "worked_child" }, Children = new List<MyLiquidTypeWithGlobalMemberAllowance>
        {
            new MyLiquidTypeWithGlobalMemberAllowance() { Name = "list_child" }
        }} }));
        Console.WriteLine(output);
    }
}