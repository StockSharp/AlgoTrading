namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Hedging strategy that simultaneously opens buy and sell positions and manages them with break-even and trailing rules.
/// </summary>
public class RandomHedgStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hedgeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<bool> _enableExitStrategy;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingDistance;
	private decimal _breakEvenTriggerDistance;
	private decimal _breakEvenOffsetDistance;

	private bool _longActive;
	private decimal _longVolume;
	private decimal _longEntryPrice;
	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal _longHighWatermark;

	private bool _shortActive;
	private decimal _shortVolume;
	private decimal _shortEntryPrice;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;
	private decimal _shortLowWatermark;

	private decimal _pendingLongEntry;
	private decimal _pendingShortEntry;
	private decimal _pendingLongExit;
	private decimal _pendingShortExit;

	/// <summary>
	/// Initializes configurable parameters.
	/// </summary>
	public RandomHedgStrategy()
	{
		_hedgeVolume = Param(nameof(HedgeVolume), 0.1m)
		.SetDisplay("Hedge Volume", "Contract volume used for the buy and sell hedge", "Risk")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 200m)
		.SetDisplay("Stop Loss (pips)", "Distance for the protective stop", "Risk")
		.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 200m)
		.SetDisplay("Take Profit (pips)", "Distance for the protective take profit", "Risk")
		.SetGreaterThanZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetDisplay("Trailing Stop (pips)", "Trailing step applied once price moves in favor", "Risk")
		.SetGreaterThanZero();

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
		.SetDisplay("Break-Even Trigger (pips)", "Profit distance that activates the break-even move", "Risk")
		.SetGreaterThanZero();

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetDisplay("Break-Even Offset (pips)", "Extra distance added when moving the stop to break even", "Risk")
		.SetGreaterThanZero();

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Enable trailing stop management", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break Even", "Enable break-even stop adjustment", "Risk");

		_enableExitStrategy = Param(nameof(EnableExitStrategy), false)
		.SetDisplay("Exit on Bollinger", "Close both legs when price touches the Bollinger lower band", "Exit");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("Bollinger Period", "Period for the optional Bollinger-based exit", "Exit")
		.SetGreaterThanZero();

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
		.SetDisplay("Bollinger Width", "Width multiplier for the Bollinger Bands", "Exit")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Candle series used for signal processing", "Data");
	}

	/// <summary>
	/// Contract volume used when opening both hedge legs.
	/// </summary>
	public decimal HedgeVolume
	{
		get => _hedgeVolume.Value;
		set => _hedgeVolume.Value = value;
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
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop step expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Break-even activation distance expressed in pips.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional distance applied when moving the stop to break even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop behaviour.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enables the break-even logic.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Enables the Bollinger-based exit routine.
	/// </summary>
	public bool EnableExitStrategy
	{
		get => _enableExitStrategy.Value;
		set => _enableExitStrategy.Value = value;
	}

	/// <summary>
	/// Period used by the Bollinger Bands indicator.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Width multiplier used by the Bollinger Bands indicator.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Candle type used to drive signal processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		StartProtection();

		_pipSize = CalculatePipSize();
		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingDistance = TrailingStopPips * _pipSize;
		_breakEvenTriggerDistance = BreakEvenTriggerPips * _pipSize;
		_breakEvenOffsetDistance = BreakEvenOffsetPips * _pipSize;

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(bollinger, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var bollinger = (BollingerBandsValue)bollingerValue;
		if (bollinger.LowBand is not decimal lower)
			return;

		if (bollinger.UpBand is not decimal upper)
			return;

		var bandWidth = upper - lower;
		if (bandWidth <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (EnableExitStrategy && lower > 0m && close <= lower)
		{
			CloseAllPositions();
			return;
		}

		if (!_longActive && !_shortActive)
		{
			OpenHedgePositions(close);
			return;
		}

		if (_longActive)
		ManageLongLeg(high, low);

		if (_shortActive)
		ManageShortLeg(high, low);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
		return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (volume <= 0m)
		return;

		if (trade.Order.Side == Sides.Buy)
		{
			ProcessBuyTrade(volume, price);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			ProcessSellTrade(volume, price);
		}
	}

	private void ProcessBuyTrade(decimal volume, decimal price)
	{
		if (_pendingLongEntry > 0m)
		{
			var handled = Math.Min(volume, _pendingLongEntry);
			_pendingLongEntry -= handled;

			_longActive = true;
			_longVolume += handled;
			_longEntryPrice = price;
			_longHighWatermark = price;
			_longStopLoss = _stopLossDistance > 0m ? price - _stopLossDistance : null;
			_longTakeProfit = _takeProfitDistance > 0m ? price + _takeProfitDistance : null;
			return;
		}

		if (_pendingShortExit > 0m)
		{
			var handled = Math.Min(volume, _pendingShortExit);
			_pendingShortExit -= handled;

			_shortVolume -= handled;

			if (_shortVolume <= 0m)
			{
				ResetShortLeg();
			}
		}
	}

	private void ProcessSellTrade(decimal volume, decimal price)
	{
		if (_pendingShortEntry > 0m)
		{
			var handled = Math.Min(volume, _pendingShortEntry);
			_pendingShortEntry -= handled;

			_shortActive = true;
			_shortVolume += handled;
			_shortEntryPrice = price;
			_shortLowWatermark = price;
			_shortStopLoss = _stopLossDistance > 0m ? price + _stopLossDistance : null;
			_shortTakeProfit = _takeProfitDistance > 0m ? price - _takeProfitDistance : null;
			return;
		}

		if (_pendingLongExit > 0m)
		{
			var handled = Math.Min(volume, _pendingLongExit);
			_pendingLongExit -= handled;

			_longVolume -= handled;

			if (_longVolume <= 0m)
			{
				ResetLongLeg();
			}
		}
	}

	private void ManageLongLeg(decimal high, decimal low)
	{
		if (_longVolume <= 0m)
		return;

		if (high > _longHighWatermark)
		_longHighWatermark = high;

		if (EnableBreakEven && _breakEvenTriggerDistance > 0m)
		{
			var profitDistance = _longHighWatermark - _longEntryPrice;
			if (profitDistance >= _breakEvenTriggerDistance)
			{
				var candidate = _longEntryPrice + _breakEvenOffsetDistance;
				if (_longStopLoss is null || candidate > _longStopLoss.Value)
				_longStopLoss = candidate;
			}
		}

		if (EnableTrailing && _trailingDistance > 0m)
		{
			var profitDistance = _longHighWatermark - _longEntryPrice;
			if (profitDistance >= _trailingDistance)
			{
				var candidate = _longHighWatermark - _trailingDistance;
				if (_longStopLoss is null || candidate > _longStopLoss.Value)
				_longStopLoss = candidate;
			}
		}

		var shouldClose = false;

		if (_longTakeProfit is decimal longTp && high >= longTp)
		shouldClose = true;

		if (!shouldClose && _longStopLoss is decimal longStop && low <= longStop)
		shouldClose = true;

		if (shouldClose)
		CloseLongLeg();
	}

	private void ManageShortLeg(decimal high, decimal low)
	{
		if (_shortVolume <= 0m)
		return;

		if (_shortLowWatermark == 0m || low < _shortLowWatermark)
		_shortLowWatermark = low;

		if (EnableBreakEven && _breakEvenTriggerDistance > 0m)
		{
			var profitDistance = _shortEntryPrice - _shortLowWatermark;
			if (profitDistance >= _breakEvenTriggerDistance)
			{
				var candidate = _shortEntryPrice - _breakEvenOffsetDistance;
				if (_shortStopLoss is null || candidate < _shortStopLoss.Value)
				_shortStopLoss = candidate;
			}
		}

		if (EnableTrailing && _trailingDistance > 0m)
		{
			var profitDistance = _shortEntryPrice - _shortLowWatermark;
			if (profitDistance >= _trailingDistance)
			{
				var candidate = _shortLowWatermark + _trailingDistance;
				if (_shortStopLoss is null || candidate < _shortStopLoss.Value)
				_shortStopLoss = candidate;
			}
		}

		var shouldClose = false;

		if (_shortTakeProfit is decimal shortTp && low <= shortTp)
		shouldClose = true;

		if (!shouldClose && _shortStopLoss is decimal shortStop && high >= shortStop)
		shouldClose = true;

		if (shouldClose)
		CloseShortLeg();
	}

	private void OpenHedgePositions(decimal referencePrice)
	{
		var volume = AdjustVolume(HedgeVolume);
		if (volume <= 0m)
		return;

		_pendingLongEntry += volume;
		_pendingShortEntry += volume;

		BuyMarket(volume);
		SellMarket(volume);

		_longEntryPrice = referencePrice;
		_longHighWatermark = referencePrice;
		_shortEntryPrice = referencePrice;
		_shortLowWatermark = referencePrice;
	}

	private void CloseLongLeg()
	{
		if (_longVolume <= 0m)
		return;

		_pendingLongExit += _longVolume;
		SellMarket(_longVolume);
	}

	private void CloseShortLeg()
	{
		if (_shortVolume <= 0m)
		return;

		_pendingShortExit += _shortVolume;
		BuyMarket(_shortVolume);
	}

	private void CloseAllPositions()
	{
		CloseLongLeg();
		CloseShortLeg();
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security?.PriceStep != null && security.PriceStep > 0m)
		{
			var step = security.PriceStep.Value;
			if (step == 0.00001m || step == 0.001m)
			return step * 10m;

			return step;
		}

		return 0.0001m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security?.VolumeStep != null && security.VolumeStep > 0m)
		{
			var step = security.VolumeStep.Value;
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume != null && security.MinVolume > 0m && volume < security.MinVolume.Value)
		volume = security.MinVolume.Value;

		if (security?.MaxVolume != null && security.MaxVolume > 0m && volume > security.MaxVolume.Value)
		volume = security.MaxVolume.Value;

		return volume;
	}

	private void ResetState()
	{
		ResetLongLeg();
		ResetShortLeg();

		_pendingLongEntry = 0m;
		_pendingShortEntry = 0m;
		_pendingLongExit = 0m;
		_pendingShortExit = 0m;

		_pipSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_trailingDistance = 0m;
		_breakEvenTriggerDistance = 0m;
		_breakEvenOffsetDistance = 0m;
	}

	private void ResetLongLeg()
	{
		_longActive = false;
		_longVolume = 0m;
		_longEntryPrice = 0m;
		_longStopLoss = null;
		_longTakeProfit = null;
		_longHighWatermark = 0m;
	}

	private void ResetShortLeg()
	{
		_shortActive = false;
		_shortVolume = 0m;
		_shortEntryPrice = 0m;
		_shortStopLoss = null;
		_shortTakeProfit = null;
		_shortLowWatermark = 0m;
	}
}
