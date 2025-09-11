using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Supertrend with EMA crossover and RSI filter.
/// </summary>
public class DailySupertrendEmaCrossoverRsiFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _hasPrev;
	private bool _wasFastAboveSlow;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit ATR multiplier.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
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
	/// Start date filter.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date filter.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public DailySupertrendEmaCrossoverRsiFilterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period for fast EMA", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period for slow EMA", "EMA")
			.SetCanOptimize(true)
			.SetOptimize(6, 30, 2);

		_atrLength = Param(nameof(AtrLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take ATR Mult", "ATR multiplier for take-profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(2m, 8m, 1m);

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
			.SetRange(50m, 100m)
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
			.SetRange(0m, 50m)
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Mult", "ATR multiplier for Supertrend", "Supertrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Start date filter", "Time");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(2099, 4, 28, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Date", "End date filter", "Time");
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

		_stopPrice = 0m;
		_takePrice = 0m;
		_hasPrev = false;
		_wasFastAboveSlow = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var supertrend = new SuperTrend { Length = AtrLength, Multiplier = SupertrendMultiplier };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, atr, supertrend, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, supertrend);

			var second = CreateChartArea();
			if (second != null)
			{
				DrawIndicator(second, rsi);
			}

			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0), new Unit(0), true);
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue fastVal,
		IIndicatorValue slowVal,
		IIndicatorValue atrVal,
		IIndicatorValue stVal,
		IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartDate || candle.OpenTime > EndDate)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();
		var atr = atrVal.ToDecimal();
		var rsi = rsiVal.ToDecimal();
		var st = (SuperTrendIndicatorValue)stVal;

		var isFastAboveSlow = fast > slow;
		if (!_hasPrev)
		{
			_wasFastAboveSlow = isFastAboveSlow;
			_hasPrev = true;
			return;
		}

		var crossUp = !_wasFastAboveSlow && isFastAboveSlow;
		var crossDown = _wasFastAboveSlow && !isFastAboveSlow;

		if (crossUp && st.IsUpTrend && rsi < RsiOverbought && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - StopLossMultiplier * atr;
			_takePrice = candle.ClosePrice + TakeProfitMultiplier * atr;
			LogInfo($"Long entry: Close {candle.ClosePrice}, ATR {atr}, RSI {rsi}");
		}
		else if (crossDown && !st.IsUpTrend && rsi > RsiOversold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + StopLossMultiplier * atr;
			_takePrice = candle.ClosePrice - TakeProfitMultiplier * atr;
			LogInfo($"Short entry: Close {candle.ClosePrice}, ATR {atr}, RSI {rsi}");
		}

		if (Position > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takePrice)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long: Close {candle.ClosePrice}, Stop {_stopPrice}, Take {_takePrice}");
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Close {candle.ClosePrice}, Stop {_stopPrice}, Take {_takePrice}");
			}
		}

		_wasFastAboveSlow = isFastAboveSlow;
	}
}
