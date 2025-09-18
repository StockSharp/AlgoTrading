using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replays historical trades from a CSV file.
/// Opens and closes positions according to the recorded trade schedule.
/// Useful for visual analysis of previous deals on a live chart.
/// </summary>
public class ShowHistoryOnChartStrategy : Strategy
{
	private sealed class ScheduledTrade
	{
		public DateTimeOffset OpenTime { get; init; }
		public DateTimeOffset CloseTime { get; init; }
		public bool IsBuy { get; init; }
		public decimal Volume { get; init; }
	}

	private readonly List<ScheduledTrade> _scheduledTrades = new();
	private readonly List<ScheduledTrade> _activeTrades = new();
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<DataType> _candleType;
	private int _nextTradeIndex;

	/// <summary>
	/// Path to the CSV file with the exported trade history.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value ?? string.Empty;
	}

	/// <summary>
	/// Candle type that drives the trade replay timeline.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ShowHistoryOnChartStrategy()
	{
		_fileName = Param(nameof(FileName), string.Empty)
			.SetDisplay("Trade History File", "Path to the CSV file exported from MetaTrader", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for scheduling trade events", "General");
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
		_scheduledTrades.Clear();
		_activeTrades.Clear();
		_nextTradeIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LoadTrades();

		if (_scheduledTrades.Count == 0)
		{
			LogInfo("No trades loaded from the CSV file. Strategy will remain idle.");
		}
		else
		{
			SkipPastTrades(time);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenCandlesFinished(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleTime = candle.CloseTime;

		while (_nextTradeIndex < _scheduledTrades.Count && _scheduledTrades[_nextTradeIndex].OpenTime <= candleTime)
		{
			var trade = _scheduledTrades[_nextTradeIndex];
			OpenScheduledTrade(trade);
			_nextTradeIndex++;
		}

		var index = 0;
		while (index < _activeTrades.Count)
		{
			var trade = _activeTrades[index];
			if (trade.CloseTime <= candleTime)
			{
				CloseScheduledTrade(trade);
				_activeTrades.RemoveAt(index);
				continue;
			}

			index++;
		}
	}

	private void OpenScheduledTrade(ScheduledTrade trade)
	{
		if (trade.IsBuy)
		{
			BuyMarket(trade.Volume);
			LogInfo($"Opened historical long position at {trade.OpenTime:O} for volume {trade.Volume}.");
		}
		else
		{
			SellMarket(trade.Volume);
			LogInfo($"Opened historical short position at {trade.OpenTime:O} for volume {trade.Volume}.");
		}

		_activeTrades.Add(trade);
	}

	private void CloseScheduledTrade(ScheduledTrade trade)
	{
		if (trade.IsBuy)
		{
			SellMarket(trade.Volume);
			LogInfo($"Closed historical long position at {trade.CloseTime:O}.");
		}
		else
		{
			BuyMarket(trade.Volume);
			LogInfo($"Closed historical short position at {trade.CloseTime:O}.");
		}
	}

	private void LoadTrades()
	{
		_scheduledTrades.Clear();
		_activeTrades.Clear();
		_nextTradeIndex = 0;

		var filePath = FileName;
		if (string.IsNullOrWhiteSpace(filePath))
		{
			LogWarning("The CSV file path is not specified.");
			return;
		}

		if (!File.Exists(filePath))
		{
			LogWarning($"The CSV file '{filePath}' was not found.");
			return;
		}

		using var reader = new StreamReader(filePath);
		while (!reader.EndOfStream)
		{
			var line = reader.ReadLine();
			if (string.IsNullOrWhiteSpace(line))
				continue;

			var parts = line.Split(';');
			if (parts.Length < 11)
			{
				LogWarning($"Skipped malformed line: {line}");
				continue;
			}

			var symbol = parts[3].Trim();
			if (!IsMatchingSecurity(symbol))
				continue;

			if (!TryParseTime(parts[0], out var openTime) || !TryParseTime(parts[6], out var closeTime))
			{
				LogWarning($"Failed to parse times for line: {line}");
				continue;
			}

			if (!decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var volume))
			{
				LogWarning($"Failed to parse volume for line: {line}");
				continue;
			}

			var typeValue = parts[1].Trim();
			var isBuy = string.Equals(typeValue, "Buy", StringComparison.OrdinalIgnoreCase);
			var isSell = string.Equals(typeValue, "Sell", StringComparison.OrdinalIgnoreCase);
			if (!isBuy && !isSell)
			{
				LogWarning($"Unsupported trade type '{typeValue}'. Line skipped.");
				continue;
			}

			if (closeTime <= openTime)
			{
				LogWarning($"Close time {closeTime:O} precedes open time {openTime:O}. Line skipped.");
				continue;
			}

			var trade = new ScheduledTrade
			{
				OpenTime = openTime,
				CloseTime = closeTime,
				IsBuy = isBuy,
				Volume = Math.Abs(volume)
			};

			InsertTradeSorted(trade);
		}
	}

	private bool TryParseTime(string value, out DateTimeOffset result)
	{
		if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result))
			return true;

		if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
		{
			result = new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
			return true;
		}

		result = default;
		return false;
	}

	private bool IsMatchingSecurity(string symbol)
	{
		if (Security == null)
			return true;

		var securityId = Security.Id ?? string.Empty;
		var securityCode = Security.Code ?? string.Empty;

		if (string.Equals(symbol, securityId, StringComparison.OrdinalIgnoreCase))
			return true;

		if (string.Equals(symbol, securityCode, StringComparison.OrdinalIgnoreCase))
			return true;

		return false;
	}

	private void InsertTradeSorted(ScheduledTrade trade)
	{
		var index = _scheduledTrades.BinarySearch(trade, ScheduledTradeComparer.Instance);
		if (index < 0)
			index = ~index;
		_scheduledTrades.Insert(index, trade);
	}

	private void SkipPastTrades(DateTimeOffset startTime)
	{
		var skipped = 0;
		while (_nextTradeIndex < _scheduledTrades.Count && _scheduledTrades[_nextTradeIndex].OpenTime <= startTime)
		{
			_nextTradeIndex++;
			skipped++;
		}

		if (skipped > 0)
		{
			LogInfo($"Skipped {skipped} trades that started before the strategy launch time.");
		}
	}

	private sealed class ScheduledTradeComparer : IComparer<ScheduledTrade>
	{
		public static readonly ScheduledTradeComparer Instance = new();

		public int Compare(ScheduledTrade? x, ScheduledTrade? y)
		{
			if (ReferenceEquals(x, y))
				return 0;

			if (x is null)
				return -1;

			if (y is null)
				return 1;

			return x.OpenTime.CompareTo(y.OpenTime);
		}
	}
}
