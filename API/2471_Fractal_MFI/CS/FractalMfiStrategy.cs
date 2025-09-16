using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Money Flow Index crossover strategy using upper and lower thresholds.
/// Opens or closes positions depending on MFI crossing defined levels.
/// The trading direction can follow the trend or work against it.
/// </summary>
public class FractalMfiStrategy : Strategy
{
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TrendMode> _trend;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	
	private decimal _prevMfi;
	private bool _isPrevSet;
	
	/// <summary>
	/// MFI calculation period.
	/// </summary>
	public int MfiPeriod { get => _mfiPeriod.Value; set => _mfiPeriod.Value = value; }
	
	/// <summary>
	/// Upper threshold for MFI.
	/// </summary>
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }
	
	/// <summary>
	/// Lower threshold for MFI.
	/// </summary>
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }
	
	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Trading direction mode.
	/// </summary>
	public TrendMode Trend { get => _trend.Value; set => _trend.Value = value; }
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	
	/// <summary>
	/// Allow closing long positions on signals.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	
	/// <summary>
	/// Allow closing short positions on signals.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public FractalMfiStrategy()
	{
		_mfiPeriod = Param(nameof(MfiPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("MFI Period", "Length of MFI indicator", "Indicator")
		.SetCanOptimize(true);
		
		_highLevel = Param<decimal>(nameof(HighLevel), 70m)
		.SetDisplay("High Level", "Upper MFI threshold", "Levels")
		.SetCanOptimize(true);
		
		_lowLevel = Param<decimal>(nameof(LowLevel), 30m)
		.SetDisplay("Low Level", "Lower MFI threshold", "Levels")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_trend = Param(nameof(Trend), TrendMode.Direct)
		.SetDisplay("Trend Mode", "Follow or trade against the trend", "General")
		.SetCanOptimize(true);
		
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Buy Open", "Enable long entries", "Signals");
		
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Sell Open", "Enable short entries", "Signals");
		
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Buy Close", "Enable closing longs", "Signals");
		
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Sell Close", "Enable closing shorts", "Signals");
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
		_prevMfi = 0m;
		_isPrevSet = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.Bind(mfi, (candle, currentMfi) =>
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (!mfi.IsFormed)
			{
				_prevMfi = currentMfi;
				return;
			}
			
			if (!_isPrevSet)
			{
				_prevMfi = currentMfi;
				_isPrevSet = true;
				return;
			}
			
			ProcessSignal(candle.ClosePrice, _prevMfi, currentMfi);
			_prevMfi = currentMfi;
		})
		.Start();
		
		StartProtection();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessSignal(decimal price, decimal prev, decimal current)
	{
		if (Trend == TrendMode.Direct)
		{
			if (prev > LowLevel && current <= LowLevel)
			{
				if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
				
				if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			}
			
			if (prev < HighLevel && current >= HighLevel)
			{
				if (BuyPosClose && Position > 0)
				SellMarket(Math.Abs(Position));
				
				if (SellPosOpen && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		else
		{
			if (prev > LowLevel && current <= LowLevel)
			{
				if (BuyPosClose && Position > 0)
				SellMarket(Math.Abs(Position));
				
				if (SellPosOpen && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			}
			
			if (prev < HighLevel && current >= HighLevel)
			{
				if (SellPosClose && Position < 0)
				BuyMarket(Math.Abs(Position));
				
				if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
	}
	
	/// <summary>
	/// Trend mode enumeration.
	/// </summary>
	public enum TrendMode
	{
		/// <summary>
		/// Trade in direction of indicator.
		/// </summary>
		Direct,
		/// <summary>
		/// Trade against indicator direction.
		/// </summary>
		Against
	}
}
