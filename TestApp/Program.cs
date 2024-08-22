namespace TestApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var csvs = System.IO.Directory.EnumerateFiles(
                @"C:\1\Norsyn\AC - Iso\PipeSpecs\", "*.csv", System.IO.SearchOption.TopDirectoryOnly);

            foreach (var csv in csvs) 
                Console.WriteLine(csv);
        }
    }
}
