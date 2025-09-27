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
/// Pending breakout strategy converted from the MetaTrader 4 expert advisor "Expert610".
/// Places symmetric buy stop and sell stop orders around the previous candle after volatility filters pass.
/// </summary>
public class Expert610BreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _roundingDigits;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _minimumVolume;
	private readonly StrategyParam<decimal> _thresholdPips;
	private readonly StrategyParam<decimal> _breakoutOffsetPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="Expert610BreakoutStrategy"/> class.
	/// </summary>
	public Expert610BreakoutStrategy()
	{
		_roundingDigits = Param(nameof(RoundingDigits), 2)
			.SetNotNegative()
			.SetDisplay("Rounding Digits", "Number of decimal places used when rounding risk calculations.", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Risk Percent", "Portion of free capital allocated to a single trade.", "Risk")
			.SetCanOptimize(true);

		_minimumVolume = Param(nameof(MinimumVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Volume", "Absolute lower bound for any pending order volume.", "Orders");

		_thresholdPips = Param(nameof(ThresholdPips), 5m)
			.SetNotNegative()
			.SetDisplay("Volatility Threshold (pips)", "Minimum distance between last close and the previous candle extremes.", "Setup")
			.SetCanOptimize(true);

		_breakoutOffsetPips = Param(nameof(BreakoutOffsetPips), 2m)
			.SetNotNegative()
			.SetDisplay("Breakout Offset (pips)", "Additional buffer added to the previous candle high/low when placing stops.", "Setup")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective distance applied after a pending order is triggered.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance applied after execution.", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to evaluate the previous bar breakout pattern.", "Data");

		_previousCandle = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_bestBid = null;
		_bestAsk = null;
		_pipSize = 0m;
	}

	/// <summary>
	/// Number of digits used when rounding monetary values.
	/// </summary>
	public int RoundingDigits
	{
		get => _roundingDigits.Value;
		set => _roundingDigits.Value = value;
	}

	/// <summary>
	/// Percentage of the available capital that may be risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Minimum order volume allowed for pending entries.
	/// </summary>
	public decimal MinimumVolume
	{
		get => _minimumVolume.Value;
		set => _minimumVolume.Value = value;
	}

	/// <summary>
	/// Required price distance in pips before breakout orders are staged.
	/// </summary>
	public decimal ThresholdPips
	{
		get => _thresholdPips.Value;
		set => _thresholdPips.Value = value;
	}

	/// <summary>
	/// Buffer added to the previous high or low before placing a stop order.
	/// </summary>
	public decimal BreakoutOffsetPips
	{
		get => _breakoutOffsetPips.Value;
		set => _breakoutOffsetPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used when calculating breakout levels.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);
		yield return (Security, DataType.OrderBook);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_bestBid = null;
		_bestAsk = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stopLossDistance = ConvertPipsToPrice(StopLossPips);
		var takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);

		if (stopLossDistance > 0m || takeProfitDistance > 0m)
		{
			StartProtection(
				stopLoss: stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : null,
				takeProfit: takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : null);
		}

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();

		SubscribeOrderBook()
		.Bind(depth =>
		{
			_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
			_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
		})
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_previousCandle == null)
		{
			// Store the very first completed candle as a reference for the next iteration.
			_previousCandle = candle;
			return;
		}

		_pipSize = _pipSize > 0m ? _pipSize : CalculatePipSize();
		if (_pipSize <= 0m)
		{
			// Without a pip size the lot calculation cannot mirror the original MetaTrader logic.
			_previousCandle = candle;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCandle = candle;
			return;
		}

		if (IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder))
		{
			// Respect the original EA behaviour: only one pair of pending orders may exist at a time.
			_previousCandle = candle;
			return;
		}

		var previousHigh = _previousCandle.HighPrice;
		var previousLow = _previousCandle.LowPrice;
		var currentOpen = candle.OpenPrice;
		var currentClose = candle.ClosePrice;

		var thresholdDistance = ConvertPipsToPrice(ThresholdPips);
		var breakoutOffset = ConvertPipsToPrice(BreakoutOffsetPips);
		var stopLossDistance = ConvertPipsToPrice(StopLossPips);
		var spread = CalculateSpread();

		var distanceToHigh = previousHigh - currentClose;
		var distanceToLow = currentClose - previousLow;

		if (distanceToHigh >= thresholdDistance && distanceToLow >= thresholdDistance && currentOpen < previousHigh)
		{
			// Place a buy stop above the previous high with a configurable breakout buffer and spread compensation.
			var entryPrice = NormalizePrice(previousHigh + breakoutOffset + spread);
			var volume = CalculateOrderVolume(stopLossDistance);

			if (volume > 0m)
				_buyStopOrder = BuyStop(volume, entryPrice);
		}

		if (distanceToHigh >= thresholdDistance && distanceToLow >= thresholdDistance && currentOpen > previousLow)
		{
			// Place a sell stop below the previous low using the same volume as the buy side.
			var entryPrice = NormalizePrice(previousLow - breakoutOffset);
			var volume = CalculateOrderVolume(stopLossDistance);

			if (volume > 0m)
				_sellStopOrder = SellStop(volume, entryPrice);
		}

		_previousCandle = candle;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && IsOrderFinal(order))
			_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && IsOrderFinal(order))
			_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelOrderIfActive(_buyStopOrder);
		CancelOrderIfActive(_sellStopOrder);
	}

	private decimal CalculateOrderVolume(decimal stopLossDistance)
	{
		var minVolume = MinimumVolume;

		var security = Security;
		if (security != null)
		{
			var securityMin = security.MinVolume ?? 0m;
			if (securityMin > minVolume)
				minVolume = securityMin;
		}

		if (stopLossDistance <= 0m)
			return NormalizeVolume(minVolume);

		var riskPercent = RiskPercent;
		if (riskPercent <= 0m)
			return NormalizeVolume(minVolume);

		var portfolio = Portfolio;
		var currentValue = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		var blockedValue = portfolio?.BlockedValue ?? 0m;
		var freeCapital = Math.Max(currentValue - blockedValue, 0m);

		if (freeCapital <= 0m)
			return NormalizeVolume(minVolume);

		var digits = Math.Max(0, RoundingDigits);
		var riskAmount = Math.Round(freeCapital * riskPercent / 100m, digits, MidpointRounding.AwayFromZero);

		if (riskAmount <= 0m)
			return NormalizeVolume(minVolume);

		var pipSize = _pipSize > 0m ? _pipSize : CalculatePipSize();
		if (pipSize <= 0m)
			return NormalizeVolume(minVolume);

		var stopPips = stopLossDistance / pipSize;
		if (stopPips <= 0m)
			return NormalizeVolume(minVolume);

		var lot = riskAmount / stopPips * 0.1m;
		lot = Math.Round(lot, digits, MidpointRounding.AwayFromZero);

		if (lot <= 0m)
			return NormalizeVolume(minVolume);

		return NormalizeVolume(lot);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		var minVolume = Math.Max(MinimumVolume, security?.MinVolume ?? 0m);
		var maxVolume = security?.MaxVolume ?? 0m;
		var volumeStep = security?.VolumeStep ?? 0m;

		if (volumeStep > 0m)
		{
			var steps = Math.Round(volume / volumeStep, 0, MidpointRounding.AwayFromZero);
			var minSteps = minVolume > 0m ? Math.Ceiling(minVolume / volumeStep) : 0m;
			if (steps < minSteps)
				steps = minSteps;

			var maxSteps = maxVolume > 0m ? Math.Floor(maxVolume / volumeStep) : (decimal?)null;
			if (maxSteps.HasValue && steps > maxSteps.Value)
				steps = Math.Max(1m, maxSteps.Value);

			if (steps <= 0m)
				steps = 1m;

			volume = steps * volumeStep;
		}

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal CalculateSpread()
	{
		if (_bestBid is decimal bid && _bestAsk is decimal ask && bid > 0m && ask >= bid)
			return ask - bid;

		return 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		var steps = Math.Round(price / step, 0, MidpointRounding.AwayFromZero);
		return steps * step;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.MinStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = security.Decimals ?? 0;
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		var pipSize = _pipSize > 0m ? _pipSize : CalculatePipSize();
		return pipSize > 0m ? pipSize * pips : pips;
	}

	private static bool IsOrderActive(Order order)
	{
		return order is not null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
	}

	private static bool IsOrderFinal(Order order)
	{
		return order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order is not null && order.State == OrderStates.Active)
			CancelOrder(order);
	}
}
