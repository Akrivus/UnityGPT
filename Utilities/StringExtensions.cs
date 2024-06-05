
public static class StringExtensions
{
    public static string Format(this string template, params string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == null)
                continue;
            template = template.Replace("{" + i + "}", args[i]);
        }
        return template;
    }

    public static string Format(this string template, string text, params string[] arg)
    {
        var args = new string[arg.Length + 1];
        args[0] = text;
        for (int i = 1; i < args.Length; i++)
            args[i] = arg[i - 1];
        return template.Format(args);
    }
}
