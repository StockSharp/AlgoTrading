using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money Flow Index based strategy that enters long when MFI exits the oversold zone.
/// Places a limit order below the close and cancels it after a specified number of bars.
/// Uses StartProtection for stop-loss and take-profit.
/// </summary>
public class MfiStrategyWithOversoldZoneExitAndAveragingStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiOversoldLevel;
	private readonly StrategyParam<decimal> _longEntryPercentage;
	private readonly StrategyParam<decimal> _stopLossPercentage;
	private readonly StrategyParam<decimal> _exitGainPercentage;
	private readonly StrategyParam<int> _cancelAfterBars;
	private readonly StrategyParam<DataType> _candleType;
	
	private Order _entryOrder;
	private decimal _longEntryPrice;
	private int _barsSinceEntryOrder;
	private bool _inOversoldZone;
	
	/// <summary>
	/// Period for MFI calculation.
	/// </summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	
	/// <summary>
	/// Oversold threshold for MFI.
	/// </summary>
	public decimal MfiOversoldLevel { get => _mfiOversoldLevel.Value; set => _mfiOversoldLevel.Value = value; }
	
	/// <summary>
	/// Percentage below close price for limit entry.
	/// </summary>
	public decimal LongEntryPercentage { get => _longEntryPercentage.Value; set => _longEntryPercentage.Value = value; }
	
	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercentage { get => _stopLossPercentage.Value; set => _stopLossPercentage.Value = value; }
	
	/// <summary>
	/// Take-profit percentage.
	/// </summary>
	public decimal ExitGainPercentage { get => _exitGainPercentage.Value; set => _exitGainPercentage.Value = value; }
	
	/// <summary>
	/// Number of bars after which unfilled order is canceled.
	/// </summary>
	public int CancelAfterBars { get => _cancelAfterBars.Value; set => _cancelAfterBars.Value = value; }
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MfiStrategyWithOversoldZoneExitAndAveragingStrategy"/>.
	/// </summary>
	public MfiStrategyWithOversoldZoneExitAndAveragingStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Period for the MFI indicator", "Indicator");
		_mfiOversoldLevel = Param(nameof(MfiOversoldLevel), 20m)
		.SetRange(0m, 50m)
		.SetDisplay("MFI Oversold", "Oversold level for MFI", "Signal");
		_longEntryPercentage = Param(nameof(LongEntryPercentage), 0.1m)
		.SetRange(0m, 5m)
		.SetDisplay("Entry %", "Percent below close for limit entry", "Trading");
		_stopLossPercentage = Param(nameof(StopLossPercentage), 1m)
		.SetRange(0m, 10m)
		.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");
		_exitGainPercentage = Param(nameof(ExitGainPercentage), 1m)
		.SetRange(0m, 10m)
		.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");
		_cancelAfterBars = Param(nameof(CancelAfterBars), 5)
		.SetRange(1, 100)
		.SetDisplay("Cancel After Bars", "Bars before canceling limit order", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		
		_entryOrder = null;
		_longEntryPrice = 0m;
		_barsSinceEntryOrder = 0;
		_inOversoldZone = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(mfi, ProcessCandle)
		.Start();
		
		StartProtection(
		new Unit(ExitGainPercentage, UnitTypes.Percent),
		new Unit(StopLossPercentage, UnitTypes.Percent)
		);
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (mfiValue < MfiOversoldLevel)
		{
			_inOversoldZone = true;
		}
		else if (_inOversoldZone && mfiValue > MfiOversoldLevel && Position == 0 && _entryOrder == null)
		{
			_inOversoldZone = false;
			_longEntryPrice = candle.ClosePrice * (1 - LongEntryPercentage / 100m);
			_entryOrder = BuyLimit(_longEntryPrice, Volume);
			_barsSinceEntryOrder = 0;
		}
		
		if (_entryOrder != null)
		{
			if (_entryOrder.State == OrderStates.Active)
			{
				_barsSinceEntryOrder++;
				
				if (_barsSinceEntryOrder >= CancelAfterBars && Position == 0)
				{
					CancelOrder(_entryOrder);
					_entryOrder = null;
				}
			}
			else
			{
				_entryOrder = null;
			}
		}
	}
}
