namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Random direction strategy with pip-based stop loss and take profit that doubles the volume after losses.
/// </summary>
public class StopLossTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _pipSizeOverride;

	private readonly Random _random = new(Environment.TickCount);

	private decimal _currentVolume;
	private decimal _pipValue;
	private Order _entryOrder;
	private Order _stopOrder;
	private Order _takeProfitOrder;
	private decimal _entryVolumeFilled;
	private decimal _pendingEntryVolume;
	private decimal _entryPrice;
	private bool _entryIsLong;

	/// <summary>
	/// Candle type used for decision making.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Starting order volume that is reset after profitable trades.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Custom pip size override (0 means auto-detect from security settings).
	/// </summary>
	public decimal PipSize
	{
		get => _pipSizeOverride.Value;
		set => _pipSizeOverride.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public StopLossTakeProfitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for evaluating entries", "General");

		_stopLossPips = Param(nameof(StopLossPips), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting order volume", "Risk");

		_pipSizeOverride = Param(nameof(PipSize), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Custom Pip Size", "Override pip size (0 = auto)", "Risk");
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

		_currentVolume = 0m;
		_pipValue = 0m;
		_entryOrder = null;
		_stopOrder = null;
		_takeProfitOrder = null;
		_entryVolumeFilled = 0m;
		_pendingEntryVolume = 0m;
		_entryPrice = 0m;
		_entryIsLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipValue = ResolvePipValue();
		_currentVolume = NormalizeVolume(InitialVolume);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (HasActiveEntryOrder())
			return;

		var volume = NormalizeVolume(_currentVolume);

		// Ensure the volume never drops to zero due to rounding.
		if (volume <= 0m)
		{
			volume = NormalizeVolume(InitialVolume);
			_currentVolume = volume;
		}

		// Randomly choose trade direction when flat.
		var goShort = _random.Next(0, 2) == 0;

		_entryOrder = goShort
			? SellMarket(volume)
			: BuyMarket(volume);

		if (_entryOrder == null)
			return;

		_entryIsLong = !goShort;
		_entryVolumeFilled = 0m;
		_pendingEntryVolume = volume;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
			return;

		// Track fills for the current market entry order.
		if (_entryOrder != null && trade.Order == _entryOrder)
		{
			var tradeVolume = trade.Trade.Volume;
			var totalVolume = _entryVolumeFilled + tradeVolume;

			if (totalVolume > 0m)
			{
				_entryPrice = (_entryPrice * _entryVolumeFilled + trade.Trade.Price * tradeVolume) / totalVolume;
				_entryVolumeFilled = totalVolume;
			}

			if (_pendingEntryVolume > 0m && _entryVolumeFilled >= _pendingEntryVolume)
			{
				PlaceProtectionOrders(_entryIsLong, _entryPrice);
				_entryOrder = null;
				_entryVolumeFilled = 0m;
				_pendingEntryVolume = 0m;
			}

			return;
		}

		// Adjust money management rules when protection orders close the position.
		if (_stopOrder != null && trade.Order == _stopOrder)
		{
			if (Position == 0)
				HandleStopExecuted();

			return;
		}

		if (_takeProfitOrder != null && trade.Order == _takeProfitOrder)
		{
			if (Position == 0)
				HandleTakeProfitExecuted();

			return;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		// Remove any resting protection orders once the position is flat.
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);
	}

	private void PlaceProtectionOrders(bool isLong, decimal entryPrice)
	{
		CancelProtectionOrders();

		var stopOffset = StopLossPips <= 0 ? 0m : StopLossPips * _pipValue;
		var takeOffset = TakeProfitPips <= 0 ? 0m : TakeProfitPips * _pipValue;
		var volume = Math.Abs(Position);

		// Register stop loss based on trade direction.
		if (stopOffset > 0m)
		{
			var stopPrice = isLong ? entryPrice - stopOffset : entryPrice + stopOffset;

			if (stopPrice > 0m)
			{
				_stopOrder = isLong
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);
			}
		}

		// Register take profit order only when requested.
		if (takeOffset > 0m)
		{
			var takePrice = isLong ? entryPrice + takeOffset : entryPrice - takeOffset;

			if (takePrice > 0m)
			{
				_takeProfitOrder = isLong
					? SellLimit(volume, takePrice)
					: BuyLimit(volume, takePrice);
			}
		}
	}

	private void HandleStopExecuted()
	{
		// Cancel the opposite protection order to avoid unintended re-entries.
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_stopOrder = null;
		_takeProfitOrder = null;

		var doubled = _currentVolume * 2m;
		var maxVolume = Security?.MaxVolume;

		if (maxVolume != null && maxVolume > 0m && doubled > maxVolume.Value)
			doubled = maxVolume.Value;

		_currentVolume = NormalizeVolume(doubled);
	}

	private void HandleTakeProfitExecuted()
	{
		// Remove the unused stop order after a profitable exit.
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_takeProfitOrder = null;

		_currentVolume = NormalizeVolume(InitialVolume);
	}

	private void CancelProtectionOrders()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_stopOrder = null;
		_takeProfitOrder = null;
	}

	private bool HasActiveEntryOrder()
	{
		return _entryOrder != null && _entryOrder.State == OrderStates.Active;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;

		if (security != null)
		{
			var step = security.VolumeStep;

			if (step != null && step > 0m)
			{
				var steps = Math.Floor(volume / step.Value);
				volume = steps * step.Value;
			}

			var minVolume = security.MinVolume;

			if (minVolume != null && minVolume > 0m && volume < minVolume.Value)
				volume = minVolume.Value;

			var maxVolume = security.MaxVolume;

			if (maxVolume != null && maxVolume > 0m && volume > maxVolume.Value)
				volume = maxVolume.Value;
		}

		// Fallback to the initial volume if rounding removed the size completely.
		if (volume <= 0m)
			volume = InitialVolume;

		return volume;
	}

	private decimal ResolvePipValue()
	{
		if (PipSize > 0m)
			return PipSize;

		var security = Security;
		var priceStep = security?.PriceStep ?? 0.0001m;
		var decimals = security?.Decimals;

		var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;
		var pip = priceStep * adjust;

		if (pip <= 0m)
			pip = priceStep > 0m ? priceStep : 0.0001m;

		return pip;
	}
}
