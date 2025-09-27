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
/// YinYang RSI Volume Trend strategy.
/// Defines dynamic buy and sell zones using volume-weighted price and RSI.
/// Enters long when price crosses above the lower zone and short when crossing below the upper zone.
/// Optional stop-loss and take-profit are based on zone distances.
/// </summary>
public class YinYangRsiVolumeTrendStrategy : Strategy
{
	/// <summary>
	/// Options for resetting purchase availability.
	/// </summary>
	public enum ResetConditions
	{
		/// <summary>
		/// Reset after entry condition is met.
		/// </summary>
		Entry,
		/// <summary>
		/// Reset after stop-loss triggers.
		/// </summary>
		StopLoss,
		/// <summary>
		/// No automatic reset.
		/// </summary>
		None
	}

	public enum CandlePrices
	{
		Open,
		High,
		Low,
		Close,
		Median,
		Typical,
		Weighted
	}

	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<ResetConditions> _resetCondition;
	private readonly StrategyParam<CandlePrices> _purchaseSource;
	private readonly StrategyParam<CandlePrices> _exitSource;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private ExponentialMovingAverage _midEma = null!;
	private SimpleMovingAverage _avgVolume = null!;
	private VolumeWeightedMovingAverage _midVolVwma = null!;
	private SimpleMovingAverage _diffSma = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _prevPurchaseSrc;
	private decimal _prevExitSrc;
	private decimal _prevZoneHigh;
	private decimal _prevZoneLow;
	private decimal _prevZoneBasis;
	private decimal _prevStopHigh;
	private decimal _prevStopLow;
	private bool _initialized;
	private bool _longAvailable;
	private bool _longTakeProfitAvailable;
	private bool _longStopLoss;
	private bool _shortAvailable;
	private bool _shortTakeProfitAvailable;
	private bool _shortStopLoss;

