using System;
using System.IO;
using System.Linq;
using SystemLogin.Core;

namespace SystemLogin.RobotScripts;

public static class SorterColorScriptBuilder
{
    public static string Build(SorterColorOptions options)
    {
        var path = RobotScriptCatalog.SorterColorPath;
        if (!File.Exists(path))
            throw new FileNotFoundException("SorterColor URScript not found.", path);

        var script = File.ReadAllText(path);

        script = ReplaceBoolean(script, "SortBlueToOrder", options.SortBlueToOrder);
        script = ReplaceBoolean(script, "SortRedToOrder", options.SortRedToOrder);
        script = ReplaceBoolean(script, "SortGreenToOrder", options.SortGreenToOrder);
        script = ReplaceBoolean(script, "SortYellowToOrder", options.SortYellowToOrder);

        script = ReplaceInt(script, "BlueRemaining", options.BlueRemaining);
        script = ReplaceInt(script, "RedRemaining", options.RedRemaining);
        script = ReplaceInt(script, "GreenRemaining", options.GreenRemaining);
        script = ReplaceInt(script, "YellowRemaining", options.YellowRemaining);

        if (!string.IsNullOrWhiteSpace(options.OrderDropPoseOverride))
            script = ReplacePose(script, "OrderDropPose", options.OrderDropPoseOverride!);

        if (!string.IsNullOrWhiteSpace(options.ResortDropPoseOverride))
            script = ReplacePose(script, "ResortDropPose", options.ResortDropPoseOverride!);

        return script + Environment.NewLine;
    }

    private static string ReplaceInt(string script, string variableName, int value)
    {
        return ReplaceValue(script, variableName, value.ToString());
    }

    private static string ReplaceBoolean(string script, string variableName, bool value)
    {
        var newValue = value ? "True" : "False";
        return ReplaceValue(script, variableName, newValue);
    }

    private static string ReplacePose(string script, string variableName, string poseExpression)
    {
        return ReplaceValue(script, variableName, poseExpression);
    }

    private static string ReplaceValue(string script, string variableName, string newValue)
    {
        var token = $"global {variableName}=";
        var index = script.IndexOf(token, StringComparison.Ordinal);
        if (index < 0)
            throw new InvalidOperationException($"Variable '{variableName}' not found in script.");

        var endOfLine = script.IndexOf('\n', index);
        if (endOfLine < 0) endOfLine = script.Length;

        var startValue = index + token.Length;
        return script[..startValue] + newValue + script[endOfLine..];
    }
}

public sealed record SorterColorOptions(
    bool SortBlueToOrder,
    bool SortRedToOrder,
    bool SortGreenToOrder,
    bool SortYellowToOrder,
    int BlueRemaining,
    int RedRemaining,
    int GreenRemaining,
    int YellowRemaining
    )
{
    public string? OrderDropPoseOverride { get; init; }
    public string? ResortDropPoseOverride { get; init; }

    public static SorterColorOptions FromOrder(SortingOrder order)
    {
        int Qty(BlockColor c) => order.Items.Where(i => i.Color == c).Sum(i => i.Quantity);

        var blue = Qty(BlockColor.Blue);
        var red = Qty(BlockColor.Red);
        var green = Qty(BlockColor.Green);
        var yellow = Qty(BlockColor.Yellow);

        return new SorterColorOptions(
            blue > 0, red > 0, green > 0, yellow > 0,
            blue, red, green, yellow);
    }
}
