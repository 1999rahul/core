namespace oops
{
    public class Program
    {
        static void Main(string[] args)
        {

            var program = new Program();

            int a = 10;
            program.AddTen(ref a);

            int x;
            int y;
            program.GetValues(out x, out y);

        }

        void GetValues(out int min, out int max)
        {
            min = 10;   // MUST assign — out guarantees a value comes back
            max = 100;
        }

        void AddTen(ref int number)
        {
            number = number + 10;  // modifies the ORIGINAL — not a copy
        }
    }
}
