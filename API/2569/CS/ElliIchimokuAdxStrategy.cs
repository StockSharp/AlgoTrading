using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MQL5 strategy "Elli" combining Ichimoku and ADX filters.
/// Focuses on impulsive moves confirmed by +DI acceleration and Ichimoku line alignment.
/// </summary>
public class ElliIchimokuAdxStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _plusDiHighThreshold;
	private readonly StrategyParam<decimal> _plusDiLowThreshold;
	private readonly StrategyParam<decimal> _baselineDistanceThreshold;
	private readonly StrategyParam<DataType> _ichimokuCandleType;
	private readonly StrategyParam<DataType> _adxCandleType;

	private Ichimoku _ichimoku;
	private AverageDirectionalIndex _adx;

	private decimal? _previousPlusDi;
	private decimal? _currentPlusDi;
	private bool _isAdxReady;

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Tenkan-sen (conversion line) period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen (base line) period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold for +DI breakout confirmation.
	/// </summary>
	public decimal PlusDiHighThreshold
	{
		get => _plusDiHighThreshold.Value;
		set => _plusDiHighThreshold.Value = value;
	}

	/// <summary>
	/// Lower threshold that previous +DI must stay below before breakout.
	/// </summary>
	public decimal PlusDiLowThreshold
	{
		get => _plusDiLowThreshold.Value;
		set => _plusDiLowThreshold.Value = value;
	}

	/// <summary>
	/// Required Tenkan/Kijun separation measured in price steps.
	/// </summary>
	public decimal BaselineDistanceThreshold
	{
		get => _baselineDistanceThreshold.Value;
		set => _baselineDistanceThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used for Ichimoku evaluation and trading decisions.
	/// </summary>
	public DataType IchimokuCandleType
	{
		get => _ichimokuCandleType.Value;
		set => _ichimokuCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for ADX calculation.
	/// </summary>
	public DataType AdxCandleType
	{
		get => _adxCandleType.Value;
		set => _adxCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ElliIchimokuAdxStrategy"/>.
	/// </summary>
	public ElliIchimokuAdxStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 60m)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk Management")
			.SetGreaterThanOrEqual(0m);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk Management")
			.SetGreaterThanOrEqual(0m);

		_tenkanPeriod = Param(nameof(TenkanPeriod), 19)
			.SetDisplay("Tenkan Period", "Tenkan-sen (conversion line) length", "Ichimoku")
			.SetGreaterThanZero();

		_kijunPeriod = Param(nameof(KijunPeriod), 60)
			.SetDisplay("Kijun Period", "Kijun-sen (base line) length", "Ichimoku")
			.SetGreaterThanZero();

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 120)
			.SetDisplay("Senkou Span B Period", "Senkou Span B length", "Ichimoku")
			.SetGreaterThanZero();

		_adxPeriod = Param(nameof(AdxPeriod), 10)
			.SetDisplay("ADX Period", "Average Directional Index period", "ADX")
			.SetGreaterThanZero();

		_plusDiHighThreshold = Param(nameof(PlusDiHighThreshold), 13m)
			.SetDisplay("+DI High Threshold", "Level current +DI must exceed", "ADX")
			.SetGreaterThanZero();

		_plusDiLowThreshold = Param(nameof(PlusDiLowThreshold), 6m)
			.SetDisplay("+DI Low Threshold", "Level previous +DI must stay below", "ADX")
			.SetGreaterThanOrEqual(0m);

		_baselineDistanceThreshold = Param(nameof(BaselineDistanceThreshold), 20m)
			.SetDisplay("Baseline Distance", "Minimum Tenkan/Kijun spread in steps", "Ichimoku")
			.SetGreaterThanOrEqual(0m);

		_ichimokuCandleType = Param(nameof(IchimokuCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Ichimoku Candle", "Candle series for Ichimoku", "General");

		_adxCandleType = Param(nameof(AdxCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("ADX Candle", "Candle series for ADX", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, IchimokuCandleType);

		if (AdxCandleType != IchimokuCandleType)
			yield return (Security, AdxCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousPlusDi = null;
		_currentPlusDi = null;
		_isAdxReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var ichimokuSubscription = SubscribeCandles(IchimokuCandleType);
		ichimokuSubscription.BindEx(_ichimoku, ProcessIchimoku);

		if (AdxCandleType == IchimokuCandleType)
		{
			ichimokuSubscription.BindEx(_adx, ProcessAdx);
			ichimokuSubscription.Start();
		}
		else
		{
			ichimokuSubscription.Start();

			var adxSubscription = SubscribeCandles(AdxCandleType);
			adxSubscription.BindEx(_adx, ProcessAdx).Start();
		}

		if (TakeProfitPoints > 0m || StopLossPoints > 0m)
		{
			StartProtection(
				takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Step) : null,
				stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Step) : null,
				isStopTrailing: false,
				useMarketOrders: true);
		}

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, ichimokuSubscription);
			DrawIndicator(priceArea, _ichimoku);
			DrawOwnTrades(priceArea);
		}

		var adxArea = CreateChartArea();
		if (adxArea != null)
		{
			DrawIndicator(adxArea, _adx);
		}
	}

	private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typed = (AverageDirectionalIndexValue)adxValue;

		if (typed.Dx.Plus is not decimal plusDi)
			return;

		_previousPlusDi = _currentPlusDi;
		_currentPlusDi = plusDi;
		_isAdxReady = typed.MovingAverage is decimal;
	}

	private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_currentPlusDi is not decimal currentPlus || _previousPlusDi is not decimal previousPlus)
			return;

		if (!_isAdxReady)
			return;

		var ich = (IchimokuValue)ichimokuValue;

		if (ich.Tenkan is not decimal tenkan ||
			ich.Kijun is not decimal kijun ||
			ich.SenkouA is not decimal senkouA ||
			ich.SenkouB is not decimal senkouB)
		{
			return;
		}

		var priceStep = Security?.PriceStep ?? 1m;
		if (priceStep <= 0m)
			priceStep = 1m;

		var baselineDistance = Math.Abs(tenkan - kijun) / priceStep;
		var hasPlusDiBreakout = previousPlus < PlusDiLowThreshold && currentPlus > PlusDiHighThreshold;

		if (!hasPlusDiBreakout)
			return;

		if (baselineDistance < BaselineDistanceThreshold)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var priceAboveCloud = senkouA > senkouB && kijun > senkouA && tenkan > kijun && candle.ClosePrice > kijun;
		var priceBelowCloud = senkouA < senkouB && kijun < senkouA && tenkan < kijun && candle.ClosePrice < kijun;

		if (priceAboveCloud)
		{
			LogInfo($"Bullish signal: Tenkan {tenkan:F2} > Kijun {kijun:F2}, cloud rising, +DI from {previousPlus:F2} to {currentPlus:F2}.");
			BuyMarket(Volume);
		}
		else if (priceBelowCloud)
		{
			LogInfo($"Bearish signal: Tenkan {tenkan:F2} < Kijun {kijun:F2}, cloud falling, +DI from {previousPlus:F2} to {currentPlus:F2}.");
			SellMarket(Volume);
		}
	}
}
