using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Squeeze Momentum Indicator strategy.
/// Detects volatility squeeze using Bollinger Bands and Keltner Channels
/// and trades momentum breakouts filtered by EMA.
/// </summary>
public class SqueezeMomentumIndicatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _kcLength;
	private readonly StrategyParam<decimal> _kcMultiplier;
	private readonly StrategyParam<int> _emaLength;

	private BollingerBands _bb;
	private KeltnerChannels _kc;
	private Highest _highestHigh;
	private Lowest _lowestLow;
	private SMA _smaClose;
	private LinearRegression _linReg;
	private EMA _emaTrend;
	private Lowest _lowestVal;
	private Highest _highestVal;

	private decimal _prevVal;
	private decimal _prevClose;
	private bool _sqzOnPrev;
	private bool _sqzOffPrev;
	private decimal _prevLowestVal;
	private decimal _prevHighestVal;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Keltner Channels period.
	/// </summary>
	public int KcLength
	{
		get => _kcLength.Value;
		set => _kcLength.Value = value;
	}

	/// <summary>
	/// Keltner Channels multiplier.
	/// </summary>
	public decimal KcMultiplier
	{
		get => _kcMultiplier.Value;
		set => _kcMultiplier.Value = value;
	}

	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SqueezeMomentumIndicatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Squeeze")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetDisplay("BB Mult", "Bollinger Bands multiplier", "Squeeze");

		_kcLength = Param(nameof(KcLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("KC Length", "Keltner Channels period", "Squeeze")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_kcMultiplier = Param(nameof(KcMultiplier), 1.5m)
			.SetDisplay("KC Mult", "Keltner Channels multiplier", "Squeeze");

		_emaLength = Param(nameof(EmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA period", "Trend")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 25);
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

		_prevVal = default;
		_prevClose = default;
		_sqzOnPrev = false;
		_sqzOffPrev = false;
		_prevLowestVal = default;
		_prevHighestVal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bb = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		_kc = new KeltnerChannels { Length = KcLength, Multiplier = KcMultiplier };
		_highestHigh = new Highest { Length = KcLength };
		_lowestLow = new Lowest { Length = KcLength };
		_smaClose = new SMA { Length = KcLength };
		_linReg = new LinearRegression { Length = KcLength };
		_emaTrend = new EMA { Length = EmaLength };
		_lowestVal = new Lowest { Length = 100 };
		_highestVal = new Highest { Length = 100 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bbVal = _bb.Process(candle);
		var kcVal = _kc.Process(candle);

		if (!_bb.IsFormed || !_kc.IsFormed)
			return;

		var bb = (BollingerBandsValue)bbVal;
		var kc = (KeltnerChannelsValue)kcVal;

		if (bb.UpBand is not decimal bbUpper ||
			bb.LowBand is not decimal bbLower ||
			kc.Upper is not decimal kcUpper ||
			kc.Lower is not decimal kcLower)
			return;

		var highVal = _highestHigh.Process(candle.HighPrice).ToDecimal();
		var lowVal = _lowestLow.Process(candle.LowPrice).ToDecimal();
		var smaVal = _smaClose.Process(candle.ClosePrice).ToDecimal();
		var emaVal = _emaTrend.Process(candle.ClosePrice).ToDecimal();

		if (!_highestHigh.IsFormed || !_lowestLow.IsFormed || !_smaClose.IsFormed || !_emaTrend.IsFormed)
			return;

		var hlAvg = (highVal + lowVal) / 2m;
		var baseAvg = (hlAvg + smaVal) / 2m;
		var diff = candle.ClosePrice - baseAvg;
		var lrVal = _linReg.Process(diff);

		if (!_linReg.IsFormed)
			return;

		var currentVal = ((LinearRegressionValue)lrVal).LinearReg ?? 0m;
		var lowMomentum = _lowestVal.Process(currentVal).ToDecimal();
		var highMomentum = _highestVal.Process(currentVal).ToDecimal();

		var sqzOn = bbLower > kcLower && bbUpper < kcUpper;
		var sqzOff = bbLower < kcLower && bbUpper > kcUpper;

		if (sqzOff && _sqzOnPrev &&
			currentVal > _prevVal &&
			currentVal > _prevLowestVal &&
			candle.ClosePrice > _prevClose &&
			candle.ClosePrice > emaVal &&
			Position <= 0)
		{
			BuyMarket();
		}
		else if (sqzOn && _sqzOffPrev &&
			currentVal < _prevVal &&
			currentVal < _prevHighestVal &&
			candle.ClosePrice < _prevClose &&
			candle.ClosePrice < emaVal &&
			Position >= 0)
		{
			SellMarket();
		}

		if (Position > 0 && currentVal < _prevVal)
		{
			SellMarket();
		}
		else if (Position < 0 && currentVal > _prevVal)
		{
			BuyMarket();
		}

		_sqzOnPrev = sqzOn;
		_sqzOffPrev = sqzOff;
		_prevVal = currentVal;
		_prevClose = candle.ClosePrice;
		_prevLowestVal = lowMomentum;
		_prevHighestVal = highMomentum;
	}
}

