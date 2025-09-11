using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Threshold Strategy.
/// Buys when CCI falls below threshold and exits when price exceeds previous close.
/// Optional stop loss and take profit in points.
/// </summary>
public class CciThresholdStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _buyThreshold;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevClose;
	
	/// <summary>
	/// Lookback period for CCI calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}
	
	/// <summary>
	/// CCI value to trigger long entry.
	/// </summary>
	public int BuyThreshold
	{
		get => _buyThreshold.Value;
		set => _buyThreshold.Value = value;
	}
	
	/// <summary>
	/// Stop loss in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}
	
	/// <summary>
	/// Take profit in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}
	
	/// <summary>
	/// Enable stop loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}
	
	/// <summary>
	/// Enable take profit protection.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}
	
	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="CciThresholdStrategy"/>.
	/// </summary>
	public CciThresholdStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 12)
			.SetDisplay("CCI Lookback Period", "Lookback period for CCI calculation", "General")
			.SetRange(5, 30)
			.SetCanOptimize(true);
		
		_buyThreshold = Param(nameof(BuyThreshold), -90)
			.SetDisplay("CCI Buy Threshold", "CCI threshold to enter long", "General")
			.SetRange(-150, -50)
			.SetCanOptimize(true);
		
		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
			.SetRange(50m, 200m)
			.SetCanOptimize(true);
		
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
			.SetRange(50m, 300m)
			.SetCanOptimize(true);
		
		_useStopLoss = Param(nameof(UseStopLoss), false)
			.SetDisplay("Enable Stop Loss", "Use stop loss", "Risk Management");
		
		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Enable Take Profit", "Use take profit", "Risk Management");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		
		_prevClose = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection(
			takeProfit: UseTakeProfit ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null,
			stopLoss: UseStopLoss ? new Unit(StopLossPoints, UnitTypes.Absolute) : null,
			isStopTrailing: false,
			useMarketOrders: true
		);
		
		var cci = new CommodityChannelIndex { Length = LookbackPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.Bind(cci, ProcessCandle)
			.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}
		
		if (cciValue < BuyThreshold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		
		if (candle.ClosePrice > _prevClose && Position > 0)
		{
			SellMarket(Position);
		}
		
		_prevClose = candle.ClosePrice;
	}
}
