namespace LemonExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var processor = new Lemon.Processor(Log);
            processor.ProcessDebugSymbols = true;
            processor.AddLookUpDirectories("");
            processor.AddTargetAssemblies();

            processor.Process(p =>
            {
                p.log("Started");

                p.log("Finished");
            });

            processor.WriteAssembliesAndDispose();
        }

        private static void Log(string obj)
        {
            Console.WriteLine(obj);
        }
    }
}
