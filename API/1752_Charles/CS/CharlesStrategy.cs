using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on daily high/low with RSI and EMA trend filter.
/// </summary>
public class CharlesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _delta;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	/// <summary>
	/// Price offset added to daily high and subtracted from daily low.
	/// </summary>
	public decimal Delta
	{
		get => _delta.Value;
		set => _delta.Value = value;
	}

	/// <summary>
	/// Timeframe used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	private decimal _dailyHigh;
	private decimal _dailyLow;
	private decimal _upperLevel;
	private decimal _lowerLevel;
	private DateTime _currentDate;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CharlesStrategy()
	{
		_delta = Param(nameof(Delta), 0.0002m)
			.SetDisplay("Price Offset", "Offset from daily high/low", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_fastPeriod = Param(nameof(FastPeriod), 18)
			.SetDisplay("Fast EMA Period", "Fast EMA length", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetDisplay("Slow EMA Period", "Slow EMA length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "Indicators");

		_takeProfit = Param(nameof(TakeProfit), 1m)
			.SetDisplay("Take Profit %", "Take profit in percent", "Protection");

		_stopLoss = Param(nameof(StopLoss), 0.5m)
			.SetDisplay("Stop Loss %", "Stop loss in percent", "Protection");
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

		StartProtection(new Unit(TakeProfit, UnitTypes.Percent), new Unit(StopLoss, UnitTypes.Percent));

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var date = candle.OpenTime.UtcDateTime.Date;
		if (date != _currentDate)
		{
		_currentDate = date;
		_dailyHigh = candle.HighPrice;
		_dailyLow = candle.LowPrice;
		}
		else
		{
		if (candle.HighPrice > _dailyHigh)
		_dailyHigh = candle.HighPrice;

		if (candle.LowPrice < _dailyLow)
		_dailyLow = candle.LowPrice;
		}

		_upperLevel = _dailyHigh + Delta;
		_lowerLevel = _dailyLow - Delta;

		var bullish = rsi > 55 && fastEma > slowEma;
		var bearish = rsi < 45 && fastEma < slowEma;

		if (bullish && candle.ClosePrice > _upperLevel && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (bearish && candle.ClosePrice < _lowerLevel && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}
}
