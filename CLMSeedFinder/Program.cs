// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using CLMSeedFinder;
using Newtonsoft.Json;
using ScottPlot;
using Formatting = System.Xml.Formatting;

public class Program
{
    private static ChaosHandler _hChaos;
    
    public static void Main(string[] args)
    {
        _hChaos = new ChaosHandler();

        var loggingIntervalString = "";
        switch (args[0])
        {
            case "range":
                loggingIntervalString = args.Length == 4 ? args[3] : "1000";
                CalculateMetricsForRangeOfSeeds(args[1], args[2], loggingIntervalString);
                break;
            case "seed":
                GetAdvancedMetrics(args[1]);
                break;
            case "best":
                if (args.Length == 3)
                    FindBest(args[1], args[2]);
                if (args.Length == 4)
                    FindBest(args[1], args[2], args[3]);
                break;
            case "plot":
                if (args.Length == 3)
                    PlotCollectibles(args[1], args[2]);
                break;
            case "stats":
                if (args.Length == 3)
                    CreateBreakdown(args[1], args[2]);
                break;
            case "level":
                loggingIntervalString = args.Length == 5 ? args[4] : "1000";
                GetLevelMetricsForRangeOfSeeds(args[1], args[2], args[3], loggingIntervalString);
                break;
        }
    }


    private static Dictionary<int, string> _levels = new Dictionary<int, string>()
    {
        { 4, "Two Up" },
        { 5, "Walk in the Park" },
        { 6, "Ship Rex" },
        { 8, "Bridge on the River Ty" },
        { 9, "Snow Worries" },
        { 10, "Outback Safari" },
        { 12, "Lyre, Lyre Pants on Fire" },
        { 13, "Beyond the Black Stump" },
        { 14, "Rex Marks the Spot" },
    };
    
    private static Dictionary<int, string> _levelCodes = new Dictionary<int, string>()
    {
        { 4, "A1" },
        { 5, "A2" },
        { 6, "A3" },
        { 8, "B1" },
        { 9, "B2" },
        { 10, "B3" },
        { 12, "C1" },
        { 13, "C2" },
        { 14, "C3" },
    };

    private static void GetAdvancedMetrics(string seedString)
    {
        var seed = int.Parse(seedString);
        using var writer = new StreamWriter("chaos_data_" + seedString + ".txt");
        foreach (var levelId in _hChaos.MainStages)
        {
            var metrics = CalculateMetricsForLevel(seed, levelId);
            writer.WriteLine(_levels[levelId] + ":");
            writer.Write("   Nearest: " + metrics.nearest + "\n");
            writer.Write("  Furthest: " + metrics.furthest + "\n");
            writer.Write("   Average: " + metrics.average + "\n");
            writer.WriteLine();
        }
    }
    
    private static void PlotCollectibles(string seedString, string levelIdString)
    {
        var seed = int.Parse(seedString);
        var levelId = int.Parse(levelIdString);

        _hChaos.ChaosSeed = seed;
        var levelIndices = _hChaos.CurrentPositionIndices[levelId];
        var positions = _hChaos.GetPositions(levelId, levelIndices);

        // Create a plot with ScottPlot
        var plt = new ScottPlot.Plot();

        var image = new Image("./Images/Cog.png");
        for (var cogIndex = 0; cogIndex < 10; cogIndex++)
            plt.Add.ImageMarker(new Coordinates(positions[cogIndex].X, positions[cogIndex].Z), image);
        image = new Image("./Images/Bilby.png");
        for (var bilbyIndex = 10; bilbyIndex < 15; bilbyIndex++)
            plt.Add.ImageMarker(new Coordinates(positions[bilbyIndex].X, positions[bilbyIndex].Z), image);

        plt.Title($"Collectible Positions for Seed {seed} on Level {_levels[levelId]}");
        plt.XLabel("X Coordinate");
        plt.YLabel("Z Coordinate");
        plt.Axes.Bottom.Label.Text = "Horizontal Axis";
        plt.Axes.Left.Label.Text = "Vertical Axis";
        plt.Axes.Title.Label.Text = "Plot Title";
        plt.Grid.MajorLineColor = Color.FromHex("#0e3d54");
        plt.FigureBackground.Color = Color.FromHex("#07263b");
        plt.DataBackground.Color = Color.FromHex("#0b3049");
        plt.Axes.Color(Color.FromHex("#a0acb5"));

        // Save the plot as a PNG image
        string fileName = $"collectibles_plot_seed_{seed}_level_{levelId}.png";
        plt.SavePng(fileName, 1000, 1000);
        Console.WriteLine($"Plot saved as {fileName}");
    }

