namespace LemonExample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var processor = new Lemon.Processor(Log);
            processor.Process();
        }

        private static void Log(string obj)
        {
            Console.WriteLine(obj);
        }
    }
}
