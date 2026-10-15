using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MFI strategy with oversold zone exit and averaging.
/// After MFI has been below MfiOversoldLevel, the first candle on which it climbs back above the level places a limit buy
/// LongEntryPercentage percent below the close. An order still unfilled after CancelAfterBars candles is cancelled.
/// Positions are closed by the percent take profit and stop loss of StartProtection.
/// </summary>
public class MfiWithOversoldZoneExitAndAveragingStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiOversoldLevel;
	private readonly StrategyParam<decimal> _longEntryPercentage;
	private readonly StrategyParam<decimal> _stopLossPercentage;
	private readonly StrategyParam<decimal> _exitGainPercentage;
	private readonly StrategyParam<int> _cancelAfterBars;
	private readonly StrategyParam<DataType> _candleType;

	private Order _entryOrder;
	private int _barsSinceOrder;
	private bool _inOversoldZone;

	/// <summary>
	/// Period for MFI calculation.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for MFI.
	/// </summary>
	public decimal MfiOversoldLevel
	{
		get => _mfiOversoldLevel.Value;
		set => _mfiOversoldLevel.Value = value;
	}

	/// <summary>
	/// Percentage below the close for the limit entry.
	/// </summary>
	public decimal LongEntryPercentage
	{
		get => _longEntryPercentage.Value;
		set => _longEntryPercentage.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercentage
	{
		get => _stopLossPercentage.Value;
		set => _stopLossPercentage.Value = value;
	}

	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal ExitGainPercentage
	{
		get => _exitGainPercentage.Value;
		set => _exitGainPercentage.Value = value;
	}

	/// <summary>
	/// Number of bars after which an unfilled order is cancelled.
	/// </summary>
	public int CancelAfterBars
	{
		get => _cancelAfterBars.Value;
		set => _cancelAfterBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MfiWithOversoldZoneExitAndAveragingStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period for the MFI indicator", "Indicators");

		_mfiOversoldLevel = Param(nameof(MfiOversoldLevel), 20m)
			.SetDisplay("MFI Oversold", "Oversold level for MFI", "Indicators");

		_longEntryPercentage = Param(nameof(LongEntryPercentage), 0.1m)
			.SetNotNegative()
			.SetDisplay("Entry %", "Percent below close for the limit entry", "Trading");

		_stopLossPercentage = Param(nameof(StopLossPercentage), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_exitGainPercentage = Param(nameof(ExitGainPercentage), 1m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");

		_cancelAfterBars = Param(nameof(CancelAfterBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Cancel After Bars", "Bars before an unfilled limit order is cancelled", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_entryOrder = null;
		_barsSinceOrder = 0;
		_inOversoldZone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var mfi = new MoneyFlowIndex { Length = MfiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		StartProtection(
			ExitGainPercentage > 0 ? new Unit(ExitGainPercentage, UnitTypes.Percent) : new Unit(),
			StopLossPercentage > 0 ? new Unit(StopLossPercentage, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, mfi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_entryOrder != null)
		{
			if (_entryOrder.State is OrderStates.Active or OrderStates.Pending)
			{
				_barsSinceOrder++;

				if (_barsSinceOrder >= CancelAfterBars)
				{
					if (_entryOrder.State == OrderStates.Active)
						CancelOrder(_entryOrder);

					_entryOrder = null;
				}
			}
			else
			{
				_entryOrder = null;
			}
		}

		var crossedUp = false;

		if (mfiValue < MfiOversoldLevel)
		{
			_inOversoldZone = true;
		}
		else if (_inOversoldZone && mfiValue > MfiOversoldLevel)
		{
			_inOversoldZone = false;
			crossedUp = true;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Every new signal adds another limit buy, so the position averages down while orders keep filling.
		if (crossedUp && _entryOrder == null)
		{
			var price = candle.ClosePrice * (1m - LongEntryPercentage / 100m);
			var step = Security?.PriceStep ?? 0m;

			if (step > 0)
				price = Math.Floor(price / step) * step;

			_entryOrder = BuyLimit(price, Volume);
			_barsSinceOrder = 0;
		}
	}
}
