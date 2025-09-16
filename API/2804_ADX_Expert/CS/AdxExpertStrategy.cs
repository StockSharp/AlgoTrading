using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX crossover strategy translated from the original MQL expert.
/// Opens a single position when DI lines cross while ADX remains weak.
/// </summary>
public class AdxExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx = null!;
	private decimal _previousPlusDi;
	private decimal _previousMinusDi;
	private bool _hasPreviousDi;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	/// <summary>
	/// Trading volume for every market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Maximum ADX level that still allows new trades.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Maximum allowed bid-ask spread measured in price points.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AdxExpertStrategy"/>.
	/// </summary>
	public AdxExpertStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Order volume used for entries", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX period", "Smoothing length for the ADX indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX threshold", "Upper ADX limit that allows trades", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(15m, 35m, 5m);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 20m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Max spread (points)", "Maximum allowed bid-ask spread in points", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 40m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 200m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop loss (points)", "Protective stop distance in price points", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 400m, 50m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take profit (points)", "Target distance in price points", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(200m, 600m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Type of candles used for ADX", "General");
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

		_previousPlusDi = 0m;
		_previousMinusDi = 0m;
		_hasPreviousDi = false;
		_bestBidPrice = null;
		_bestAskPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize ADX indicator with the selected period.
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };

		// Subscribe to candles and bind the ADX indicator to them.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		// Track best bid and ask prices to evaluate the current spread.
		SubscribeOrderBook()
			.Bind(depth =>
			{
				_bestBidPrice = depth.GetBestBid()?.Price ?? _bestBidPrice;
				_bestAskPrice = depth.GetBestAsk()?.Price ?? _bestAskPrice;
			})
			.Start();

		// Configure automatic stop-loss and take-profit handling.
		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null,
			stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null);

		// Visualize candles and indicator if charting is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Process only finished candles to match the original expert behavior.
		if (candle.State != CandleStates.Finished)
			return;

		if (adxValue is not AverageDirectionalIndexValue adxData)
			return;

		if (!adxValue.IsFinal)
			return;

		var plusDi = adxData.Dx.Plus;
		var minusDi = adxData.Dx.Minus;

		if (adxData.MovingAverage is not decimal currentAdx)
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			_hasPreviousDi = true;
			return;
		}

		if (!_hasPreviousDi)
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			_hasPreviousDi = true;
			return;
		}

		// Respect the maximum spread filter if it is enabled.
		if (MaxSpreadPoints > 0m)
		{
			var step = Security?.PriceStep ?? 1m;

			if (_bestBidPrice is not decimal bid || _bestAskPrice is not decimal ask)
			{
				_previousPlusDi = plusDi;
				_previousMinusDi = minusDi;
				return;
			}

			var spread = ask - bid;
			var maxAllowedSpread = MaxSpreadPoints * step;

			if (spread > maxAllowedSpread)
			{
				LogInfo($"Spread {spread:F5} is above the allowed {maxAllowedSpread:F5}. Waiting for tighter market.");
				_previousPlusDi = plusDi;
				_previousMinusDi = minusDi;
				return;
			}
		}

		// Make sure the strategy is ready for trading and connection is healthy.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousPlusDi = plusDi;
			_previousMinusDi = minusDi;
			return;
		}

		var bullishCross = _previousPlusDi <= _previousMinusDi && plusDi > minusDi;
		var bearishCross = _previousPlusDi >= _previousMinusDi && plusDi < minusDi;

		// Trade only when ADX indicates a ranging market.
		if (currentAdx < AdxThreshold && Position == 0)
		{
			if (bullishCross)
			{
				LogInfo($"Opening long position at {candle.ClosePrice} because +DI crossed above -DI.");
				BuyMarket(TradeVolume);
			}
			else if (bearishCross)
			{
				LogInfo($"Opening short position at {candle.ClosePrice} because +DI crossed below -DI.");
				SellMarket(TradeVolume);
			}
		}

		_previousPlusDi = plusDi;
		_previousMinusDi = minusDi;
	}
}
