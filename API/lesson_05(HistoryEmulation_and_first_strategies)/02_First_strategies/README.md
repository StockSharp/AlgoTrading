# Historical Testing of Trading Strategies with StockSharp

## Overview

The process involves setting up a test harness that loads historical data, initializes each of the trading strategies with this data, and runs simulations to see how each strategy would have performed. This is crucial for validating trading logic before deployment in a live trading environment.

## Key Components

1. **Historical Data Loader**: Responsible for fetching and providing historical candle data for the securities of interest.
2. **Simulation Environment Setup**: Includes configuring a backtesting engine that uses historical data to mimic real-time trading.
3. **Strategy Configuration**: Each strategy is initialized and configured for the test.
4. **Performance Metrics**: Collecting and analyzing trading statistics and performance metrics to evaluate each strategy.

## Detailed Setup and Execution

### 1. Historical Data Preparation

Assuming you have access to historical candle data stored locally or can retrieve it from a database:

```csharp
var storageRegistry = new StorageRegistry
{
    DefaultDrive = new LocalMarketDataDrive(Paths.HistoryDataPath)
};

var secId = "SBER@TQBR".ToSecurityId();
var candleSeries = new CandleSeries(typeof(TimeFrameCandle), secId, TimeSpan.FromMinutes(1));
var candles = storageRegistry.GetCandleStorage(candleSeries.CandleType, secId, candleSeries.Arg, candleSeries.Drive, StorageFormats.Binary)
                             .Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);
```

### 2. Strategy Initialization

Create and configure instances of each strategy:

```csharp
var oneCandleCountertrend = new OneCandleCountertrend(candleSeries);
var oneCandleTrend = new OneCandleTrend(candleSeries);
var stairsCountertrend = new StairsCountertrend(candleSeries) { Length = 3 };
var stairsTrend = new StairsTrend(candleSeries) { Length = 3 };
```

### 3. Backtesting Engine Configuration

Set up a backtesting engine to run these strategies using historical data:

```csharp
var start = Paths.HistoryBeginDate;
var end = Paths.HistoryEndDate;

var emulator = new HistoryEmulationConnector(candles.Select(c => c.Security), new[] { new Portfolio { Name = "backtest", BeginValue = 100000 } })
{
    HistoryMessageAdapter =
    {
        StorageRegistry = storageRegistry,
        StorageFormat = StorageFormats.Binary,
        StartDate = start,
        StopDate = end
    }
};

emulator.Strategies.Add(oneCandleCountertrend);
emulator.Strategies.Add(oneCandleTrend);
emulator.Strategies.Add(stairsCountertrend);
emulator.Strategies.Add(stairsTrend);
```

### 4. Running the Test

Execute the backtesting process and collect results:

```csharp
emulator.Start();

// Wait for the completion or handle asynchronously
emulator.Stop();
```

### 5. Performance Evaluation

After the simulation, gather and analyze the trading logs, performance metrics, and other statistical data:

```csharp
var stats = emulator.GetStatistics();
Console.WriteLine("Strategy performance metrics:");
foreach (var stat in stats)
{
    Console.WriteLine($"{stat.Key} : {stat.Value}");
}
```

## Conclusion

This setup provides a comprehensive method for testing multiple trading strategies against historical data using StockSharp’s trading and simulation tools. It helps identify the strengths and weaknesses of each strategy under various market conditions without financial risk. The insights gained from these tests are invaluable for refining the strategies, adjusting parameters, or scrapping ineffective trading rules.