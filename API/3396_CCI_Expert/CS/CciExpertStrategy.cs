namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// CCI based crossover strategy translated from the original MQL expert.
/// Opens a single position when two recent values stay above the bullish threshold and the third latest was below it.
/// </summary>
public class CciExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private decimal? _previousCci;
	private decimal? _previousPreviousCci;
	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;

	/// <summary>
	/// Fixed order volume. Set to zero to enable risk based sizing.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used when <see cref="FixedVolume"/> is zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in price points. Set to zero to disable the filter.
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Period of the Commodity Channel Index indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CciExpertStrategy()
	{
		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Fixed volume", "Lot size used for trading when greater than zero", "Risk management")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 1m, 0.05m);

		_riskPercent = Param(nameof(RiskPercent), 0m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Risk percent", "Risk based sizing when fixed volume is zero", "Risk management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 10m, 1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take profit (points)", "Take-profit distance in price points", "Risk management")
		.SetCanOptimize(true)
		.SetOptimize(50m, 300m, 25m);

		_stopLossPoints = Param(nameof(StopLossPoints), 600m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop loss (points)", "Stop-loss distance in price points", "Risk management")
		.SetCanOptimize(true)
		.SetOptimize(100m, 800m, 50m);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 30m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Max spread (points)", "Maximum allowed bid-ask spread in points", "Trading filters")
		.SetCanOptimize(true)
		.SetOptimize(10m, 60m, 5m);

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI period", "Lookback period for the Commodity Channel Index", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle type", "Time frame used for calculations", "Indicator");
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

		_previousCci = null;
		_previousPreviousCci = null;
		_bestBidPrice = null;
		_bestAskPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBidPrice = depth.GetBestBid()?.Price ?? _bestBidPrice;
			_bestAskPrice = depth.GetBestAsk()?.Price ?? _bestAskPrice;
		})
		.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
		takeProfit: TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null,
		stopLoss: StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_cci != null)
			{
				DrawIndicator(area, _cci);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_cci == null || !_cci.IsFormed)
		{
			UpdateHistory(cciValue);
			return;
		}

		var previous = _previousCci;
		var beforePrevious = _previousPreviousCci;

		UpdateHistory(cciValue);

		if (previous is not decimal prev || beforePrevious is not decimal prev2)
		return;

		var longSignal = cciValue > 1m && prev > 1m && prev2 < 1m;
		var shortSignal = cciValue < 1m && prev < 1m && prev2 > 1m;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsSpreadAcceptable())
		return;

		if (Position == 0)
		{
			if (longSignal)
			{
				var volume = CalculateVolume();
				if (volume > 0m)
				{
					LogInfo($"Opening long position at {candle.ClosePrice:F5} because CCI stayed above the bullish level while the prior value was below.");
					BuyMarket(volume);
				}
			}
			else if (shortSignal)
			{
				var volume = CalculateVolume();
				if (volume > 0m)
				{
					LogInfo($"Opening short position at {candle.ClosePrice:F5} because CCI stayed below the bearish level while the prior value was above.");
					SellMarket(volume);
				}
			}
		}
		else if (Position > 0)
		{
			if (shortSignal && PositionPrice is decimal entryPrice && candle.ClosePrice > entryPrice)
			{
				LogInfo($"Closing long position at {candle.ClosePrice:F5} after bearish confirmation with profitable exit.");
				ClosePosition();
			}
		}
		else if (Position < 0)
		{
			if (longSignal && PositionPrice is decimal entryPrice && candle.ClosePrice < entryPrice)
			{
				LogInfo($"Closing short position at {candle.ClosePrice:F5} after bullish confirmation with profitable exit.");
				ClosePosition();
			}
		}
	}

	private void UpdateHistory(decimal currentValue)
	{
		_previousPreviousCci = _previousCci;
		_previousCci = currentValue;
	}

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPoints <= 0m)
		return true;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return true;

		if (_bestBidPrice is not decimal bid || _bestAskPrice is not decimal ask)
		return false;

		var spread = ask - bid;
		var maxAllowedSpread = MaxSpreadPoints * step;

		if (spread > maxAllowedSpread)
		{
			LogInfo($"Spread {spread:F5} is above the allowed {maxAllowedSpread:F5}. Waiting for tighter market.");
			return false;
		}

		return true;
	}

	private decimal CalculateVolume()
	{
		if (FixedVolume > 0m)
		return AdjustVolume(FixedVolume);

		var step = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;

		if (RiskPercent <= 0m || StopLossPoints <= 0m || step <= 0m || stepPrice <= 0m)
		return AdjustVolume(Volume);

		var stopDistance = StopLossPoints * step;
		if (stopDistance <= 0m)
		return AdjustVolume(Volume);

		var lossPerUnit = stopDistance / step * stepPrice;
		if (lossPerUnit <= 0m)
		return AdjustVolume(Volume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
		return AdjustVolume(Volume);

		var riskAmount = equity * RiskPercent / 100m;
		if (riskAmount <= 0m)
		return AdjustVolume(Volume);

		var volume = riskAmount / lossPerUnit;
		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.VolumeMin ?? step;
		var max = security.VolumeMax ?? decimal.MaxValue;

		if (step > 0m)
		volume = Math.Floor(volume / step) * step;

		if (min > 0m && volume < min)
		volume = min;

		if (volume > max)
		volume = max;

		return volume;
	}
}

