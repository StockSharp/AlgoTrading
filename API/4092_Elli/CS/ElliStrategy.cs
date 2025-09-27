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
/// Ichimoku plus ADX trend-following strategy converted from the original MetaTrader 4 expert advisor "Elli".
/// Combines multi-timeframe directional filters with DI acceleration checks before entering trades.
/// </summary>
public class ElliStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<decimal> _tenkanKijunGapPips;
	private readonly StrategyParam<decimal> _convertHigh;
	private readonly StrategyParam<decimal> _convertLow;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _adxCandleType;

	private Ichimoku _ichimoku = null!;
	private AverageDirectionalIndex _adx = null!;

	private decimal _pipSize;
	private decimal? _latestPlusDi;
	private decimal? _previousPlusDi;
	private decimal? _latestMinusDi;
	private decimal? _previousMinusDi;
	private bool _hasAdxHistory;

	/// <summary>
	/// Initializes a new instance of the <see cref="ElliStrategy"/> class.
	/// </summary>
	public ElliStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Default market order volume", "Trading")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance to the protective take profit in pips", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop loss in pips", "Risk Management")
			.SetCanOptimize(true);

		_tenkanPeriod = Param(nameof(TenkanPeriod), 19)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period for the Ichimoku calculation", "Ichimoku")
			.SetCanOptimize(true);

		_kijunPeriod = Param(nameof(KijunPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period for the Ichimoku calculation", "Ichimoku")
			.SetCanOptimize(true);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B period for the Ichimoku cloud", "Ichimoku")
			.SetCanOptimize(true);

		_tenkanKijunGapPips = Param(nameof(TenkanKijunGapPips), 20m)
			.SetNotNegative()
			.SetDisplay("Tenkan-Kijun Gap (pips)", "Minimum Tenkan/Kijun distance in pips required for entries", "Ichimoku")
			.SetCanOptimize(true);

		_convertHigh = Param(nameof(ConvertHigh), 13m)
			.SetNotNegative()
			.SetDisplay("DI High Threshold", "Current DI value that confirms momentum expansion", "ADX")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_convertLow = Param(nameof(ConvertLow), 6m)
			.SetNotNegative()
			.SetDisplay("DI Low Threshold", "Previous DI value that must stay below this level", "ADX")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_adxPeriod = Param(nameof(AdxPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period used for the Average Directional Index", "ADX")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe driving the Ichimoku structure", "Timeframes");

		_adxCandleType = Param(nameof(AdxCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("ADX Candle Type", "Timeframe used for DI calculations", "Timeframes");
	}

	/// <summary>
	/// Default order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Tenkan-sen period of the Ichimoku indicator.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period of the Ichimoku indicator.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period of the Ichimoku indicator.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Minimum Tenkan/Kijun separation required to validate a signal.
	/// </summary>
	public decimal TenkanKijunGapPips
	{
		get => _tenkanKijunGapPips.Value;
		set => _tenkanKijunGapPips.Value = value;
	}

	/// <summary>
	/// Upper DI threshold that the current value must exceed.
	/// </summary>
	public decimal ConvertHigh
	{
		get => _convertHigh.Value;
		set => _convertHigh.Value = value;
	}

	/// <summary>
	/// Lower DI threshold that the previous value must stay below.
	/// </summary>
	public decimal ConvertLow
	{
		get => _convertLow.Value;
		set => _convertLow.Value = value;
	}

	/// <summary>
	/// Period used for the ADX calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Main candle type that drives the Ichimoku logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe used to compute the ADX and DI components.
	/// </summary>
	public DataType AdxCandleType
	{
		get => _adxCandleType.Value;
		set => _adxCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);

		if (AdxCandleType != CandleType)
			yield return (Security, AdxCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ichimoku = null!;
		_adx = null!;
		_latestPlusDi = null;
		_previousPlusDi = null;
		_latestMinusDi = null;
		_previousMinusDi = null;
		_hasAdxHistory = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		Volume = OrderVolume;

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		_adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		Unit stopLoss = StopLossPips > 0m && _pipSize > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: null;
		Unit takeProfit = TakeProfitPips > 0m && _pipSize > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(stopLoss: stopLoss, takeProfit: takeProfit, useMarketOrders: true);
		}

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription.BindEx(_ichimoku, ProcessIchimokuCandle);

		if (AdxCandleType == CandleType)
		{
			primarySubscription.BindEx(_adx, ProcessAdxCandle);
			primarySubscription.Start();
		}
		else
		{
			primarySubscription.Start();

			var adxSubscription = SubscribeCandles(AdxCandleType);
			adxSubscription.BindEx(_adx, ProcessAdxCandle).Start();
		}

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, primarySubscription);
			DrawIndicator(priceArea, _ichimoku);
			DrawOwnTrades(priceArea);
		}

		var adxArea = CreateChartArea();
		if (adxArea != null)
		{
			DrawIndicator(adxArea, _adx);
		}
	}

	private void ProcessAdxCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal)
			return;

		var typed = (AverageDirectionalIndexValue)adxValue;

		if (typed.PlusDi is not decimal plus || typed.MinusDi is not decimal minus)
			return;

		if (_latestPlusDi is decimal lastPlus && _latestMinusDi is decimal lastMinus)
		{
			_previousPlusDi = lastPlus;
			_previousMinusDi = lastMinus;
			_hasAdxHistory = true;
		}
		else
		{
			_previousPlusDi = plus;
			_previousMinusDi = minus;
		}

		_latestPlusDi = plus;
		_latestMinusDi = minus;
	}

	private void ProcessIchimokuCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasAdxHistory || _latestPlusDi is null || _previousPlusDi is null || _latestMinusDi is null || _previousMinusDi is null)
			return;

		if (Volume <= 0m)
			return;

		var typed = (IchimokuValue)ichimokuValue;

		if (typed.Tenkan is not decimal tenkan)
			return;
		if (typed.Kijun is not decimal kijun)
			return;
		if (typed.SenkouA is not decimal senkouA)
			return;
		if (typed.SenkouB is not decimal senkouB)
			return;

		var gap = Math.Abs(tenkan - kijun);
		var pipGap = _pipSize > 0m ? gap / _pipSize : gap;

		var closePrice = candle.ClosePrice;
		var plusDi = _latestPlusDi.Value;
		var prevPlusDi = _previousPlusDi.Value;
		var minusDi = _latestMinusDi.Value;
		var prevMinusDi = _previousMinusDi.Value;

		var bullishStructure = tenkan > kijun && kijun > senkouA && senkouA > senkouB && closePrice > kijun;
		var bearishStructure = tenkan < kijun && kijun < senkouA && senkouA < senkouB && closePrice < kijun;

		var longSignal = bullishStructure
			&& pipGap > TenkanKijunGapPips
			&& prevPlusDi < ConvertLow
			&& plusDi > ConvertHigh
			&& Position <= 0m;

		var shortSignal = bearishStructure
			&& pipGap > TenkanKijunGapPips
			&& prevMinusDi < ConvertLow
			&& minusDi > ConvertHigh
			&& Position >= 0m;

		if (longSignal)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortSignal)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			return 1m;

		var decimals = CountDecimals(priceStep);
		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 8)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
