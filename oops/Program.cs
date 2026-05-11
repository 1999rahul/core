namespace oops
{
    public class Program
    {
        static void Main(string[] args)
        {
            int[] asteroids = { 5, 10, -5 };
            int [] res = AsteroidCollision(asteroids);

            foreach (int i in res)
            {
                Console.WriteLine(i);
            }

        }


        public static int[] AsteroidCollision(int[] asteroids)
        {
            var stack = new Stack<int>();

            foreach (var asteroid in asteroids)
            {
                if (asteroid > 0)
                {
                    stack.Push(asteroid);
                }
                else
                {
                    bool shouldPush = true;
                    while (true)
                    {
                        if (stack.Count > 0 && stack.Peek() > 0 && stack.First() < Math.Abs(asteroid))
                        {
                            stack.Pop();
                        }
                        else
                        {
                            shouldPush = false;
                            break;
                        }
                    }

                    if (shouldPush)
                    {
                        stack.Push(asteroid);
                    }
                }
            }

            return stack.Reverse().ToArray();
        }
    }
}
