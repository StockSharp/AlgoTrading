using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DSL strategy.
/// Discontinued signal lines: the upper line moves toward the close (by DslFastMode ? 2 : 1 divided by the length) only while the
/// close is above its SMA, the lower line only while the close is below it. Price lines use Length, the Beluga oscillator is an
/// RSI(BelugaLength) with its own lines over BelugaLength. The upper band is the upper line minus ATR(Offset) * BandsWidth and the
/// lower band is the lower line plus that distance.
/// Long: upper band above the lower line, open and close above the upper line for three candles, and the oscillator crossing above
/// its lower line. Short: lower band below the upper line, open and close below the lower line for three candles, and the
/// oscillator crossing below its upper line. The stop sits at the band of the entry side and the target RiskReward times the risk away.
/// </summary>
public class DslStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _offset;
	private readonly StrategyParam<decimal> _bandsWidth;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _belugaLength;
	private readonly StrategyParam<bool> _dslFastMode;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _oscSma;
	private decimal? _upper;
	private decimal? _lower;
	private decimal? _oscUpper;
	private decimal? _oscLower;
	private decimal? _prevOsc;
	private decimal? _prevOscUpper;
	private decimal? _prevOscLower;
	private int _barsAbove;
	private int _barsBelow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Period of the price DSL lines.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// ATR period of the band offset.
	/// </summary>
	public int Offset
	{
		get => _offset.Value;
		set => _offset.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the bands.
	/// </summary>
	public decimal BandsWidth
	{
		get => _bandsWidth.Value;
		set => _bandsWidth.Value = value;
	}

	/// <summary>
	/// Take profit as a multiple of the risk.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Period of the Beluga oscillator and its DSL lines.
	/// </summary>
	public int BelugaLength
	{
		get => _belugaLength.Value;
		set => _belugaLength.Value = value;
	}

	/// <summary>
	/// Doubles the speed of the DSL lines.
	/// </summary>
	public bool DslFastMode
	{
		get => _dslFastMode.Value;
		set => _dslFastMode.Value = value;
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
	public DslStrategy()
	{
		_length = Param(nameof(Length), 34)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period of the price DSL lines", "DSL");

		_offset = Param(nameof(Offset), 30)
			.SetGreaterThanZero()
			.SetDisplay("Offset", "ATR period of the band offset", "DSL");

		_bandsWidth = Param(nameof(BandsWidth), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bands Width", "ATR multiplier of the bands", "DSL");

		_riskReward = Param(nameof(RiskReward), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit as a multiple of the risk", "Risk");

		_belugaLength = Param(nameof(BelugaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Beluga Length", "Period of the Beluga oscillator", "Oscillator");

		_dslFastMode = Param(nameof(DslFastMode), true)
			.SetDisplay("DSL Fast Mode", "Doubles the speed of the DSL lines", "DSL");

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
		_upper = null;
		_lower = null;
		_oscUpper = null;
		_oscLower = null;
		_prevOsc = null;
		_prevOscUpper = null;
		_prevOscLower = null;
		_barsAbove = 0;
		_barsBelow = 0;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sma = new SimpleMovingAverage { Length = Length };
		var atr = new AverageTrueRange { Length = Offset };
		var rsi = new RelativeStrengthIndex { Length = BelugaLength };
		_oscSma = new SimpleMovingAverage { Length = BelugaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, atr, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private static decimal UpdateLine(decimal? line, decimal value, bool move, decimal alpha)
	{
		if (line is not decimal current)
			return value;

		return move ? current + alpha * (value - current) : current;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue atrValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !atrValue.IsFormed || !rsiValue.IsFormed)
			return;

		var close = candle.ClosePrice;
		var speed = DslFastMode ? 2m : 1m;

		var sma = smaValue.GetValue<decimal>();
		var upper = UpdateLine(_upper, close, close > sma, speed / Length);
		var lower = UpdateLine(_lower, close, close < sma, speed / Length);
		_upper = upper;
		_lower = lower;

		var osc = rsiValue.GetValue<decimal>();
		var oscSmaValue = _oscSma.Process(new DecimalIndicatorValue(_oscSma, osc, candle.OpenTime) { IsFinal = true });

		if (!oscSmaValue.IsFormed)
			return;

		var oscSma = oscSmaValue.GetValue<decimal>();
		var oscUpper = UpdateLine(_oscUpper, osc, osc > oscSma, speed / BelugaLength);
		var oscLower = UpdateLine(_oscLower, osc, osc < oscSma, speed / BelugaLength);
		_oscUpper = oscUpper;
		_oscLower = oscLower;

		var prevOsc = _prevOsc;
		var prevOscUpper = _prevOscUpper;
		var prevOscLower = _prevOscLower;
		_prevOsc = osc;
		_prevOscUpper = oscUpper;
		_prevOscLower = oscLower;

		_barsAbove = candle.OpenPrice > upper && close > upper ? _barsAbove + 1 : 0;
		_barsBelow = candle.OpenPrice < lower && close < lower ? _barsBelow + 1 : 0;

		var distance = atrValue.GetValue<decimal>() * BandsWidth;
		var upperBand = upper - distance;
		var lowerBand = lower + distance;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);

			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);

			return;
		}

		if (prevOsc is not decimal po || prevOscUpper is not decimal pou || prevOscLower is not decimal pol)
			return;

		var oscCrossUp = po <= pol && osc > oscLower;
		var oscCrossDown = po >= pou && osc < oscUpper;

		if (upperBand > lower && _barsAbove >= 3 && oscCrossUp && close > upperBand)
		{
			_stopPrice = upperBand;
			_takePrice = close + (close - upperBand) * RiskReward;
			BuyMarket(Volume);
		}
		else if (lowerBand < upper && _barsBelow >= 3 && oscCrossDown && close < lowerBand)
		{
			_stopPrice = lowerBand;
			_takePrice = close - (lowerBand - close) * RiskReward;
			SellMarket(Volume);
		}
	}
}
