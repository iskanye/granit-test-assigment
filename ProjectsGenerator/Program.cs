namespace ProjectsGenerator;

public static class Program
{
    private static Arguments _args;

    public static async Task<int> Main(string[] args)
    {
        try
        {
            _args = new Arguments(args);
            var tasks = new Task[_args.ProjectsNum];

            for (int i = 0; i < _args.ProjectsNum; i++)
            {
                var project = new Project(_args.Path, i.ToString(), i.ToString());
                tasks[i] = project.Save();
            }

            Task.WaitAll(tasks);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return 1;
        }

        return 0;
    }
}