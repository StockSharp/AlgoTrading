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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Graal-003" that combines fractal breakouts with adaptive channel filters.
/// Uses fractal extremes as triggers, validates them against custom Donchian-like envelopes, and optionally closes trades via Williams %R.
/// Counter-trend stop orders can be staged automatically to mirror the original hedging behaviour.
/// </summary>
public class GraalFractalChannelStrategy : Strategy
{

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _offsetPips;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<bool> _useFractalChannel;
	private readonly StrategyParam<decimal> _depthPercent;
	private readonly StrategyParam<bool> _useHighLowChannel;
	private readonly StrategyParam<decimal> _orientationPercent;
	private readonly StrategyParam<bool> _allowFlatTrading;
	private readonly StrategyParam<decimal> _flatThresholdPips;
	private readonly StrategyParam<bool> _useWilliamsExit;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _williamsThreshold;
	private readonly StrategyParam<bool> _useCounterOrders;
	private readonly StrategyParam<bool> _singlePosition;
	private readonly StrategyParam<int> _signalAgeLimit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<ICandleMessage> _channelHistory = new();

	private WilliamsPercentRange _williams = null!;

	private ICandleMessage _c1;
	private ICandleMessage _c2;
	private ICandleMessage _c3;
	private ICandleMessage _c4;
	private ICandleMessage _c5;

	private decimal? _lastUpperFractalPrice;
	private decimal? _lastLowerFractalPrice;
	private ICandleMessage _lastUpperFractalCandle;
	private ICandleMessage _lastLowerFractalCandle;
	private int _upperFractalAge = int.MaxValue;
	private int _lowerFractalAge = int.MaxValue;
	private bool _pendingLongSignal;
	private bool _pendingShortSignal;

	private Order _buyStopOrder;
	private Order _sellStopOrder;

