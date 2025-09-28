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
/// Protective manager converted from the MetaTrader "Auto_Tp" expert advisor.
/// Automatically applies take-profit, optional stop-loss, trailing stop, and equity protection to the current position.
/// </summary>
public class AutoTpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useEquityProtection;
	private readonly StrategyParam<decimal> _minEquityPercent;

	private Order _stopOrder;
	private Order _takeProfitOrder;
	private Sides? _currentSide;
	private decimal? _currentStopPrice;
	private decimal? _currentTakeProfitPrice;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal _pipSize;
	private decimal _pointSize;
	private bool _equityProtectionTriggered;

	/// <summary>
	/// Initializes a new instance of the <see cref="AutoTpStrategy"/> class.
	/// </summary>
	public AutoTpStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance of the take-profit order attached to the position.", "Risk");

		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Use Stop Loss", "Enable initial stop-loss placement.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance of the protective stop-loss.", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Trail the protective stop once the trade moves into profit.", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (pips)", "Distance between price and the trailing stop.", "Risk");

		_useEquityProtection = Param(nameof(UseEquityProtection), false)
			.SetDisplay("Use Equity Protection", "Close all trades when equity drops below the configured percentage.", "Risk");

		_minEquityPercent = Param(nameof(MinEquityPercent), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("Minimum Equity %", "Close trades if equity is below this percentage of balance.", "Risk");
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable placement of the protective stop-loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enable trailing-stop behaviour for the protective stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in MetaTrader pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable account level equity protection.
	/// </summary>
	public bool UseEquityProtection
	{
		get => _useEquityProtection.Value;
		set => _useEquityProtection.Value = value;
	}

	/// <summary>
	/// Minimum equity expressed as a percentage of balance.
	/// </summary>
	public decimal MinEquityPercent
	{
		get => _minEquityPercent.Value;
		set => _minEquityPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetProtectionOrders();
		_currentSide = null;
		_currentBid = null;
		_currentAsk = null;
		_currentStopPrice = null;
		_currentTakeProfitPrice = null;
		_pipSize = 0m;
		_pointSize = 0m;
		_equityProtectionTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculateAdjustedPoint();
		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
			_pointSize = 1m;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
		{
			ResetProtectionOrders();
			_currentSide = null;
			return;
		}

		var side = Position > 0m ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtectionOrders();
			_currentSide = side;
		}

		ApplyInitialProtection();

		if (side == Sides.Buy)
			ManageLongProtection();
		else
			ManageShortProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_currentBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_currentAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0m)
			ManageLongProtection();
		else if (Position < 0m)
			ManageShortProtection();

		CheckEquityProtection();
	}

	private void ApplyInitialProtection()
	{
		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (TakeProfitPips > 0m)
		{
			var takeProfitOffset = TakeProfitPips * _pipSize;
			var takeProfitPrice = _currentSide == Sides.Buy
				? entryPrice + takeProfitOffset
				: entryPrice - takeProfitOffset;

			if (takeProfitPrice > 0m)
				UpdateTakeProfitOrder(_currentSide == Sides.Buy, takeProfitPrice, volume);
		}
		else
		{
			ResetTakeProfitOrder();
		}

		if (!UseStopLoss || StopLossPips <= 0m)
		{
			ResetStopOrder();
			return;
		}

		var stopOffset = StopLossPips * _pipSize;
		var baseStop = _currentSide == Sides.Buy
			? entryPrice - stopOffset
			: entryPrice + stopOffset;

		if (_currentStopPrice is decimal existingStop)
		{
			baseStop = _currentSide == Sides.Buy
				? Math.Max(baseStop, existingStop)
				: Math.Min(baseStop, existingStop);
		}

		if (baseStop > 0m)
			UpdateStopOrder(_currentSide == Sides.Buy, baseStop, volume);
	}

	private void ManageLongProtection()
	{
		if (_currentSide != Sides.Buy)
			return;

		if (_currentBid is not decimal bid || bid <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopPrice = _currentStopPrice ?? 0m;

		if (UseStopLoss && StopLossPips > 0m)
		{
			var stopOffset = StopLossPips * _pipSize;
			var baseStop = entryPrice - stopOffset;
			if (baseStop > 0m)
				stopPrice = stopPrice > 0m ? Math.Max(stopPrice, baseStop) : baseStop;

			if (UseTrailingStop && TrailingStopPips > 0m)
			{
				var trailingDistance = TrailingStopPips * _pipSize;
				if (bid - entryPrice > trailingDistance)
				{
					var candidate = bid - trailingDistance;
					if (candidate > 0m)
						stopPrice = stopPrice > 0m ? Math.Max(stopPrice, candidate) : candidate;
				}
			}

			if (stopPrice > 0m)
			{
				if (stopPrice >= bid)
					stopPrice = bid - Math.Max(_pointSize, _pipSize / 10m);

				if (stopPrice > 0m)
					UpdateStopOrder(true, stopPrice, volume);
			}
		}
		else if (_stopOrder != null)
		{
			ResetStopOrder();
		}
	}

	private void ManageShortProtection()
	{
		if (_currentSide != Sides.Sell)
			return;

		if (_currentAsk is not decimal ask || ask <= 0m)
			return;

		if (PositionPrice is not decimal entryPrice || entryPrice <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		var stopPrice = _currentStopPrice ?? 0m;

		if (UseStopLoss && StopLossPips > 0m)
		{
			var stopOffset = StopLossPips * _pipSize;
			var baseStop = entryPrice + stopOffset;
			if (baseStop > 0m)
				stopPrice = stopPrice > 0m ? Math.Min(stopPrice, baseStop) : baseStop;

			if (UseTrailingStop && TrailingStopPips > 0m)
			{
				var trailingDistance = TrailingStopPips * _pipSize;
				if (entryPrice - ask > trailingDistance)
				{
					var candidate = ask + trailingDistance;
					if (candidate > 0m)
						stopPrice = stopPrice > 0m ? Math.Min(stopPrice, candidate) : candidate;
				}
			}

			if (stopPrice > 0m)
			{
				if (stopPrice <= ask)
					stopPrice = ask + Math.Max(_pointSize, _pipSize / 10m);

				UpdateStopOrder(false, stopPrice, volume);
			}
		}
		else if (_stopOrder != null)
		{
			ResetStopOrder();
		}
	}

	private void CheckEquityProtection()
	{
		if (!UseEquityProtection || Portfolio == null)
			return;

		var balance = Portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
			balance = Portfolio.CurrentValue ?? 0m;

		if (balance <= 0m)
			return;

		var equity = Portfolio.CurrentValue ?? 0m;
		var threshold = balance * MinEquityPercent / 100m;

		if (threshold <= 0m)
			return;

		if (equity <= threshold)
		{
			if (_equityProtectionTriggered)
				return;

			_equityProtectionTriggered = true;
			LogInfo($"Equity protection triggered. Equity={equity:0.##}, Balance={balance:0.##}.");
			CloseAll("Equity protection");
		}
		else
		{
			_equityProtectionTriggered = false;
		}
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		if (stopPrice <= 0m || volume <= 0m)
			return;

		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
		{
			if (_stopOrder.Price == stopPrice && _stopOrder.Volume == volume)
				return;

			CancelOrder(_stopOrder);
		}

		_stopOrder = isLong
			? SellStop(price: stopPrice, volume: volume)
			: BuyStop(price: stopPrice, volume: volume);

		_currentStopPrice = stopPrice;
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takeProfitPrice, decimal volume)
	{
		if (takeProfitPrice <= 0m || volume <= 0m)
			return;

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
		{
			if (_takeProfitOrder.Price == takeProfitPrice && _takeProfitOrder.Volume == volume)
				return;

			CancelOrder(_takeProfitOrder);
		}

		_takeProfitOrder = isLong
			? SellLimit(price: takeProfitPrice, volume: volume)
			: BuyLimit(price: takeProfitPrice, volume: volume);

		_currentTakeProfitPrice = takeProfitPrice;
	}

	private void ResetProtectionOrders()
	{
		ResetStopOrder();
		ResetTakeProfitOrder();
	}

	private void ResetStopOrder()
	{
		if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
			CancelOrder(_stopOrder);

		_stopOrder = null;
		_currentStopPrice = null;
	}

	private void ResetTakeProfitOrder()
	{
		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_takeProfitOrder = null;
		_currentTakeProfitPrice = null;
	}

	private decimal CalculateAdjustedPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = CountDecimals(step);
		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}

