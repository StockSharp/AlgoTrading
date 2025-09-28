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
/// Port of the MetaTrader 4 expert advisor "Training2".
/// Implements manual long/short toggles, configurable protective distances, and breakpoint alerts.
/// </summary>
public class TrainingSimulatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _upperStopPrice;
	private readonly StrategyParam<decimal> _lowerStopPrice;
	private readonly StrategyParam<bool> _pauseOnBreakpoint;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<bool> _modifyLongTargets;
	private readonly StrategyParam<bool> _modifyShortTargets;

	private bool _previousBuyEnabled;
	private bool _previousSellEnabled;
	private decimal _pointSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal? _entryPrice;
	private decimal? _lastTradePrice;
	private bool _closeInProgress;
	private bool _breakpointTriggered;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrainingSimulatorStrategy"/> class.
	/// </summary>
	public TrainingSimulatorStrategy()
	{

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance in MetaTrader points applied above the entry price.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Distance in MetaTrader points applied below the entry price.", "Risk")
			.SetCanOptimize(true);

		_upperStopPrice = Param(nameof(UpperStopPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Upper Breakpoint", "Price level that pauses the strategy when crossed upward.", "Controls");

		_lowerStopPrice = Param(nameof(LowerStopPrice), 0m)
			.SetNotNegative()
			.SetDisplay("Lower Breakpoint", "Price level that pauses the strategy when crossed downward.", "Controls");

		_pauseOnBreakpoint = Param(nameof(PauseOnBreakpoint), true)
			.SetDisplay("Pause on breakpoint", "Stop the strategy when an upper or lower breakpoint is hit.", "Controls");

		_enableBuy = Param(nameof(EnableBuy), false)
			.SetDisplay("Enable long", "Set to true to open a buy position; set to false to flatten.", "Controls");

		_enableSell = Param(nameof(EnableSell), false)
			.SetDisplay("Enable short", "Set to true to open a sell position; set to false to flatten.", "Controls");

		_modifyLongTargets = Param(nameof(ModifyLongTargets), false)
			.SetDisplay("Modify long targets", "Reapply stop-loss and take-profit distances to an open long position.", "Controls");

		_modifyShortTargets = Param(nameof(ModifyShortTargets), false)
			.SetDisplay("Modify short targets", "Reapply stop-loss and take-profit distances to an open short position.", "Controls");

		_pointSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_entryPrice = null;
		_lastTradePrice = null;
		_closeInProgress = false;
		_breakpointTriggered = false;
		_previousBuyEnabled = false;
		_previousSellEnabled = false;
	}


	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Upper breakpoint price that triggers a pause.
	/// </summary>
	public decimal UpperStopPrice
	{
		get => _upperStopPrice.Value;
		set => _upperStopPrice.Value = value;
	}

	/// <summary>
	/// Lower breakpoint price that triggers a pause.
	/// </summary>
	public decimal LowerStopPrice
	{
		get => _lowerStopPrice.Value;
		set => _lowerStopPrice.Value = value;
	}

	/// <summary>
	/// Whether the strategy should stop automatically when a breakpoint is reached.
	/// </summary>
	public bool PauseOnBreakpoint
	{
		get => _pauseOnBreakpoint.Value;
		set => _pauseOnBreakpoint.Value = value;
	}

	/// <summary>
	/// Toggle that opens or closes a long position.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Toggle that opens or closes a short position.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Reapply protective distances to an existing long position.
	/// </summary>
	public bool ModifyLongTargets
	{
		get => _modifyLongTargets.Value;
		set => _modifyLongTargets.Value = value;
	}

	/// <summary>
	/// Reapply protective distances to an existing short position.
	/// </summary>
	public bool ModifyShortTargets
	{
		get => _modifyShortTargets.Value;
		set => _modifyShortTargets.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointSize = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_entryPrice = null;
		_lastTradePrice = null;
		_closeInProgress = false;
		_breakpointTriggered = false;
		_previousBuyEnabled = EnableBuy;
		_previousSellEnabled = EnableSell;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();

		Timer.Start(TimeSpan.FromMilliseconds(250), ProcessManualControls);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			_entryPrice = null;
			_stopLossDistance = 0m;
			_takeProfitDistance = 0m;
			_closeInProgress = false;
			_breakpointTriggered = false;
			EnableBuy = false;
			EnableSell = false;
			_previousBuyEnabled = false;
			_previousSellEnabled = false;
			return;
		}

		if (Position > 0m && delta > 0m)
		{
			InitializePositionState();
		}
		else if (Position < 0m && delta < 0m)
		{
			InitializePositionState();
		}
	}

	private void ProcessManualControls()
	{
		var buyEnabled = EnableBuy;
		var sellEnabled = EnableSell;

		if (buyEnabled != _previousBuyEnabled)
		{
			_previousBuyEnabled = buyEnabled;

			if (buyEnabled)
			{
				EnableSell = false;
				_previousSellEnabled = false;
				TryOpenPosition(Sides.Buy);
			}
			else if (Position > 0m)
			{
				RequestClosePosition();
			}
		}

		if (sellEnabled != _previousSellEnabled)
		{
			_previousSellEnabled = sellEnabled;

			if (sellEnabled)
			{
				EnableBuy = false;
				_previousBuyEnabled = false;
				TryOpenPosition(Sides.Sell);
			}
			else if (Position < 0m)
			{
				RequestClosePosition();
			}
		}

		if (Position > 0m && ModifyLongTargets)
		{
			ApplyProtectiveDistances();
			ModifyLongTargets = false;
		}
		else if (Position < 0m && ModifyShortTargets)
		{
			ApplyProtectiveDistances();
			ModifyShortTargets = false;
		}
	}

	private void TryOpenPosition(Sides direction)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
		{
			if ((direction == Sides.Buy && Position < 0m) ||
				(direction == Sides.Sell && Position > 0m))
			{
				RequestClosePosition();
			}

			return;
		}

		var volume = AdjustVolume(Volume);

		if (volume <= 0m)
			return;

		_closeInProgress = false;

		var order = direction == Sides.Buy
			? BuyMarket(volume)
			: SellMarket(volume);

		if (order == null)
			return;

		ApplyProtectiveDistances();
	}

	private void RequestClosePosition()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0m || _closeInProgress)
			return;

		_closeInProgress = true;
		ClosePosition();
	}

	private void InitializePositionState()
	{
		_entryPrice = PositionPrice != 0m ? PositionPrice : _lastTradePrice;
		ApplyProtectiveDistances();
		_closeInProgress = false;
	}

	private void ApplyProtectiveDistances()
	{
		if (_pointSize <= 0m)
			_pointSize = CalculatePointSize();

		_stopLossDistance = StopLossPoints > 0m ? StopLossPoints * _pointSize : 0m;
		_takeProfitDistance = TakeProfitPoints > 0m ? TakeProfitPoints * _pointSize : 0m;
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		var price = trade.Price;

		_lastTradePrice = price;

		CheckBreakpoints(price);

		if (_entryPrice is not decimal entry)
			return;

		if (Position > 0m)
		{
			if (_stopLossDistance > 0m && price <= entry - _stopLossDistance)
			{
				HandleExit("Long stop-loss hit.");
			}
			else if (_takeProfitDistance > 0m && price >= entry + _takeProfitDistance)
			{
				HandleExit("Long take-profit hit.");
			}
		}
		else if (Position < 0m)
		{
			if (_stopLossDistance > 0m && price >= entry + _stopLossDistance)
			{
				HandleExit("Short stop-loss hit.");
			}
			else if (_takeProfitDistance > 0m && price <= entry - _takeProfitDistance)
			{
				HandleExit("Short take-profit hit.");
			}
		}
	}

	private void HandleExit(string reason)
	{
		if (_closeInProgress)
			return;

		_closeInProgress = true;
		LogInfo(reason);
		ClosePosition();
	}

	private void CheckBreakpoints(decimal price)
	{
		var upper = UpperStopPrice > 0m ? UpperStopPrice : (decimal?)null;
		var lower = LowerStopPrice > 0m ? LowerStopPrice : (decimal?)null;

		if (upper is decimal upperPrice && price >= upperPrice)
		{
			HandleBreakpoint($"Upper breakpoint reached at {upperPrice:0.#####}.");
		}
		else if (lower is decimal lowerPrice && price <= lowerPrice)
		{
			HandleBreakpoint($"Lower breakpoint reached at {lowerPrice:0.#####}.");
		}
	}

	private void HandleBreakpoint(string message)
	{
		if (_breakpointTriggered)
			return;

		_breakpointTriggered = true;
		LogInfo(message);

		if (PauseOnBreakpoint)
			Stop();
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;

		if (security == null)
			return volume;

		var step = security.VolumeStep;

		if (step > 0m)
		{
			volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;
		}

		var minVolume = security.MinVolume;
		var maxVolume = security.MaxVolume;

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private decimal CalculatePointSize()
	{
		var security = Security;

		if (security?.PriceStep is decimal step && step > 0m)
			return step;

		var decimals = security?.Decimals ?? 0;

		if (decimals > 0)
		{
			var value = Math.Pow(10d, -decimals);
			return (decimal)value;
		}

		return 0.0001m;
	}
}
