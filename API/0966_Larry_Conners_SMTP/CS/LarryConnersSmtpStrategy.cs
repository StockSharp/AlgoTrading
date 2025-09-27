using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Conners SMTP Strategy.
/// </summary>
public class LarryConnersSmtpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tickSize;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _rangeHighest = null!;
	private decimal _stopLoss;

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
	/// Initializes a new instance of the <see cref="LarryConnersSmtpStrategy"/> class.
	/// </summary>
	public LarryConnersSmtpStrategy()
	{
		_tickSize = Param(nameof(TickSize), 0.01m)
			.SetDisplay("Tick Size", "Minimum price increment", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.1m, 0.001m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

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

		var range = candle.HighPrice - candle.LowPrice;
		var maxRange = _rangeHighest.Process(range);

		if (!_rangeHighest.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		var is10PeriodLow = candle.LowPrice == low10;
		var isLargestRange = range == maxRange;
		var isCloseInTop25 = range > 0m && (candle.ClosePrice - candle.LowPrice) / range >= 0.75m;

		var buyCondition = is10PeriodLow && isLargestRange && isCloseInTop25;

		if (buyCondition && Position == 0)
		{
			var buyPrice = candle.HighPrice + TickSize;
			_stopLoss = candle.LowPrice;
			BuyStop(Volume, buyPrice);
		}

		if (Position > 0)
		{
			_stopLoss = Math.Max(_stopLoss, candle.LowPrice);
			SellStop(Position, _stopLoss);
		}
	}
}
