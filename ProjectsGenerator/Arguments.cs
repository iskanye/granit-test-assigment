namespace ProjectsGenerator;

public struct Arguments
{
    public readonly string Path = "";
    public readonly int ProjectsNum = 20;

    public Arguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                case "--path":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value for --path");
                    Path = args[++i];
                    break;
                case "-n":
                case "--num":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("Missing value for --num");
                    if (!int.TryParse(args[++i], out int n))
                        throw new ArgumentException("--num must be an integer");
                    ProjectsNum = n;
                    break;
                default:
                    throw new ArgumentException("Unknown argument: " + args[i]);
            }
        }

        if (Path == "")
            throw new ArgumentException("--path must be declared");
    }
}