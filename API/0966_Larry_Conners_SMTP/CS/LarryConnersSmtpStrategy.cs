using System;
using System.Collections.Generic;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Conners SMTP Strategy.
/// </summary>
public class LarryConnersSmtpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tickSize;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _rangeHighest = null!;
	private decimal _stopLoss;
	private int _entriesExecuted;
	private int _barsSinceSignal;

	/// <summary>
	/// Tick size.
	/// </summary>
	public decimal TickSize
	{
		get => _tickSize.Value;
		set => _tickSize.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum entries per run.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LarryConnersSmtpStrategy"/> class.
	/// </summary>
	public LarryConnersSmtpStrategy()
	{
		_tickSize = Param(nameof(TickSize), 0.01m)
			.SetDisplay("Tick Size", "Minimum price increment", "General")
			.SetGreaterThanZero()
			
			.SetOptimize(0.001m, 0.1m, 0.001m);

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetGreaterThanZero()
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_stopLoss = 0m;
		_rangeHighest = null!;
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		var lowest = new Lowest { Length = 10 };
		_rangeHighest = new Highest { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lowest, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal low10)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		var range = candle.HighPrice - candle.LowPrice;
		var maxRangeValue = _rangeHighest.Process(new DecimalIndicatorValue(_rangeHighest, range, candle.OpenTime)); var maxRange = maxRangeValue.ToDecimal();

		if (!_rangeHighest.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var is10PeriodLow = candle.LowPrice <= low10 + TickSize;
		var buyCondition = is10PeriodLow && candle.ClosePrice > candle.OpenPrice;

		if (buyCondition && Position == 0 && _entriesExecuted < MaxEntries && _barsSinceSignal >= CooldownBars)
		{
			_stopLoss = candle.LowPrice;
			BuyMarket();
			_entriesExecuted++;
			_barsSinceSignal = 0;
		}

		if (Position > 0)
		{
			_stopLoss = Math.Max(_stopLoss, candle.LowPrice);
			if (candle.ClosePrice <= _stopLoss)
			{
				SellMarket(Math.Abs(Position));
				_barsSinceSignal = 0;
			}
		}
	}
}
