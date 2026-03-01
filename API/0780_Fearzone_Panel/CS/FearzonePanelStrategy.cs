using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fearzone Panel strategy.
/// </summary>
public class FearzonePanelStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<int> _impulsePeriod;
	private readonly StrategyParam<decimal> _impulsePercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stochThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private SimpleMovingAverage _lookbackSma;
	private BollingerBands _fz1Bands;
	private BollingerBands _fz2Bands;
	private SimpleMovingAverage _ma;
	private RateOfChange _roc;
	private StochasticOscillator _stochastic;

	/// <summary>
	/// Lookback period for FZ calculations.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Period for impulse drop check.
	/// </summary>
	public int ImpulsePeriod
	{
		get => _impulsePeriod.Value;
		set => _impulsePeriod.Value = value;
	}

	/// <summary>
	/// Required price drop ratio.
	/// </summary>
	public decimal ImpulsePercent
	{
		get => _impulsePercent.Value;
		set => _impulsePercent.Value = value;
	}

	/// <summary>
	/// Moving average filter period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oversold threshold.
	/// </summary>
	public decimal StochThreshold
	{
		get => _stochThreshold.Value;
		set => _stochThreshold.Value = value;
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
	/// Initializes a new instance of the <see cref="FearzonePanelStrategy"/>.
	/// </summary>
	public FearzonePanelStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 22)
		.SetGreaterThanZero()
		.SetDisplay("Lookback Period", "Lookback for FZ calculations", "Indicators")
		;

		_bollingerPeriod = Param(nameof(BollingerPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Bollinger period for FZ bands", "Indicators")
		;

		_impulsePeriod = Param(nameof(ImpulsePeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Impulse Period", "Lookback for impulse drop", "Strategy")
		;

		_impulsePercent = Param(nameof(ImpulsePercent), 0.1m)
		.SetDisplay("Impulse Percent", "Required drop ratio", "Strategy")
		;

		_maPeriod = Param(nameof(MaPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Filter MA length", "Indicators")
		;

		_stochThreshold = Param(nameof(StochThreshold), 30m)
		.SetDisplay("Stochastic Threshold", "Oversold level", "Strategy")
		;

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
		_highest = null;
		_lookbackSma = null;
		_fz1Bands = null;
		_fz2Bands = null;
		_ma = null;
		_roc = null;
		_stochastic = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lookbackSma = new SMA { Length = LookbackPeriod };
		_fz1Bands = new BollingerBands { Length = BollingerPeriod, Width = 1m };
		_fz2Bands = new BollingerBands { Length = BollingerPeriod, Width = 1m };
		_ma = new SMA { Length = MaPeriod };
		_roc = new RateOfChange { Length = ImpulsePeriod };
		_stochastic = new StochasticOscillator
		{
			K = { Length = 2 },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal kValue)
		return;

		var ohlc4 = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		var highestVal = _highest.Process(new DecimalIndicatorValue(_highest, ohlc4, candle.ServerTime)).ToDecimal();
		var smaVal = _lookbackSma.Process(new DecimalIndicatorValue(_lookbackSma, ohlc4, candle.ServerTime)).ToDecimal();
		var maVal = _ma.Process(new DecimalIndicatorValue(_ma, candle.ClosePrice, candle.ServerTime)).ToDecimal();
		var rocVal = _roc.Process(new DecimalIndicatorValue(_roc, candle.ClosePrice, candle.ServerTime)).ToDecimal();

		if (!_highest.IsFormed || !_lookbackSma.IsFormed || !_ma.IsFormed || !_roc.IsFormed)
		return;

		var fz1 = highestVal != 0 ? (highestVal - ohlc4) / highestVal : 0m;
		var fz1BandsResult = _fz1Bands.Process(new DecimalIndicatorValue(_fz1Bands, fz1, candle.ServerTime));
		var fz1BandsVal = (ComplexIndicatorValue<BollingerBands>)fz1BandsResult;
		var inFz1 = _fz1Bands.IsFormed && fz1 > fz1BandsVal.InnerValues[_fz1Bands.UpBand].ToDecimal();

		var fz2 = ohlc4 - smaVal;
		var fz2BandsResult = _fz2Bands.Process(new DecimalIndicatorValue(_fz2Bands, fz2, candle.ServerTime));
		var fz2BandsVal = (ComplexIndicatorValue<BollingerBands>)fz2BandsResult;
		var inFz2 = _fz2Bands.IsFormed && fz2 < fz2BandsVal.InnerValues[_fz2Bands.LowBand].ToDecimal();

		var isDown = rocVal <= -ImpulsePercent;

		var range = candle.HighPrice - candle.LowPrice;
		var inRicochet = range > 0 && candle.ClosePrice < candle.LowPrice + range * 0.1m;

		var isMagic = kValue < StochThreshold;
		var isAboveMa = candle.ClosePrice > maVal;

		if (inFz1 && inFz2 && isAboveMa && (isDown || inRicochet || isMagic) && Position <= 0)
		{
			BuyMarket();
		}
		else if (Position > 0 && !isAboveMa)
		{
			SellMarket();
		}
	}
}
