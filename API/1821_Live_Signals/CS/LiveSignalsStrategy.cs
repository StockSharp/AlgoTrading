namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy that executes trades from a CSV file with predefined times.
/// </summary>
public class LiveSignalsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _filePath;

	private readonly Queue<Signal> _signals = new();
	private Signal _current;

	private sealed class Signal
	{
		public DateTimeOffset OpenTime { get; init; }
		public DateTimeOffset CloseTime { get; init; }
		public decimal StopLoss { get; init; }
		public decimal TakeProfit { get; init; }
		public bool IsBuy { get; init; }
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used to check time.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Path to CSV file with signals.
	/// </summary>
	public string FilePath
	{
		get => _filePath.Value;
		set => _filePath.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="LiveSignalsStrategy"/>.
	/// </summary>
	public LiveSignalsStrategy()
	{
		_volume = Param(nameof(Volume), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for monitoring", "General");

		_filePath = Param(nameof(FilePath), "signals.csv")
			.SetDisplay("File Path", "CSV file with trading signals", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LoadSignals();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void LoadSignals()
	{
		if (!File.Exists(FilePath))
			return;

		foreach (var line in File.ReadLines(FilePath))
		{
			var parts = line.Split(',');
			if (parts.Length < 9)
				continue;

			var openTime = DateTimeOffset.Parse(parts[1], CultureInfo.InvariantCulture);
			var closeTime = DateTimeOffset.Parse(parts[2], CultureInfo.InvariantCulture);
			var take = decimal.Parse(parts[5], CultureInfo.InvariantCulture);
			var stop = decimal.Parse(parts[6], CultureInfo.InvariantCulture);
			var type = parts[7];

			_signals.Enqueue(new Signal
			{
				OpenTime = openTime,
				CloseTime = closeTime,
				TakeProfit = take,
				StopLoss = stop,
				IsBuy = type.Equals("Buy", StringComparison.OrdinalIgnoreCase)
			});
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_current == null && _signals.Count > 0)
		{
			var next = _signals.Peek();
			if (candle.OpenTime >= next.OpenTime)
			{
				_signals.Dequeue();
				_current = next;

				var volume = Volume + Math.Abs(Position);
				if (_current.IsBuy && Position <= 0)
					BuyMarket(volume);
				else if (!_current.IsBuy && Position >= 0)
					SellMarket(volume);
			}
		}

		if (_current != null && Position != 0)
		{
			var exit = candle.OpenTime >= _current.CloseTime;

			if (Position > 0)
				exit |= candle.LowPrice <= _current.StopLoss || candle.HighPrice >= _current.TakeProfit;
			else if (Position < 0)
				exit |= candle.HighPrice >= _current.StopLoss || candle.LowPrice <= _current.TakeProfit;

			if (exit)
			{
				var volume = Math.Abs(Position);
				if (Position > 0)
					SellMarket(volume);
				else
					BuyMarket(volume);

				_current = null;
			}
		}
	}
}