	/// <summary>
	/// Trend calculation length.
	/// </summary>
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }

	/// <summary>
	/// Use take-profit logic.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Use stop-loss logic.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Stop-loss multiplier in percent.
	/// </summary>
	public decimal StopLossMultiplier { get => _stopLossMultiplier.Value; set => _stopLossMultiplier.Value = value; }

	/// <summary>
	/// Reset mode for purchase availability.
	/// </summary>
	public ResetConditions ResetCondition { get => _resetCondition.Value; set => _resetCondition.Value = value; }

	/// <summary>
	/// Price source for purchase checks.
	/// </summary>
	public CandlePrices PurchaseSource { get => _purchaseSource.Value; set => _purchaseSource.Value = value; }

	/// <summary>
	/// Price source for exit checks.
	/// </summary>
	public CandlePrices ExitSource { get => _exitSource.Value; set => _exitSource.Value = value; }

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="YinYangRsiVolumeTrendStrategy"/>.
	/// </summary>
	public YinYangRsiVolumeTrendStrategy()
	{
		_trendLength = Param(nameof(TrendLength), 80)
		.SetGreaterThanZero()
		.SetDisplay("Trend Length", "Lookback length for calculations", "General");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
		.SetDisplay("Use Take Profit", "Enable take profit logic", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable stop loss logic", "Risk");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Stoploss Multiplier %", "Distance from purchase lines", "Risk");

		_resetCondition = Param(nameof(ResetCondition), ResetConditions.Entry)
		.SetDisplay("Reset After", "When to reset purchase availability", "General");

		_purchaseSource = Param(nameof(PurchaseSource), CandlePrices.Close)
		.SetDisplay("Purchase Source", "Price source for entry", "General");

		_exitSource = Param(nameof(ExitSource), CandlePrices.Close)
		.SetDisplay("Exit Source", "Price source for exit", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe", "General");
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

		_prevPurchaseSrc = 0m;
		_prevExitSrc = 0m;
		_prevZoneHigh = 0m;
		_prevZoneLow = 0m;
		_prevZoneBasis = 0m;
		_prevStopHigh = 0m;
		_prevStopLow = 0m;
		_initialized = false;
		_longAvailable = false;
		_longTakeProfitAvailable = false;
		_longStopLoss = false;
		_shortAvailable = false;
		_shortTakeProfitAvailable = false;
		_shortStopLoss = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = TrendLength, CandlePrice = CandlePrices.High };
		_lowest = new Lowest { Length = TrendLength, CandlePrice = CandlePrices.Low };
		_midEma = new ExponentialMovingAverage { Length = TrendLength };
		_avgVolume = new SimpleMovingAverage { Length = TrendLength };
		_midVolVwma = new VolumeWeightedMovingAverage { Length = 3 };
		_diffSma = new SimpleMovingAverage { Length = TrendLength };
		_rsi = new RelativeStrengthIndex { Length = TrendLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mid = (highest + lowest) / 2m;
		var midSmoothed = _midEma.Process(mid, candle.OpenTime, true).ToDecimal();

		var avgVol = _avgVolume.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		var volDiff = avgVol == 0 ? 0 : candle.TotalVolume / avgVol;
		var midVol = midSmoothed * volDiff;
		var midVolSmoothed = _midVolVwma.Process(midVol, candle.OpenTime, true).ToDecimal();

		var diff = highest - lowest;
		var midDifference = _diffSma.Process(diff, candle.OpenTime, true).ToDecimal();
		var midRsi = _rsi.Process(midVolSmoothed, candle.OpenTime, true).ToDecimal() * 0.01m;
		var midAdd = midRsi * midDifference;

		var zoneHigh = midSmoothed + midAdd;
		var zoneLow = midSmoothed - midAdd;
		var zoneBasis = (zoneHigh + zoneLow) / 2m;
		var stopHigh = zoneHigh * (1 + StopLossMultiplier / 100m);
		var stopLow = zoneLow * (1 - StopLossMultiplier / 100m);

		var purchaseSrc = GetPrice(candle, PurchaseSource);
		var exitSrc = GetPrice(candle, ExitSource);

		if (!_initialized)
		{
			_prevPurchaseSrc = purchaseSrc;
			_prevExitSrc = exitSrc;
			_prevZoneHigh = zoneHigh;
			_prevZoneLow = zoneLow;
			_prevZoneBasis = zoneBasis;
			_prevStopHigh = stopHigh;
			_prevStopLow = stopLow;
			_initialized = true;
			_longAvailable = true;
			_shortAvailable = true;
			return;
		}

		var longEntry = CrossDown(_prevPurchaseSrc, _prevZoneLow, purchaseSrc, zoneLow);
		var longStart = CrossUp(_prevPurchaseSrc, _prevZoneLow, purchaseSrc, zoneLow) && _longAvailable;
		var longEnd = CrossUp(_prevExitSrc, _prevZoneHigh, exitSrc, zoneHigh);
		_longStopLoss = CrossDown(_prevExitSrc, _prevStopLow, exitSrc, stopLow);
		var crossAboveBasis = CrossUp(_prevExitSrc, _prevZoneBasis, exitSrc, zoneBasis);
		if (crossAboveBasis)
			_longTakeProfitAvailable = true;
		else if (longEnd)
			_longTakeProfitAvailable = false;
		var longTakeProfit = CrossDown(_prevExitSrc, _prevZoneBasis, exitSrc, zoneBasis) && _longTakeProfitAvailable;

		var longAvailReset = CrossDown(_prevPurchaseSrc, _prevZoneHigh, purchaseSrc, zoneHigh)
			|| (ResetCondition == ResetConditions.StopLoss && _longStopLoss)
			|| (ResetCondition == ResetConditions.Entry && longEntry);

		if (longAvailReset)
			_longAvailable = true;
		else if (longStart)
			_longAvailable = false;

		var shortEntry = CrossUp(_prevPurchaseSrc, _prevZoneHigh, purchaseSrc, zoneHigh);
		var shortStart = CrossDown(_prevPurchaseSrc, _prevZoneHigh, purchaseSrc, zoneHigh) && _shortAvailable;
		var shortEnd = CrossDown(_prevExitSrc, _prevZoneLow, exitSrc, zoneLow);
		_shortStopLoss = CrossUp(_prevExitSrc, _prevStopHigh, exitSrc, stopHigh);
		var crossUnderBasis = CrossDown(_prevExitSrc, _prevZoneBasis, exitSrc, zoneBasis);
		if (crossUnderBasis)
			_shortTakeProfitAvailable = true;
		else if (shortEnd)
			_shortTakeProfitAvailable = false;
		var shortTakeProfit = CrossUp(_prevExitSrc, _prevZoneBasis, exitSrc, zoneBasis) && _shortTakeProfitAvailable;

		var shortAvailReset = CrossUp(_prevPurchaseSrc, _prevZoneLow, purchaseSrc, zoneLow)
			|| (ResetCondition == ResetConditions.StopLoss && _shortStopLoss)
			|| (ResetCondition == ResetConditions.Entry && shortEntry);

		if (shortAvailReset)
			_shortAvailable = true;
		else if (shortStart)
			_shortAvailable = false;

		if (longStart && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && (longEnd || (UseStopLoss && _longStopLoss) || (UseTakeProfit && longTakeProfit)))
			SellMarket(Math.Abs(Position));

		if (shortStart && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position < 0 && (shortEnd || (UseStopLoss && _shortStopLoss) || (UseTakeProfit && shortTakeProfit)))
			BuyMarket(Math.Abs(Position));

		_prevPurchaseSrc = purchaseSrc;
		_prevExitSrc = exitSrc;
		_prevZoneHigh = zoneHigh;
		_prevZoneLow = zoneLow;
		_prevZoneBasis = zoneBasis;
		_prevStopHigh = stopHigh;
		_prevStopLow = stopLow;
	}

	private static bool CrossUp(decimal prevSrc, decimal prevLine, decimal src, decimal line)
	{
		return prevSrc <= prevLine && src > line;
	}

	private static bool CrossDown(decimal prevSrc, decimal prevLine, decimal src, decimal line)
	{
		return prevSrc >= prevLine && src < line;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrices price)
	{
		return price switch
		{
			CandlePrices.Open => candle.OpenPrice,
			CandlePrices.High => candle.HighPrice,
			CandlePrices.Low => candle.LowPrice,
			CandlePrices.Close => candle.ClosePrice,
			CandlePrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrices.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}