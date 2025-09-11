using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replays trades from a CSV description.
/// Each line describes a trade with side, quantity, price and execution time.
/// Orders are placed as soon as the current candle close time reaches the trade time.
/// </summary>
public class TradingReportGeneratorFromCsvStrategy : Strategy
{
private readonly StrategyParam<string> _tradesCsv;
private readonly StrategyParam<DataType> _candleType;

private Queue<Trade> _trades;

private record Trade(bool IsBuy, decimal Quantity, decimal Price, DateTimeOffset Time);

/// <summary>
/// CSV with trades.
/// </summary>
public string TradesCsv
{
get => _tradesCsv.Value;
set => _tradesCsv.Value = value;
}

/// <summary>
/// Candle type for processing.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public TradingReportGeneratorFromCsvStrategy()
{
_tradesCsv = Param(nameof(TradesCsv), "Symbol,Side,Qty,Price,Time\n")
.SetDisplay("Trades CSV", "CSV describing trades", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Candles used for scheduling", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_trades = new Queue<Trade>();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_trades = ParseTrades(TradesCsv);

SubscribeCandles(CandleType)
.Bind(ProcessCandle)
.Start();
}

private Queue<Trade> ParseTrades(string csv)
{
var list = new List<Trade>();
var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
for (var i = 1; i < lines.Length; i++)
{
var parts = lines[i].Split(',');
if (parts.Length < 5)
continue;
var ticker = parts[0].Trim();
if (!string.Equals(ticker, Security.Id, StringComparison.OrdinalIgnoreCase))
continue;
var isBuy = parts[1].Trim().Equals("buy", StringComparison.OrdinalIgnoreCase);
var qty = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
var price = decimal.Parse(parts[3], CultureInfo.InvariantCulture);
var time = DateTimeOffset.Parse(parts[4], CultureInfo.InvariantCulture);
list.Add(new Trade(isBuy, qty, price, time));
}
list.Sort((a, b) => a.Time.CompareTo(b.Time));
return new Queue<Trade>(list);
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

while (_trades.Count > 0 && _trades.Peek().Time <= candle.CloseTime)
{
var t = _trades.Dequeue();
if (t.IsBuy)
BuyLimit(t.Price, t.Quantity);
else
SellLimit(t.Price, t.Quantity);
}
}
}
