
using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle pattern based strategy converted from the CandlePatternsTest EA.
/// </summary>
public class CandlePatternsTestStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _averageBodyPeriod;
	private readonly StrategyParam<bool> _enableBullishPatterns;
	private readonly StrategyParam<bool> _enableBearishPatterns;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<decimal> _takeProfitFactor;

	private SimpleMovingAverage _closeAverage;
	private SimpleMovingAverage _bodyAverage;

	private ICandleMessage _previousCandle1;
	private ICandleMessage _previousCandle2;
	private ICandleMessage _previousCandle3;

	private decimal? _previousCloseAverage1;
	private decimal? _previousCloseAverage2;
	private decimal? _previousCloseAverage3;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlePatternsTestStrategy"/> class.
	/// </summary>
	public CandlePatternsTestStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for pattern detection", "General");

		_averageBodyPeriod = Param(nameof(AverageBodyPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Body Average Period", "Number of candles for average body", "Pattern")
			.SetCanOptimize(true)
			.SetOptimize(6, 24, 2);

		_enableBullishPatterns = Param(nameof(EnableBullishPatterns), true)
			.SetDisplay("Enable Bullish", "Allow opening long positions", "Pattern");

		_enableBearishPatterns = Param(nameof(EnableBearishPatterns), true)
			.SetDisplay("Enable Bearish", "Allow opening short positions", "Pattern");

		_stopLossFactor = Param(nameof(StopLossFactor), 1.5m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss Factor", "Average body multiplier for stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 3m)
			.SetRange(1m, 8m)
			.SetDisplay("Take Profit Factor", "Average body multiplier for target", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(2m, 6m, 1m);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to build the average body size.
	/// </summary>
	public int AverageBodyPeriod
	{
		get => _averageBodyPeriod.Value;
		set => _averageBodyPeriod.Value = value;
	}

	/// <summary>
	/// Enables long entries based on bullish patterns.
	/// </summary>
	public bool EnableBullishPatterns
	{
		get => _enableBullishPatterns.Value;
		set => _enableBullishPatterns.Value = value;
	}

	/// <summary>
	/// Enables short entries based on bearish patterns.
	/// </summary>
	public bool EnableBearishPatterns
	{
		get => _enableBearishPatterns.Value;
		set => _enableBearishPatterns.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average body for stop loss distance.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average body for take profit distance.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
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

		_previousCandle1 = null;
		_previousCandle2 = null;
		_previousCandle3 = null;

		_previousCloseAverage1 = null;
		_previousCloseAverage2 = null;
		_previousCloseAverage3 = null;

		_entryPrice = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeAverage = new SimpleMovingAverage { Length = AverageBodyPeriod };
		_bodyAverage = new SimpleMovingAverage { Length = AverageBodyPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeAverage, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeAverage);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeAverage)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var bodyValue = _bodyAverage.Process(new DecimalIndicatorValue(_bodyAverage, Math.Abs(candle.ClosePrice - candle.OpenPrice), candle.ServerTime));
		if (!bodyValue.IsFormed || !_closeAverage.IsFormed)
		{
		ShiftHistory(candle, null);
		return;
		}

		var averageBody = bodyValue.ToDecimal();
		var currentCloseAverage = closeAverage;

		var bullish = EnableBullishPatterns && _previousCandle3 != null && _previousCandle2 != null && _previousCandle1 != null &&
		IsBullishPattern(_previousCandle3, _previousCandle2, _previousCandle1, averageBody, _previousCloseAverage3, _previousCloseAverage2, _previousCloseAverage1);

		var bearish = EnableBearishPatterns && _previousCandle3 != null && _previousCandle2 != null && _previousCandle1 != null &&
		IsBearishPattern(_previousCandle3, _previousCandle2, _previousCandle1, averageBody, _previousCloseAverage3, _previousCloseAverage2, _previousCloseAverage1);

		ManagePosition(candle);

		if (Position == 0)
		{
		if (bullish)
		{
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = candle.LowPrice - averageBody * StopLossFactor;
		_targetPrice = candle.ClosePrice + averageBody * TakeProfitFactor;
		}
		else if (bearish)
		{
		SellMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = candle.HighPrice + averageBody * StopLossFactor;
		_targetPrice = candle.ClosePrice - averageBody * TakeProfitFactor;
		}
		}
		else if (Position > 0 && bearish)
		{
		ClosePosition();
		SellMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = candle.HighPrice + averageBody * StopLossFactor;
		_targetPrice = candle.ClosePrice - averageBody * TakeProfitFactor;
		}
		else if (Position < 0 && bullish)
		{
		ClosePosition();
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		_stopPrice = candle.LowPrice - averageBody * StopLossFactor;
		_targetPrice = candle.ClosePrice + averageBody * TakeProfitFactor;
		}

		ShiftHistory(candle, currentCloseAverage);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position == 0 || _entryPrice == null)
		return;

		if (Position > 0)
		{
		var hitStop = _stopPrice != null && candle.LowPrice <= _stopPrice;
		var hitTarget = _targetPrice != null && candle.HighPrice >= _targetPrice;

		if (hitStop || hitTarget)
		{
		ClosePosition();
		_entryPrice = null;
		_stopPrice = null;
		_targetPrice = null;
		}
		}
		else if (Position < 0)
		{
		var hitStop = _stopPrice != null && candle.HighPrice >= _stopPrice;
		var hitTarget = _targetPrice != null && candle.LowPrice <= _targetPrice;

		if (hitStop || hitTarget)
		{
		ClosePosition();
		_entryPrice = null;
		_stopPrice = null;
		_targetPrice = null;
		}
		}
	}

	private void ShiftHistory(ICandleMessage candle, decimal? closeAverage)
	{
		_previousCandle3 = _previousCandle2;
		_previousCandle2 = _previousCandle1;
		_previousCandle1 = candle;

		_previousCloseAverage3 = _previousCloseAverage2;
		_previousCloseAverage2 = _previousCloseAverage1;
		_previousCloseAverage1 = closeAverage;
	}

	private static bool IsBullishPattern(ICandleMessage c3, ICandleMessage c2, ICandleMessage c1, decimal avgBody,
	decimal? closeAvg3, decimal? closeAvg2, decimal? closeAvg1)
	{
		var body3 = Math.Abs(c3.ClosePrice - c3.OpenPrice);
		var body2 = Math.Abs(c2.ClosePrice - c2.OpenPrice);
		var body1 = Math.Abs(c1.ClosePrice - c1.OpenPrice);

		var mid3 = (c3.HighPrice + c3.LowPrice) / 2m;
		var mid2 = (c2.HighPrice + c2.LowPrice) / 2m;
		var mid1 = (c1.HighPrice + c1.LowPrice) / 2m;

		var midOc3 = (c3.OpenPrice + c3.ClosePrice) / 2m;
		var midOc2 = (c2.OpenPrice + c2.ClosePrice) / 2m;

		if (c3.ClosePrice > c3.OpenPrice && c2.ClosePrice > c2.OpenPrice && c1.ClosePrice > c1.OpenPrice &&
		body3 > avgBody && body2 > avgBody && body1 > avgBody && mid2 > mid3 && mid1 > mid2)
		return true;

		if (closeAvg2.HasValue && c1.ClosePrice > c1.OpenPrice && c2.OpenPrice > c2.ClosePrice && c2.ClosePrice > c1.ClosePrice &&
		c1.ClosePrice < c2.OpenPrice && midOc2 < closeAvg2.Value && c1.OpenPrice < c2.LowPrice)
		return true;

		if (closeAvg2.HasValue && closeAvg3.HasValue && c3.OpenPrice > c3.ClosePrice && body2 < avgBody * 0.1m &&
		c2.ClosePrice < c3.ClosePrice && c2.OpenPrice < c3.OpenPrice && c1.OpenPrice > c2.ClosePrice && c1.ClosePrice > c2.ClosePrice)
		return true;

		if (closeAvg2.HasValue && c2.OpenPrice > c2.ClosePrice && body1 > avgBody && c1.ClosePrice > c2.OpenPrice &&
		midOc2 < closeAvg2.Value && c1.OpenPrice < c2.ClosePrice)
		return true;

		if (c3.OpenPrice > c3.ClosePrice && body2 < avgBody * 0.5m && c2.ClosePrice < c3.ClosePrice && c2.OpenPrice < c3.OpenPrice &&
		c1.ClosePrice > midOc3)
		return true;

		if (closeAvg3.HasValue && c1.ClosePrice > c1.OpenPrice && c2.OpenPrice - c2.ClosePrice > avgBody &&
		c1.ClosePrice < c2.OpenPrice && c1.OpenPrice > c2.ClosePrice && mid3 < closeAvg3.Value)
		return true;

		if (c2.OpenPrice - c2.ClosePrice > avgBody && c1.ClosePrice - c1.OpenPrice > avgBody && Math.Abs(c1.ClosePrice - c2.ClosePrice) < 0.1m * avgBody)
		return true;

		return false;
	}

	private static bool IsBearishPattern(ICandleMessage c3, ICandleMessage c2, ICandleMessage c1, decimal avgBody,
	decimal? closeAvg3, decimal? closeAvg2, decimal? closeAvg1)
	{
		var body3 = Math.Abs(c3.ClosePrice - c3.OpenPrice);
		var body2 = Math.Abs(c2.ClosePrice - c2.OpenPrice);
		var body1 = Math.Abs(c1.ClosePrice - c1.OpenPrice);

		var mid3 = (c3.HighPrice + c3.LowPrice) / 2m;
		var mid2 = (c2.HighPrice + c2.LowPrice) / 2m;
		var mid1 = (c1.HighPrice + c1.LowPrice) / 2m;

		var midOc3 = (c3.OpenPrice + c3.ClosePrice) / 2m;
		var midOc2 = (c2.OpenPrice + c2.ClosePrice) / 2m;

		if (c3.OpenPrice > c3.ClosePrice && c2.OpenPrice > c2.ClosePrice && c1.OpenPrice > c1.ClosePrice &&
		body3 > avgBody && body2 > avgBody && body1 > avgBody && mid2 < mid3 && mid1 < mid2)
		return true;

		if (closeAvg1.HasValue && c2.ClosePrice > c2.OpenPrice && c1.ClosePrice < c2.ClosePrice && c1.ClosePrice > c2.OpenPrice &&
		midOc2 > closeAvg1.Value && c1.OpenPrice > c2.HighPrice)
		return true;

		if (c3.ClosePrice > c3.OpenPrice && body2 < avgBody * 0.1m && c2.ClosePrice > c3.ClosePrice && c2.OpenPrice > c3.OpenPrice &&
		c1.OpenPrice < c2.ClosePrice && c1.ClosePrice < c2.ClosePrice)
		return true;

		if (closeAvg2.HasValue && c2.OpenPrice < c2.ClosePrice && body1 > avgBody && c1.ClosePrice < c2.OpenPrice &&
		midOc2 > closeAvg2.Value && c1.OpenPrice > c2.ClosePrice)
		return true;

		if (c3.ClosePrice > c3.OpenPrice && body2 < avgBody * 0.5m && c2.ClosePrice > c3.ClosePrice && c2.OpenPrice > c3.OpenPrice &&
		c1.ClosePrice < midOc3)
		return true;

		if (closeAvg3.HasValue && c1.OpenPrice > c1.ClosePrice && c2.ClosePrice - c2.OpenPrice > avgBody &&
		c1.ClosePrice > c2.OpenPrice && c1.OpenPrice < c2.ClosePrice && mid3 > closeAvg3.Value)
		return true;

		if (c2.ClosePrice - c2.OpenPrice > avgBody && c1.OpenPrice - c1.ClosePrice > avgBody && Math.Abs(c1.ClosePrice - c2.ClosePrice) < 0.1m * avgBody)
		return true;

		return false;
	}
}
