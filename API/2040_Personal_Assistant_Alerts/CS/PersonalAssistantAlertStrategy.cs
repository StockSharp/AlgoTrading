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
/// Manual helper strategy that monitors account state and allows manual order actions.
/// It does not implement automatic signals but exposes methods to open and close positions.
/// </summary>
public class PersonalAssistantAlertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _allowPending;
	private readonly StrategyParam<bool> _requireStopLoss;
	private readonly StrategyParam<bool> _requireTakeProfit;
	private readonly StrategyParam<bool> _displayLegend;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>Volume for manual orders.</summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>Allow placing pending orders.</summary>
	public bool AllowPending
	{
		get => _allowPending.Value;
		set => _allowPending.Value = value;
	}

	/// <summary>Require stop loss for manual orders.</summary>
	public bool RequireStopLoss
	{
		get => _requireStopLoss.Value;
		set => _requireStopLoss.Value = value;
	}

	/// <summary>Require take profit for manual orders.</summary>
	public bool RequireTakeProfit
	{
		get => _requireTakeProfit.Value;
		set => _requireTakeProfit.Value = value;
	}

	/// <summary>Display legend with available actions.</summary>
	public bool DisplayLegend
	{
		get => _displayLegend.Value;
		set => _displayLegend.Value = value;
	}

	/// <summary>The type of candles used for periodic updates.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Constructor.</summary>
        public PersonalAssistantAlertStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_allowPending = Param(nameof(AllowPending), true)
			.SetDisplay("Allow Pending", "Allow placing pending orders", "General");

		_requireStopLoss = Param(nameof(RequireStopLoss), false)
			.SetDisplay("Require Stop Loss", "Stop loss must be specified", "General");

		_requireTakeProfit = Param(nameof(RequireTakeProfit), false)
			.SetDisplay("Require Take Profit", "Take profit must be specified", "General");

		_displayLegend = Param(nameof(DisplayLegend), false)
			.SetDisplay("Display Legend", "Show helper messages", "Display");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for updates", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		if (DisplayLegend)
		{
			LogInfo("Action legend:");
			LogInfo("* Buy() opens a long position.");
			LogInfo("* Sell() opens a short position.");
			LogInfo("* CloseAll() closes open positions.");
			LogInfo("* IncreaseVolume() and DecreaseVolume() change order volume.");
			LogInfo("* Pending orders require calling BuyStop/SellStop/BuyLimit/SellLimit methods.");
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openPositions = Position != 0 ? 1 : 0;

		LogInfo($"PnL={PnL}, Position={Position}, OpenPositions={openPositions}");
	}

	/// <summary>Open a long position using market order.</summary>
	public void Buy()
	{
		BuyMarket(OrderVolume + Math.Abs(Position));
	}

	/// <summary>Open a short position using market order.</summary>
	public void Sell()
	{
		SellMarket(OrderVolume + Math.Abs(Position));
	}

	/// <summary>Close any open position.</summary>
	public void CloseAll()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}

	/// <summary>Increase trading volume by one step.</summary>
	public void IncreaseVolume()
	{
		OrderVolume += 0.01m;
		LogInfo($"Volume increased to {OrderVolume}");
	}

	/// <summary>Decrease trading volume by one step.</summary>
	public void DecreaseVolume()
	{
		OrderVolume = Math.Max(0.01m, OrderVolume - 0.01m);
		LogInfo($"Volume decreased to {OrderVolume}");
	}

}