	private decimal _pipSize;
	private decimal? _lastWilliamsValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="GraalFractalChannelStrategy"/> class.
	/// </summary>
	public GraalFractalChannelStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetDisplay("Order Volume", "Base size of market orders and hedging stops.", "General")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 500m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance applied through portfolio protection.", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance applied through portfolio protection.", "Risk")
			.SetNotNegative();

		_offsetPips = Param(nameof(OffsetPips), 5m)
			.SetDisplay("Counter Stop Offset (pips)", "Distance for hedging stop orders relative to fractal levels.", "Orders")
			.SetNotNegative();

		_channelPeriod = Param(nameof(ChannelPeriod), 14)
			.SetDisplay("Channel Period", "Number of candles considered when building the close-price channel.", "Filters")
			.SetGreaterThanZero();

		_useFractalChannel = Param(nameof(UseFractalChannel), false)
			.SetDisplay("Use Fractal Channel", "Require price to stay within the inner fractal channel before entries.", "Filters");

		_depthPercent = Param(nameof(DepthPercent), 25m)
			.SetDisplay("Fractal Channel Depth (%)", "Percentage width inside the fractal channel that must hold before trading.", "Filters")
			.SetNotNegative();

		_useHighLowChannel = Param(nameof(UseHighLowChannel), false)
			.SetDisplay("Use HL Channel", "Validate entries against a channel built from recent closes.", "Filters");

		_orientationPercent = Param(nameof(OrientationPercent), 20m)
			.SetDisplay("HL Orientation (%)", "Allowed penetration inside the HL channel before signals stay valid.", "Filters")
			.SetNotNegative();

		_allowFlatTrading = Param(nameof(AllowFlatTrading), true)
			.SetDisplay("Allow Flat Trading", "Permit trades when the close channel width is below the threshold.", "Filters");

		_flatThresholdPips = Param(nameof(FlatThresholdPips), 20m)
			.SetDisplay("Flat Threshold (pips)", "Minimum channel width required when flat trading is disabled.", "Filters")
			.SetNotNegative();

		_useWilliamsExit = Param(nameof(UseWilliamsExit), false)
			.SetDisplay("Use Williams Exit", "Close positions when Williams %R reaches extreme levels.", "Filters");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetDisplay("Williams %R Period", "Look-back period for the Williams %R exit filter.", "Filters")
			.SetGreaterThanZero();

		_williamsThreshold = Param(nameof(WilliamsThreshold), 30m)
			.SetDisplay("Williams Threshold", "Sensitivity of the Williams %R exit trigger (in percentage points).", "Filters")
			.SetNotNegative();

		_useCounterOrders = Param(nameof(UseCounterOrders), false)
			.SetDisplay("Use Counter Orders", "Stage opposite stop orders after entering a position.", "Orders");

		_singlePosition = Param(nameof(SinglePosition), false)
			.SetDisplay("Single Position Mode", "Block additional entries while already holding exposure in that direction.", "Orders");

		_signalAgeLimit = Param(nameof(SignalAgeLimit), 3)
			.SetDisplay("Signal Age (bars)", "Maximum number of new bars during which a fractal signal remains valid.", "Orders")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for fractal and channel calculations.", "Data");
	}

	/// <summary>
	/// Base order size sent to the market.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
	/// Offset applied when placing counter-trend stop orders.
	/// </summary>
	public decimal OffsetPips
	{
		get => _offsetPips.Value;
		set => _offsetPips.Value = value;
	}

	/// <summary>
	/// Number of candles used when building the HL channel.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Determines whether the fractal channel filter is active.
	/// </summary>
	public bool UseFractalChannel
	{
		get => _useFractalChannel.Value;
		set => _useFractalChannel.Value = value;
	}

	/// <summary>
	/// Percentage width for the inner fractal channel.
	/// </summary>
	public decimal DepthPercent
	{
		get => _depthPercent.Value;
		set => _depthPercent.Value = value;
	}

	/// <summary>
	/// Determines whether the HL channel filter is active.
	/// </summary>
	public bool UseHighLowChannel
	{
		get => _useHighLowChannel.Value;
		set => _useHighLowChannel.Value = value;
	}

	/// <summary>
	/// Allowed penetration percentage inside the HL channel.
	/// </summary>
	public decimal OrientationPercent
	{
		get => _orientationPercent.Value;
		set => _orientationPercent.Value = value;
	}

	/// <summary>
	/// Allow trades when the market is considered flat.
	/// </summary>
	public bool AllowFlatTrading
	{
		get => _allowFlatTrading.Value;
		set => _allowFlatTrading.Value = value;
	}

	/// <summary>
	/// Minimum HL channel width required when flat trading is disallowed.
	/// </summary>
	public decimal FlatThresholdPips
	{
		get => _flatThresholdPips.Value;
		set => _flatThresholdPips.Value = value;
	}

	/// <summary>
	/// Enables Williams %R based exits.
	/// </summary>
	public bool UseWilliamsExit
	{
		get => _useWilliamsExit.Value;
		set => _useWilliamsExit.Value = value;
	}

	/// <summary>
	/// Look-back period used by the Williams %R indicator.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Threshold that triggers a Williams %R exit.
	/// </summary>
	public decimal WilliamsThreshold
	{
		get => _williamsThreshold.Value;
		set => _williamsThreshold.Value = value;
	}

	/// <summary>
	/// Enable hedging stop orders after market entries.
	/// </summary>
	public bool UseCounterOrders
	{
		get => _useCounterOrders.Value;
		set => _useCounterOrders.Value = value;
	}

	/// <summary>
	/// Allow only one position per direction.
	/// </summary>
	public bool SinglePosition
	{
		get => _singlePosition.Value;
		set => _singlePosition.Value = value;
	}

	/// <summary>
	/// Maximum age of a fractal signal in bars.
	/// </summary>
	public int SignalAgeLimit
	{
		get => _signalAgeLimit.Value;
		set => _signalAgeLimit.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
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
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_channelHistory.Clear();
		_c1 = null;
		_c2 = null;
		_c3 = null;
		_c4 = null;
		_c5 = null;
		_lastUpperFractalPrice = null;
		_lastLowerFractalPrice = null;
		_lastUpperFractalCandle = null;
		_lastLowerFractalCandle = null;
		_upperFractalAge = int.MaxValue;
		_lowerFractalAge = int.MaxValue;
		_pendingLongSignal = false;
		_pendingShortSignal = false;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_pipSize = 0m;
		_lastWilliamsValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_williams = new WilliamsPercentRange
		{
			Length = WilliamsPeriod
		};

		_pipSize = CalculatePipSize();

		var stopLossDistance = ConvertPipsToPrice(StopLossPips);
		var takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);

		if (stopLossDistance > 0m || takeProfitDistance > 0m)
		{
			StartProtection(
				stopLoss: stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Price) : null,
				takeProfit: takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Price) : null);
		}

		Volume = OrderVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_williams, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Cache the latest Williams %R value whenever the indicator is fully formed.
		UpdateWilliamsValue(williamsValue);
		// Maintain the rolling queue that feeds the close-price channel filters.
		UpdateCandleHistory(candle);
		// Update fractal buffers with the newly finished candle.
		UpdateFractals(candle);

		var (channelHigh, channelLow, channelRange) = CalculateChannel();

		if (!AllowFlatTrading && channelRange > 0m)
		{
			var flatThreshold = ConvertPipsToPrice(FlatThresholdPips);
			if (flatThreshold > 0m && channelRange < flatThreshold)
			{
				CancelSignals();
				DropCounterOrders();
				return;
			}
		}

		var longAllowed = EvaluateLongFilters(candle, channelLow, channelHigh, channelRange);
		var shortAllowed = EvaluateShortFilters(candle, channelLow, channelHigh, channelRange);

		if (!UseCounterOrders)
			DropCounterOrders();

		ExecuteWilliamsExit();
		TryExecuteLong(longAllowed);
		TryExecuteShort(shortAllowed);
	}

	private void UpdateWilliamsValue(decimal williamsValue)
	{
		if (!_williams.IsFormed)
		{
			_lastWilliamsValue = null;
			return;
		}

		_lastWilliamsValue = williamsValue;
	}

	private void UpdateCandleHistory(ICandleMessage candle)
	{
		_channelHistory.Enqueue(candle);

		var limit = Math.Max(1, ChannelPeriod);
		// Ensure the queue only contains the configured number of candles.
		while (_channelHistory.Count > limit)
			_channelHistory.Dequeue();
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		if (_upperFractalAge < int.MaxValue)
			_upperFractalAge++;

		if (_lowerFractalAge < int.MaxValue)
			_lowerFractalAge++;

		// Shift the five-candle window forward.
		_c1 = _c2;
		_c2 = _c3;
		_c3 = _c4;
		_c4 = _c5;
		_c5 = candle;

		if (_c1 == null || _c2 == null || _c3 == null || _c4 == null || _c5 == null)
			return;

		var middleHigh = _c3.HighPrice;
		var middleLow = _c3.LowPrice;

		if (middleHigh > _c1.HighPrice && middleHigh > _c2.HighPrice && middleHigh > _c4.HighPrice && middleHigh > _c5.HighPrice)
		{
			_lastUpperFractalPrice = middleHigh;
			_lastUpperFractalCandle = _c3;
			_upperFractalAge = 0;
			_pendingShortSignal = true;
		}

		if (middleLow < _c1.LowPrice && middleLow < _c2.LowPrice && middleLow < _c4.LowPrice && middleLow < _c5.LowPrice)
		{
			_lastLowerFractalPrice = middleLow;
			_lastLowerFractalCandle = _c3;
			_lowerFractalAge = 0;
			_pendingLongSignal = true;
		}
	}

	private (decimal high, decimal low, decimal range) CalculateChannel()
	{
		var hasData = false;
		var maxClose = decimal.MinValue;
		var minClose = decimal.MaxValue;

		foreach (var item in _channelHistory)
		{
			hasData = true;
			var close = item.ClosePrice;

			if (close > maxClose)
				maxClose = close;

			if (close < minClose)
				minClose = close;
		}

		if (!hasData)
			return (0m, 0m, 0m);

		var range = maxClose - minClose;
		return (maxClose, minClose, range);
	}

	private bool EvaluateLongFilters(ICandleMessage candle, decimal channelLow, decimal channelHigh, decimal channelRange)
	{
		if (_lastLowerFractalPrice is not decimal lower)
			return false;

		if (SinglePosition && Position > 0m)
			return false;

		if (UseFractalChannel && _lastUpperFractalPrice is decimal upper)
		{
			var span = upper - lower;
			if (span <= 0m)
				return false;

			var margin = span * DepthPercent / 100m;
			var gate = lower + margin;
			if (candle.ClosePrice < gate)
				return false;
		}

		if (UseHighLowChannel)
		{
			if (channelRange <= 0m)
				return false;

			var gate = channelLow + channelRange * OrientationPercent / 100m;
			if (candle.ClosePrice < gate)
				return false;
		}

		return true;
	}

	private bool EvaluateShortFilters(ICandleMessage candle, decimal channelLow, decimal channelHigh, decimal channelRange)
	{
		if (_lastUpperFractalPrice is not decimal upper)
			return false;

		if (SinglePosition && Position < 0m)
			return false;

		if (UseFractalChannel && _lastLowerFractalPrice is decimal lower)
		{
			var span = upper - lower;
			if (span <= 0m)
				return false;

			var margin = span * DepthPercent / 100m;
			var gate = upper - margin;
			if (candle.ClosePrice > gate)
				return false;
		}

		if (UseHighLowChannel)
		{
			if (channelRange <= 0m)
				return false;

			var gate = channelHigh - channelRange * OrientationPercent / 100m;
			if (candle.ClosePrice > gate)
				return false;
		}

		return true;
	}

	private void TryExecuteLong(bool filtersPassed)
	{
		if (!_pendingLongSignal)
			return;

		if (_lowerFractalAge > SignalAgeLimit)
		{
			_pendingLongSignal = false;
			return;
		}

		if (!filtersPassed || _lastLowerFractalPrice is not decimal lower)
			return;

		var volume = NormalizeOrderVolume(OrderVolume);
		if (volume <= 0m)
			return;

		// Enter in the direction of the bullish fractal breakout.
		BuyMarket(volume);
		_pendingLongSignal = false;

		if (UseCounterOrders)
			PlaceSellStop(volume, lower);
	}

	private void TryExecuteShort(bool filtersPassed)
	{
		if (!_pendingShortSignal)
			return;

		if (_upperFractalAge > SignalAgeLimit)
		{
			_pendingShortSignal = false;
			return;
		}

		if (!filtersPassed || _lastUpperFractalPrice is not decimal upper)
			return;

		var volume = NormalizeOrderVolume(OrderVolume);
		if (volume <= 0m)
			return;

		// Enter in the direction of the bearish fractal breakout.
		SellMarket(volume);
		_pendingShortSignal = false;

		if (UseCounterOrders)
			PlaceBuyStop(volume, upper);
	}

	private void ExecuteWilliamsExit()
	{
		if (!UseWilliamsExit)
			return;

		if (_lastWilliamsValue is not decimal williams)
			return;

		var threshold = WilliamsThreshold;
		if (threshold <= 0m)
			return;

		// Exit positions when Williams %R reaches the configured extremes.
		if (Position > 0m && williams > -threshold)
		{
			ClosePosition();
			_pendingLongSignal = false;
		}
		else if (Position < 0m && williams < -100m + threshold)
		{
			ClosePosition();
			_pendingShortSignal = false;
		}
	}

	private void PlaceBuyStop(decimal volume, decimal reference)
	{
		var offset = ConvertPipsToPrice(OffsetPips);
		var price = NormalizePrice(reference + offset);

		CancelBuyStop();

		if (price <= 0m)
			return;

		_buyStopOrder = BuyStop(volume, price);
	}

	private void PlaceSellStop(decimal volume, decimal reference)
	{
		var offset = ConvertPipsToPrice(OffsetPips);
		var price = NormalizePrice(reference - offset);

		CancelSellStop();

		if (price <= 0m)
			return;

		_sellStopOrder = SellStop(volume, price);
	}

	private void CancelBuyStop()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);

		_buyStopOrder = null;
	}

	private void CancelSellStop()
	{
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);

		_sellStopOrder = null;
	}

	private void DropCounterOrders()
	{
		CancelBuyStop();
		CancelSellStop();
	}

	private void CancelSignals()
	{
		_pendingLongSignal = false;
		_pendingShortSignal = false;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_buyStopOrder != null && order == _buyStopOrder && order.State.IsFinal())
			_buyStopOrder = null;

		if (_sellStopOrder != null && order == _sellStopOrder && order.State.IsFinal())
			_sellStopOrder = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			DropCounterOrders();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		DropCounterOrders();
	}

	private decimal NormalizeOrderVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		// Align the requested volume with the instrument limits and volume step.
		var security = Security;
		var minVolume = security?.MinVolume ?? 0m;
		var maxVolume = security?.MaxVolume ?? 0m;
		var step = security?.VolumeStep ?? 0m;

		var normalized = volume;

		if (step > 0m)
		{
			var steps = Math.Round(volume / step, 0, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
				steps = 1m;

			normalized = steps * step;
		}

		if (normalized < minVolume && minVolume > 0m)
			normalized = minVolume;

		if (maxVolume > 0m && normalized > maxVolume)
			normalized = maxVolume;

		return normalized;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		// MetaTrader uses a 10x adjustment for symbols quoted with 3 or 5 decimals.
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

		// Convert pip distance to an absolute price offset using the cached pip size.
		var pip = _pipSize > 0m ? _pipSize : CalculatePipSize();
		return pip > 0m ? pip * pips : pips;
	}
}

