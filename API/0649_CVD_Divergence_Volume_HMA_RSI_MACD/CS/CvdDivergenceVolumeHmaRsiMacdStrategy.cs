namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

public class CvdDivergenceVolumeHmaRsiMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _hma20Length;
	private readonly StrategyParam<int> _hma50Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _cvdLength;
	private readonly StrategyParam<int> _divergenceLookback;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _cvdSma;
	private Highest _priceHigh;
	private Lowest _priceLow;
	private Highest _cvdHigh;
	private Lowest _cvdLow;

	private decimal? _lastPriceHigh;
	private decimal? _lastCvdHigh;
	private decimal? _lastPriceLow;
	private decimal? _lastCvdLow;
	private decimal _prevCvd;
	private decimal _prevMacdHist;

	/// HMA 20 length.
	public int Hma20Length { get => _hma20Length.Value; set => _hma20Length.Value = value; }

	/// HMA 50 length.
	public int Hma50Length { get => _hma50Length.Value; set => _hma50Length.Value = value; }

	/// RSI length.
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// RSI overbought level.
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// RSI oversold level.
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// MACD fast period.
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// MACD slow period.
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// MACD signal period.
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// Volume MA length.
	public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }

	/// Volume multiplier.
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// CVD smoothing length.
	public int CvdLength { get => _cvdLength.Value; set => _cvdLength.Value = value; }

	/// Divergence lookback bars.
	public int DivergenceLookback { get => _divergenceLookback.Value; set => _divergenceLookback.Value = value; }

	/// Initializes a new instance of the strategy.
	public CvdDivergenceVolumeHmaRsiMacdStrategy()
	{
		_hma20Length = Param(nameof(Hma20Length), 20)
		.SetDisplay("HMA 20 Length", "Fast Hull MA period", "Parameters")
		.SetCanOptimize(true);

		_hma50Length = Param(nameof(Hma50Length), 50)
		.SetDisplay("HMA 50 Length", "Slow Hull MA period", "Parameters")
		.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Relative Strength Index period", "Parameters")
		.SetCanOptimize(true);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
		.SetDisplay("RSI Overbought", "Upper RSI level", "Parameters")
		.SetCanOptimize(true);

		_rsiOversold = Param(nameof(RsiOversold), 30)
		.SetDisplay("RSI Oversold", "Lower RSI level", "Parameters")
		.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("MACD Fast", "MACD fast period", "Parameters")
		.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("MACD Slow", "MACD slow period", "Parameters")
		.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("MACD Signal", "MACD signal period", "Parameters")
		.SetCanOptimize(true);

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
		.SetDisplay("Volume MA Length", "Volume moving average period", "Parameters")
		.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
		.SetDisplay("Volume Multiplier", "High volume threshold multiplier", "Parameters")
		.SetCanOptimize(true);

		_cvdLength = Param(nameof(CvdLength), 14)
		.SetDisplay("CVD Length", "CVD smoothing period", "Parameters")
		.SetCanOptimize(true);

		_divergenceLookback = Param(nameof(DivergenceLookback), 5)
		.SetDisplay("Divergence Lookback", "Bars for pivot detection", "Parameters")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var hma20 = new HullMovingAverage { Length = Hma20Length };
		var hma50 = new HullMovingAverage { Length = Hma50Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFast,
			LongPeriod = MacdSlow,
			SignalPeriod = MacdSignal
		};

		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		_cvdSma = new SimpleMovingAverage { Length = CvdLength };
		_priceHigh = new Highest { Length = DivergenceLookback };
		_priceLow = new Lowest { Length = DivergenceLookback };
		_cvdHigh = new Highest { Length = DivergenceLookback };
		_cvdLow = new Lowest { Length = DivergenceLookback };
		_prevCvd = 0m;
		_prevMacdHist = 0m;
		_lastPriceHigh = null;
		_lastCvdHigh = null;
		_lastPriceLow = null;
		_lastCvdLow = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(hma20, hma50, rsi, macd, _volumeSma, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal hma20, decimal hma50, decimal rsi,
		decimal macdLine, decimal macdSignal, decimal macdHist, decimal volumeMa)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var direction = candle.ClosePrice > candle.OpenPrice ? 1m : candle.ClosePrice < candle.OpenPrice ? -1m : 0m;
		var cvdRaw = candle.TotalVolume * direction;
		var cvd = _cvdSma.Process(cvdRaw, candle.OpenTime, true).ToDecimal();

		var priceHigh = _priceHigh.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var priceLow = _priceLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();
		var cvdHigh = _cvdHigh.Process(cvd, candle.OpenTime, true).ToDecimal();
		var cvdLow = _cvdLow.Process(cvd, candle.OpenTime, true).ToDecimal();

		var pricePivotHigh = priceHigh == candle.HighPrice;
		var pricePivotLow = priceLow == candle.LowPrice;
		var cvdPivotHigh = cvdHigh == cvd;
		var cvdPivotLow = cvdLow == cvd;

		var bullishDiv = false;
		if (pricePivotLow && cvdPivotLow)
		{
			if (_lastPriceLow is decimal lp && _lastCvdLow is decimal lc && candle.LowPrice < lp && cvd > lc)
			bullishDiv = true;

			_lastPriceLow = candle.LowPrice;
			_lastCvdLow = cvd;
		}

		var bearishDiv = false;
		if (pricePivotHigh && cvdPivotHigh)
		{
			if (_lastPriceHigh is decimal hp && _lastCvdHigh is decimal hc && candle.HighPrice > hp && cvd < hc)
			bearishDiv = true;

			_lastPriceHigh = candle.HighPrice;
			_lastCvdHigh = cvd;
		}

		var highVolume = candle.TotalVolume > volumeMa * VolumeMultiplier;
		var cvdBullish = bullishDiv || cvd > _prevCvd;
		var cvdBearish = bearishDiv || cvd < _prevCvd;

		var hmaBullish = hma20 > hma50 && candle.ClosePrice > hma20;
		var hmaBearish = hma20 < hma50 && candle.ClosePrice < hma20;
		var rsiBullish = rsi < RsiOverbought && rsi > 40m;
		var rsiBearish = rsi > RsiOversold && rsi < 60m;
		var macdBullish = macdLine > macdSignal && macdHist > _prevMacdHist;
		var macdBearish = macdLine < macdSignal && macdHist < _prevMacdHist;

		var longCondition = hmaBullish && rsiBullish && macdBullish && highVolume && cvdBullish;
		var shortCondition = hmaBearish && rsiBearish && macdBearish && highVolume && cvdBearish;

		if (Position == 0)
		{
			if (longCondition)
			BuyMarket();
			else if (shortCondition)
			SellMarket();
		}
		else if (Position > 0 && (candle.ClosePrice < hma20 || rsi > RsiOverbought || macdLine < macdSignal))
		{
			SellMarket();
		}
		else if (Position < 0 && (candle.ClosePrice > hma20 || rsi < RsiOversold || macdLine > macdSignal))
		{
			BuyMarket();
		}

		_prevCvd = cvd;
		_prevMacdHist = macdHist;
	}
}

