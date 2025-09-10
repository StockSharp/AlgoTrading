using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy for AUD/USD using EMA trend filter, Bollinger Bands and RSI.
/// </summary>
public class AudUsdScalpingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaShort;
	private readonly StrategyParam<int> _emaLong;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int EmaShort
	{
		get => _emaShort.Value;
		set => _emaShort.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaLong
	{
		get => _emaLong.Value;
		set => _emaLong.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit value in price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss value in price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public AudUsdScalpingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_emaShort = Param(nameof(EmaShort), 13)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Fast EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_emaLong = Param(nameof(EmaLong), 26)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Slow EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 5);

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 2);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "Overbought threshold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60, 80, 5);

		_rsiOversold = Param(nameof(RsiOversold), 30)
			.SetDisplay("RSI Oversold", "Oversold threshold", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 5);

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Mult", "Bollinger Bands multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_takeProfit = Param(nameof(TakeProfit), 0.0005m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.001m, 0.0001m);

		_stopLoss = Param(nameof(StopLoss), 0.0004m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.0001m, 0.001m, 0.0001m);
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

		_emaFast = new ExponentialMovingAverage { Length = EmaShort };
		_emaSlow = new ExponentialMovingAverage { Length = EmaLong };
		_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _emaFast, _emaSlow, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfit, UnitTypes.Price), new Unit(StopLoss, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal bbMiddle, decimal bbUpper, decimal bbLower,
		decimal emaFast, decimal emaSlow, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed)
			return;

		var isUpTrend = emaFast > emaSlow;
		var isDownTrend = emaFast < emaSlow;

		var longCondition = isUpTrend && candle.ClosePrice <= bbLower && rsiValue > RsiOversold;
		var shortCondition = isDownTrend && candle.ClosePrice >= bbUpper && rsiValue < RsiOverbought;

		if (longCondition && Position <= 0)
			RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, Volume));
		else if (shortCondition && Position >= 0)
			RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Volume));
	}
}
