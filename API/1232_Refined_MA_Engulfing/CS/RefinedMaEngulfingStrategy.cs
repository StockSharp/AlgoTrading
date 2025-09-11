using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Refined MA + Engulfing strategy.
/// Uses two SMAs, engulfing candles, and confirmed structure breaks with cooldown.
/// </summary>
public class RefinedMaEngulfingStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _lastSwingHigh;
	private decimal? _lastSwingLow;
	private MarketStructure _marketStructure;
	private bool _structureConfirmed;
	private decimal _prevClose;
	private int _barIndex;
	private int? _lastLongBar;
	private int? _lastShortBar;

	/// <summary>
	/// First SMA length.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Second SMA length.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	/// <summary>
	/// Bars to wait after a trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initialize <see cref="RefinedMaEngulfingStrategy"/>.
	/// </summary>
	public RefinedMaEngulfingStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 66)
						 .SetGreaterThanZero()
						 .SetDisplay("MA1 Length", "Length of first MA", "Parameters")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 200, 1);

		_ma2Length = Param(nameof(Ma2Length), 85)
						 .SetGreaterThanZero()
						 .SetDisplay("MA2 Length", "Length of second MA", "Parameters")
						 .SetCanOptimize(true)
						 .SetOptimize(10, 200, 1);

		_cooldownBars = Param(nameof(CooldownBars), 5)
							.SetGreaterThanZero()
							.SetDisplay("Cooldown Bars", "Bars to wait after a trade", "Parameters")
							.SetCanOptimize(true)
							.SetOptimize(1, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma1 = new SimpleMovingAverage { Length = Ma1Length };
		var sma2 = new SimpleMovingAverage { Length = Ma2Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma1, sma2, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma1Value, decimal ma2Value)
	{
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;
		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		if (candle.State != CandleStates.Finished)
			return;

		if (_barIndex == 0)
		{
			_prevClose = candle.ClosePrice;
			_barIndex = 1;
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		var aboveMAs = close > ma1Value && close > ma2Value;
		var belowMAs = close < ma1Value && close < ma2Value;

		var bullEngulf = close > open && close > _prevClose && open <= _prevClose;
		var bearEngulf = close < open && close < _prevClose && open >= _prevClose;

		var pivotHigh = _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5;
		var pivotLow = _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5;

		if (pivotHigh)
			_lastSwingHigh = _h3;

		if (pivotLow)
			_lastSwingLow = _l3;

		var bullBreakConfirmed = _lastSwingHigh is decimal sh && close > sh;
		var bearBreakConfirmed = _lastSwingLow is decimal sl && close < sl;

		if (bullBreakConfirmed)
		{
			_marketStructure = MarketStructure.Bullish;
			_structureConfirmed = true;
		}

		if (bearBreakConfirmed)
		{
			_marketStructure = MarketStructure.Bearish;
			_structureConfirmed = true;
		}

		var bullishStructure = _marketStructure == MarketStructure.Bullish && _structureConfirmed;
		var bearishStructure = _marketStructure == MarketStructure.Bearish && _structureConfirmed;

		var longConfluence = 1; // fib placeholder
		if (bullEngulf)
			longConfluence++;
		if (bullishStructure)
			longConfluence++;
		if (aboveMAs)
			longConfluence++;

		var shortConfluence = 1; // fib placeholder
		if (bearEngulf)
			shortConfluence++;
		if (bearishStructure)
			shortConfluence++;
		if (belowMAs)
			shortConfluence++;

		var longReady = longConfluence >= 2;
		var shortReady = shortConfluence >= 2;

		var canLong = _lastLongBar is null || (_barIndex - _lastLongBar) >= CooldownBars;
		var canShort = _lastShortBar is null || (_barIndex - _lastShortBar) >= CooldownBars;

		var longCondition = longReady && canLong && bullishStructure && aboveMAs;
		var shortCondition = shortReady && canShort && bearishStructure && belowMAs;

		var volume = Volume + Math.Abs(Position);

		if (longCondition && Position <= 0)
		{
			BuyMarket(volume);
			_lastLongBar = _barIndex;
		}

		if (shortCondition && Position >= 0)
		{
			SellMarket(volume);
			_lastShortBar = _barIndex;
		}

		_prevClose = close;
		_barIndex++;
	}

	private enum MarketStructure
	{
		None,
		Bullish,
		Bearish
	}
}
