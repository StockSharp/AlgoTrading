using System;
using System.Collections.Generic;

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
		.SetCanOptimize(true);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Bollinger period for FZ bands", "Indicators")
		.SetCanOptimize(true);

		_impulsePeriod = Param(nameof(ImpulsePeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Impulse Period", "Lookback for impulse drop", "Strategy")
		.SetCanOptimize(true);

		_impulsePercent = Param(nameof(ImpulsePercent), 0.1m)
		.SetDisplay("Impulse Percent", "Required drop ratio", "Strategy")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Filter MA length", "Indicators")
		.SetCanOptimize(true);

		_stochThreshold = Param(nameof(StochThreshold), 30m)
		.SetDisplay("Stochastic Threshold", "Oversold level", "Strategy")
		.SetCanOptimize(true);

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
		_highest = null;
		_lookbackSma = null;
		_fz1Bands = null;
		_fz2Bands = null;
		_ma = null;
		_roc = null;
		_stochastic = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lookbackSma = new SimpleMovingAverage { Length = LookbackPeriod };
		_fz1Bands = new BollingerBands { Length = BollingerPeriod, Width = 1m };
		_fz2Bands = new BollingerBands { Length = BollingerPeriod, Width = 1m };
		_ma = new SimpleMovingAverage { Length = MaPeriod };
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

		var highestVal = _highest.Process(ohlc4);
		var smaVal = _lookbackSma.Process(ohlc4);
		var maVal = _ma.Process(candle.ClosePrice);
		var rocVal = _roc.Process(candle.ClosePrice);

		if (!_highest.IsFormed || !_lookbackSma.IsFormed || !_ma.IsFormed || !_roc.IsFormed)
		return;

		var fz1 = (highestVal - ohlc4) / highestVal;
		var fz1BandsVal = (BollingerBandsValue)_fz1Bands.Process(fz1);
		var inFz1 = _fz1Bands.IsFormed && fz1BandsVal.UpBand is decimal up1 && fz1 > up1;

		var fz2 = ohlc4 - smaVal;
		var fz2BandsVal = (BollingerBandsValue)_fz2Bands.Process(fz2);
		var inFz2 = _fz2Bands.IsFormed && fz2BandsVal.LowBand is decimal low2 && fz2 < low2;

		var isDown = rocVal <= -ImpulsePercent;

		var range = candle.HighPrice - candle.LowPrice;
		var inRicochet = range > 0 && candle.ClosePrice < candle.LowPrice + range * 0.1m;

		var isMagic = kValue < StochThreshold;
		var isAboveMa = candle.ClosePrice > maVal;

		if (inFz1 && inFz2 && isAboveMa && (isDown || inRicochet || isMagic) && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (Position > 0 && !isAboveMa)
		{
			SellMarket(Math.Abs(Position));
		}
	}
}