    private static void CreateBreakdown(string inputPath, string outputPath)
    {
        Console.WriteLine("Starting...");
        // Read all lines from the input CSV file
        var seeds = new List<int>();
        using (var reader = new StreamReader(inputPath))
        {
            // Skip header
            reader.ReadLine();
            while (reader.ReadLine() is { } line)
            {
                var values = line.Split(',');
                seeds.Add(int.Parse(values[0])); // Assuming the first column is the Seed
            }
        }

        // Prepare to write to the output CSV file
        using (var writer = new StreamWriter(outputPath))
        {
            // Write the CSV header
            var headerColumns = new List<string> { "Seed" };
            foreach (var levelId in _hChaos.MainStages)
            {
                headerColumns.Add($"Nearest {_levelCodes[levelId]}");
                headerColumns.Add($"Furthest {_levelCodes[levelId]}");
                headerColumns.Add($"Average {_levelCodes[levelId]}");
            }
            headerColumns.Add("Overall Nearest");
            headerColumns.Add("Overall Furthest");
            headerColumns.Add("Overall Average");
            writer.WriteLine(string.Join(",", headerColumns));

            // Process each seed
            foreach (var seed in seeds)
            {
                var metricsList = new List<(float nearest, float furthest, float average)>();
                foreach (var levelId in _hChaos.MainStages)
                {
                    var metrics = CalculateMetricsForLevel(seed, levelId);
                    metricsList.Add(metrics);
                }

                // Calculate overall averages
                double overallNearest = metricsList.Average(m => m.nearest);
                double overallFurthest = metricsList.Average(m => m.furthest);
                double overallAverage = metricsList.Average(m => m.average);

                // Write the row for the current seed
                var rowColumns = new List<string> { seed.ToString() };
                foreach (var metrics in metricsList)
                {
                    rowColumns.Add(metrics.nearest.ToString(CultureInfo.InvariantCulture));
                    rowColumns.Add(metrics.furthest.ToString(CultureInfo.InvariantCulture));
                    rowColumns.Add(metrics.average.ToString(CultureInfo.InvariantCulture));
                }
                rowColumns.Add(overallNearest.ToString(CultureInfo.InvariantCulture));
                rowColumns.Add(overallFurthest.ToString(CultureInfo.InvariantCulture));
                rowColumns.Add(overallAverage.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine(string.Join(",", rowColumns));
            }
        }
    }
    
    private static void FindBest(string startSeedString = "0", string endSeedString = "2147483647")
    {
        var startSeed = int.Parse(startSeedString);
        var endSeed = int.Parse(endSeedString);
        // Open or create a CSV file to write the logs
        float? bestAverage = null;
        using (StreamWriter writer = new StreamWriter("chaos_data.csv"))
        {
            // Write the header line
            writer.WriteLine("Seed,Closest,Furthest,Average");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var seed = startSeed; seed < endSeed; seed++)
            {
                if (seed % 100000 == 0)
                {
                    Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds / 1000f);
                    Console.WriteLine($"Reached Seed: {seed}");
                }
                var metrics = CalculateMetrics(seed);
                if (!(metrics.average < bestAverage) && bestAverage is not null) 
                    continue;
                bestAverage = metrics.average;
                writer.WriteLine($"{seed},{metrics.nearest},{metrics.furthest},{metrics.average}");
                Console.WriteLine("Current Best: " + seed);
            }
            Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds / 1000f);
            stopwatch.Stop();
        }
    }

    private static void FindBest(string thresholdString, string startSeedString = "0", string endSeedString = "2147483647")
    {
        var startSeed = int.Parse(startSeedString);
        var endSeed = int.Parse(endSeedString);
        var threshold = float.Parse(thresholdString);
        using (StreamWriter writer = new StreamWriter("chaos_data.csv"))
        {
            // Write the header line
            writer.WriteLine("Seed,Closest,Furthest,Average");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var seed = startSeed; seed < endSeed; seed++)
            {
                if (seed % 100000 == 0)
                {
                    Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds / 1000f);
                    Console.WriteLine($"Reached Seed: {seed}");
                }
                var metrics = CalculateMetrics(seed);
                if (!(metrics.average < threshold)) 
                    continue;
                writer.WriteLine($"{seed},{metrics.nearest},{metrics.furthest},{metrics.average}");
                Console.WriteLine("Seed Found: " + seed);
            }
            Console.WriteLine("Time Elapsed: " + stopwatch.ElapsedMilliseconds / 1000f);
            stopwatch.Stop();
        }
    }
    
    private static void GetLevelMetricsForRangeOfSeeds(string levelIdString, string startSeedString, string endSeedString, string loggingInvtervalString = "1000")
    {
        var levelId = int.Parse(levelIdString);
        var startSeed = int.Parse(startSeedString);
        var endSeed = int.Parse(endSeedString);
        var loggingInterval = int.Parse(loggingInvtervalString);
        
        // Open or create a CSV file to write the logs
        using (StreamWriter writer = new StreamWriter($"chaos_data_for_{_levelCodes[levelId]}.csv"))
        {
            // Write the header line
            writer.WriteLine($"Seed,Closest for {_levelCodes[levelId]},Furthest for {_levelCodes[levelId]},Average for {_levelCodes[levelId]}");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var seed = startSeed; seed < endSeed; seed++)
            {
                if (seed % loggingInterval == 0)
                    Console.WriteLine($"Calculating Metrics for Seed: {seed}");
                var metrics = CalculateMetricsForLevel(seed, levelId);
                writer.WriteLine($"{seed},{metrics.nearest},{metrics.furthest},{metrics.average}");
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000f);
            stopwatch.Stop();
        }
    }

    public static void CalculateMetricsForRangeOfSeeds(string startSeedString, string endSeedString, string loggingInvtervalString = "1000")
    {
        var startSeed = int.Parse(startSeedString);
        var endSeed = int.Parse(endSeedString);
        var loggingInterval = int.Parse(loggingInvtervalString);
        // Open or create a CSV file to write the logs
        using (StreamWriter writer = new StreamWriter("chaos_data.csv"))
        {
            // Write the header line
            writer.WriteLine("Seed,Closest,Furthest,Average");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var seed = startSeed; seed < endSeed; seed++)
            {
                if (seed % loggingInterval == 0)
                    Console.WriteLine($"Calculating Metrics for Seed: {seed}");
                var metrics = CalculateMetrics(seed);
                writer.WriteLine($"{seed},{metrics.nearest},{metrics.furthest},{metrics.average}");
            }
            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000f);
            stopwatch.Stop();
        }
    }

    public static (float nearest, float furthest, float average) CalculateMetrics(int seed)
    {
        _hChaos.ChaosSeed = seed;

        Span<float> furthestArray = stackalloc float[9];
        Span<float> nearestArray = stackalloc float[9];
        Span<float> averageArray = stackalloc float[9];
        int levelCounter = 0;
        
        foreach (var level in _hChaos.CurrentPositionIndices.Keys)
        {
            var levelIndices = _hChaos.CurrentPositionIndices[level];
            var positions = _hChaos.GetPositions(level, levelIndices);

            (float furthest, float nearest, float average) = CalculateDistanceMetrics(positions);

            nearestArray[levelCounter] = nearest;
            furthestArray[levelCounter] = furthest;
            averageArray[levelCounter] = average;
            levelCounter++;
        }
        
        // Calculate averages manually to avoid LINQ overhead
        float nearestAvg = 0, furthestAvg = 0, averageAvg = 0;
        for (int i = 0; i < levelCounter; i++)
        {
            nearestAvg += nearestArray[i];
            furthestAvg += furthestArray[i];
            averageAvg += averageArray[i];
        }
        nearestAvg /= levelCounter;
        furthestAvg /= levelCounter;
        averageAvg /= levelCounter;
        
        return (nearestAvg, furthestAvg, averageAvg);
    }
    
    public static (float nearest, float furthest, float average) CalculateMetricsForLevel(int seed, int levelId)
    {
        _hChaos.ChaosSeed = seed;
        var levelIndices = _hChaos.CurrentPositionIndices[levelId];
        var positions = _hChaos.GetPositions(levelId, levelIndices);
        (var furthest, var nearest, var average) = CalculateDistanceMetrics(positions);
        return (nearest, furthest, average);
    }

    public static float CalculateDistance(PositionData pos1, PositionData pos2)
    {
        float dx = pos2.X - pos1.X;
        float dy = pos2.Y - pos1.Y;
        float dz = pos2.Z - pos1.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static (float furthest, float nearest, float average) CalculateDistanceMetrics(PositionData[] positions)
    {
        float furthest = 0f;
        float nearest = float.MaxValue;
        float totalDistance = 0f;
        int count = 0;
        int length = positions.Length;

        for (int i = 0; i < length - 1; i++)
        {
            for (int j = i + 1; j < length; j++)
            {
                float distance = CalculateDistance(positions[i], positions[j]);
                totalDistance += distance;
                count++;

                if (distance > furthest)
                    furthest = distance;

                if (distance < nearest)
                    nearest = distance;
            }
        }

        float average = totalDistance / count;
        return (furthest, nearest, average);
    }

}

