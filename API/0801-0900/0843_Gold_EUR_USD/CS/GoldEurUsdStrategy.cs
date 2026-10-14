using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold and EUR/USD liquidity grab strategy.
/// A bullish liquidity grab is a candle that wicks below the lowest low of the previous candles while RSI is below Oversold and the
/// Stochastic %K below StochOversold. Within the following candles a bullish fair value gap (low above the high two candles back) whose
/// candle spans more than one ATR and closes above the previous high marks the market structure shift; if the close is also above the
/// MaLength SMA the strategy goes long. Shorts mirror the rules. An opposite signal reverses the position.
/// </summary>
public class GoldEurUsdStrategy : Strategy
{
	private const int _swingLength = 20;
	private const int _atrLength = 14;
	private const int _setupBars = 10;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHighest;
	private decimal? _prevLowest;
	private decimal? _high1, _high2, _low1, _low2;
	private int _barsSinceBullGrab;
	private int _barsSinceBearGrab;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// SMA trend filter length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
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
	/// Constructor.
	/// </summary>
	public GoldEurUsdStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA trend filter length", "Indicators");

		_stochLength = Param(nameof(StochLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch Length", "Stochastic %K length", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "RSI overbought level", "Levels");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "RSI oversold level", "Levels");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stoch Overbought", "Stochastic overbought level", "Levels");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stoch Oversold", "Stochastic oversold level", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevHighest = null;
		_prevLowest = null;
		_high1 = _high2 = _low1 = _low2 = null;
		_barsSinceBullGrab = int.MaxValue;
		_barsSinceBearGrab = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var sma = new SimpleMovingAverage { Length = MaLength };
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = 3 },
		};
		var atr = new AverageTrueRange { Length = _atrLength };
		var highest = new Highest { Length = _swingLength };
		var lowest = new Lowest { Length = _swingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, sma, stochastic, atr, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue smaValue, IIndicatorValue stochValue,
		IIndicatorValue atrValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The swing extremes and the fair value gap use the candles before this one.
		var swingHigh = _prevHighest;
		var swingLow = _prevLowest;
		var high1 = _high1;
		var high2 = _high2;
		var low1 = _low1;
		var low2 = _low2;

		if (highestValue.IsFormed && lowestValue.IsFormed)
		{
			_prevHighest = highestValue.GetValue<decimal>();
			_prevLowest = lowestValue.GetValue<decimal>();
		}

		_high2 = _high1;
		_low2 = _low1;
		_high1 = candle.HighPrice;
		_low1 = candle.LowPrice;

		if (_barsSinceBullGrab < int.MaxValue)
			_barsSinceBullGrab++;
		if (_barsSinceBearGrab < int.MaxValue)
			_barsSinceBearGrab++;

		if (!rsiValue.IsFormed || !smaValue.IsFormed || !stochValue.IsFormed || !atrValue.IsFormed)
			return;

		if (stochValue is not IStochasticOscillatorValue { K: decimal stochK })
			return;

		if (swingHigh is not decimal sh || swingLow is not decimal sl || high1 is not decimal h1 || high2 is not decimal h2 || low1 is not decimal l1 || low2 is not decimal l2)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var sma = smaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (candle.LowPrice < sl && close > sl && rsi < Oversold && stochK < StochOversold)
			_barsSinceBullGrab = 0;

		if (candle.HighPrice > sh && close < sh && rsi > Overbought && stochK > StochOverbought)
			_barsSinceBearGrab = 0;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var displacement = candle.HighPrice - candle.LowPrice > atr;
		var bullishShift = displacement && candle.LowPrice > h2 && close > h1;
		var bearishShift = displacement && candle.HighPrice < l2 && close < l1;

		if (_barsSinceBullGrab <= _setupBars && bullishShift && close > sma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsSinceBullGrab = int.MaxValue;
		}
		else if (_barsSinceBearGrab <= _setupBars && bearishShift && close < sma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_barsSinceBearGrab = int.MaxValue;
		}
	}
}
