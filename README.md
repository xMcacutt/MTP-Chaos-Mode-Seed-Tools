# Mul-Ty-Player Chaos Mode Seed Tools

This is a command line application designed to allow players to find information about seeds in Mul-Ty-Player's "Chaos Mode".

## Usage

Since this is a command line application, it must be run from a command line. 
Each usage starts with a single keyword argument. Here are the options for keyword arguments:

### range
Typing "range" as the first keyword will allow you to log the basic metrics (near, far, range) for every seed in the specified range.
```
CHMSeedFinder range <starting_seed> <ending_seed>
```
You can also provide an optional logging interval which determines how often the code will report its progress through the seeds.
```
CHMSeedFinder range <starting_seed> <ending_seed> <logging_interval>
```

### seed
Typing "seed" as the first keyword will allow you to log the advanced metrics of a specific seed.
Advanced metrics include the nearest two, furthest two, and average between each pair of collectibles for each level.
```
CHMSeedFinder seed <seed>
```

### best
Typing "best" as the first keyword will allow you to scan through seeds to find the seed with the best average
```
CHMSeedFinder best <starting_seed> <ending_seed>
```
Alternatively, you can provide a threshold which will be used to test for good seeds. Any seeds with an average below the threshold will be logged.
```
CHMSeedFinder best <threshold> <starting_seed> <ending_seed>
```

### plot 
Typing "plot" as the first keyword will allow you to create an image showing where collectibles would be moved to in a given seed and level.
```
CHMSeedFinder plot <seed> <level_number>
```

### stats
Typing "stats" as the first keyword will allow you to create advanced metrics for every level from basic metrics.
To do this, you must have a basic metrics CSV already generated.
```
CHMSeedFinder stats <input_csv> <output_csv>
```

### level
Typing "level" as the first keyword will allow you to get basic metrics for a specific level across a range of seeds.
```
CHMSeedFinder level <starting_seed> <ending_seed> <logging_interval>
```
```
