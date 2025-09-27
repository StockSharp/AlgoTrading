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
/// Port of the "_HPCS_IntFourth_MT4_EA_V01_WE" MetaTrader expert advisor.
/// Immediately opens a long position, applies protective orders and exits after a fixed holding time.
/// </summary>
public class HpcsInter4Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _extraStopPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _closeDelaySeconds;

	private decimal _pipSize;
	private DateTimeOffset? _entryTime;
	private bool _orderSubmitted;
	private bool _exitRequested;

	/// <summary>
	/// Fixed trade volume used for the market entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Extra stop-loss buffer in MetaTrader pips that replicates the post-entry modification.
	/// </summary>
	public int ExtraStopPips
	{
		get => _extraStopPips.Value;
		set => _extraStopPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Number of seconds the position must remain open before it is forcefully closed.
	/// </summary>
	public int CloseDelaySeconds
	{
		get => _closeDelaySeconds.Value;
		set => _closeDelaySeconds.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter4Strategy"/> class.
	/// </summary>
	public HpcsInter4Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Fixed volume used for the initial market order.", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 10)
			.SetDisplay("Stop Loss (pips)", "Base stop-loss distance expressed in MetaTrader pips.", "Risk")
			.SetNotNegative();

		_extraStopPips = Param(nameof(ExtraStopPips), 10)
			.SetDisplay("Extra Stop Buffer", "Additional MetaTrader pips subtracted from the stop after entry.", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in MetaTrader pips.", "Risk")
			.SetNotNegative();

		_closeDelaySeconds = Param(nameof(CloseDelaySeconds), 30)
			.SetDisplay("Close Delay (seconds)", "Holding time before the position is closed by the timer.", "Execution")
			.SetNotNegative();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_entryTime = null;
		_orderSubmitted = false;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSize();

		var takeProfitDistance = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;
		var stopLossTotalPips = StopLossPips + ExtraStopPips;
		var stopLossDistance = stopLossTotalPips > 0 ? stopLossTotalPips * _pipSize : 0m;

		Unit takeProfitUnit = takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : null;
		Unit stopLossUnit = stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : null;

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		if (Volume <= 0m)
		{
			Volume = OrderVolume;
		}

		SubmitEntry();

		if (CloseDelaySeconds > 0)
		{
			Timer.Start(TimeSpan.FromSeconds(1), OnTimer);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade == null)
			return;

		// Capture the entry time after the first filled buy trade.
		if (!_entryTime.HasValue && trade.Order.Direction == Sides.Buy && Position > 0m)
		{
			_entryTime = trade.Trade.ServerTime;
			_exitRequested = false;
		}

		// Reset state after the position is completely closed.
		if (Position <= 0m)
		{
			_entryTime = null;
			_exitRequested = false;
		}
	}

	private void SubmitEntry()
	{
		if (_orderSubmitted)
			return;

		var volume = Volume > 0m ? Volume : OrderVolume;
		if (volume <= 0m)
			return;

		// Issue the initial market buy order exactly once.
		BuyMarket(volume);
		_orderSubmitted = true;
	}

	private void OnTimer()
	{
		if (!_entryTime.HasValue || _exitRequested || CloseDelaySeconds <= 0)
			return;

		var now = CurrentTime;
		var elapsed = now - _entryTime.Value;
		if (elapsed < TimeSpan.FromSeconds(CloseDelaySeconds))
			return;

		var position = Position;
		if (position <= 0m)
		{
			_entryTime = null;
			return;
		}

		var exitVolume = Math.Abs(position);
		if (exitVolume <= 0m)
			return;

		// Close the long position using a market sell order when the holding period expires.
		SellMarket(exitVolume);
		_exitRequested = true;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			step = 1m;
		}

		var decimals = Security?.Decimals ?? 0;

		_pipSize = step;
		if (decimals == 3 || decimals == 5)
		{
			_pipSize = step * 10m;
		}
	}
}

