using System.Text;
namespace Game
{
    class Program
    {
        private const int ScreenWidth = 180;
        private const int ScreenHeight = 60;

        private const int MapWidth = 32;
        private const int MapHeight = 32;

        private const double Fov = Math.PI / 3;
        private const double Depth = 16;

        private static double _playerX = 2;
        private static double _playerY = 2;
        private static double _playerA = 0;

        private static readonly StringBuilder Map = new StringBuilder();

        private static readonly char[] Screen = new char[ScreenWidth * ScreenHeight];
        static async Task Main(string[] args)
        {
            Console.SetWindowSize(ScreenWidth, ScreenHeight + 1);
            Console.SetBufferSize(ScreenWidth, ScreenHeight + 1);
            Console.CursorVisible = false;

            /*for (int i = 0; i < MapWidth; i++)
            {
                for (int j = 0; j < MapHeight; j++)
                {
                    if (i == 0 || i == MapWidth - 1 || j == 0 || j == MapHeight - 1)
                        _map += "#";
                    else
                        _map += ".";
                }
            }*/


            DateTime dateTimeFrom = DateTime.Now;

            while (true)
            {
                InitMap();
                DateTime dateTimeTo = DateTime.Now;
                double elapsedTime = (dateTimeTo - dateTimeFrom).TotalSeconds;
                dateTimeFrom = DateTime.Now;

                if (Console.KeyAvailable)
                {
                    ConsoleKey consoleKey = Console.ReadKey(true).Key;

                    switch (consoleKey)
                    {
                        case ConsoleKey.A:
                            _playerA += 6 * elapsedTime;
                            break;
                        case ConsoleKey.D:
                            _playerA -= 6 * elapsedTime;
                            break;
                        case ConsoleKey.W:
                            {
                                _playerX += Math.Sin(_playerA) * elapsedTime * 20;
                                _playerY += Math.Cos(_playerA) * elapsedTime * 20;

                                if (Map[(int)_playerX * MapWidth + (int)_playerX] == '#')
                                {
                                    _playerX -= Math.Sin(_playerA) * elapsedTime * 20;
                                    _playerY -= Math.Cos(_playerA) * elapsedTime * 20;
                                }
                                break;
                            }
                        case ConsoleKey.S:
                            {
                                _playerX -= Math.Sin(_playerA) * elapsedTime * 20;
                                _playerY -= Math.Cos(_playerA) * elapsedTime * 20;

                                if (Map[(int)_playerX * MapWidth + (int)_playerX] == '#')
                                {
                                    _playerX += Math.Sin(_playerA) * elapsedTime * 20;
                                    _playerY += Math.Cos(_playerA) * elapsedTime * 20;
                                }
                                break;
                            }
                    }
                }
                //ray casting

                var rayCastingTasks = new List<Task<Dictionary<int, char>>>();

                for (int x = 0; x < ScreenWidth; x++)
                {
                    int x1 = x;
                    rayCastingTasks.Add(Task.Run(() => CastRay(x1)));
                }

                Dictionary<int, char>[] rays = await Task.WhenAll(rayCastingTasks);

                foreach (Dictionary<int, char> dictionary in rays)
                {
                    foreach (int key in dictionary.Keys)
                    {
                        Screen[key] = dictionary[key];
                    }
                }

                //stats
                char[] stats = $"X: {_playerX}, Y: {_playerY}, A: {_playerA}, FPS: {(int)(1/elapsedTime)}"
                    .ToCharArray();
                stats.CopyTo( Screen, 0 );

                //map
                for(int x = 0; x < MapWidth; x++)
                {
                    for(int y = 0; y < MapHeight; y++)
                    {
                        Screen[(y + 1) * ScreenWidth + x] = Map[y * MapWidth + x];
                    }
                }

                //player
                Screen[(int)(_playerY + 1) * ScreenWidth + (int)_playerX] = 'P';

                Console.SetCursorPosition(0, 0);
                Console.Write(Screen);
            }
            
        }

        public static Dictionary<int, char> CastRay(int x)
        {
            var result = new Dictionary<int, char>();
            double rayAngle = _playerA + Fov / 2 - x * Fov / ScreenWidth;

            double rayX = Math.Sin(rayAngle);
            double rayY = Math.Cos(rayAngle);

            double distanceToWall = 0;
            bool hitWall = false;
            bool isBound = false;

            while (!hitWall && distanceToWall < Depth)
            {
                distanceToWall += 0.1;

                int testX = (int)(_playerX + rayX * distanceToWall);
                int testY = (int)(_playerY + rayY * distanceToWall);

                if (testX < 0 || testX >= Depth + _playerX || testY < 0 || testY >= Depth + _playerY)
                {
                    hitWall = true;
                    distanceToWall = Depth;
                }
                else
                {
                    char testCell = Map[testY * MapWidth + testX];

                    if (testCell == '#')
                    {
                        hitWall = true;

                        var boundsVectorList = new List<(double module, double cos)>();

                        for (int tX = 0; tX < 2; tX++)
                        {
                            for (int tY = 0; tY < 2; tY++)
                            {
                                double vX = testX + tX + _playerX;
                                double vY = testY + tY + _playerY;

                                double vectorModule = Math.Sqrt(vX * vX + vY * vY);
                                double cosAngle = rayX * vX / vectorModule + rayY * vY / vectorModule;

                                boundsVectorList.Add((vectorModule, cosAngle));
                            }
                        }

                        boundsVectorList = boundsVectorList.OrderBy(v => v.module).ToList();

                        double boundAngle = 0.03 / distanceToWall;

                        if (Math.Acos(boundsVectorList[0].cos) < boundAngle
                            || Math.Acos(boundsVectorList[1].cos) < boundAngle)
                            isBound = true;
                    }
                    else
                    {
                        Map[testY * MapWidth + testX] = '*';
                    }
                }
            }

            int ceilling = (int)(ScreenHeight / 2d - ScreenHeight * Fov / distanceToWall);
            int floor = ScreenHeight - ceilling;

            char wallShade;

            if (isBound)
                wallShade = '|';
            else if (distanceToWall <= Depth / 4d)
                wallShade = '\u2588';
            else if (distanceToWall < Depth / 3d)
                wallShade = '\u2593';
            else if (distanceToWall < Depth / 2d)
                wallShade = '\u2592';
            else if (distanceToWall < Depth)
                wallShade = '\u2591';
            else
                wallShade = ' ';

            for (int y = 0; y < ScreenHeight; y++)
            {
                if (y <= ceilling)
                {
                    result[y * ScreenWidth + x] = ' ';
                }
                else if (y > ceilling && y <= floor)
                {
                    result[y * ScreenWidth + x] = wallShade;
                }
                else
                {
                    char floorShade;

                    double b = 1 - (y - ScreenHeight / 2d) / (ScreenHeight / 2d);

                    if (b < 0.25)
                        floorShade = '#';
                    else if (b < 0.5)
                        floorShade = 'x';
                    else if (b < 0.75)
                        floorShade = '-';
                    else if (b < 1)
                        floorShade = '.';
                    else
                        floorShade = ' ';

                    result[y * ScreenWidth + x] = floorShade;
                }
            }
            return result;
        }

        private static void InitMap()
        {
            Map.Clear();
            Map.Append("################################");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#............##........#########");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#................#");
            Map.Append("#.............#######......#####");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("##############........##########");
            Map.Append("#..............................#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#................#.............#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("#...........#########..........#");
            Map.Append("#..............................#");
            Map.Append("#..............................#");
            Map.Append("################################");
        }
    }
}
