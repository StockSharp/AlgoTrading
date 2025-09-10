using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class Vietnamese3xSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastAtrLength;
	private readonly StrategyParam<decimal> _fastMultiplier;
	private readonly StrategyParam<int> _mediumAtrLength;
	private readonly StrategyParam<decimal> _mediumMultiplier;
	private readonly StrategyParam<int> _slowAtrLength;
	private readonly StrategyParam<decimal> _slowMultiplier;
	private readonly StrategyParam<bool> _useHighestOfTwoRedCandles;
	private readonly StrategyParam<bool> _useEntryStopLoss;
	private readonly StrategyParam<bool> _useAllDowntrendExit;
	private readonly StrategyParam<bool> _useAvgPriceInLoss;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _highestGreen;
	private bool _breakEvenActive;
	private decimal _avgEntryPrice;
	private int _entryCount;
	
	public int FastAtrLength { get => _fastAtrLength.Value; set => _fastAtrLength.Value = value; }
	public decimal FastMultiplier { get => _fastMultiplier.Value; set => _fastMultiplier.Value = value; }
	public int MediumAtrLength { get => _mediumAtrLength.Value; set => _mediumAtrLength.Value = value; }
	public decimal MediumMultiplier { get => _mediumMultiplier.Value; set => _mediumMultiplier.Value = value; }
	public int SlowAtrLength { get => _slowAtrLength.Value; set => _slowAtrLength.Value = value; }
	public decimal SlowMultiplier { get => _slowMultiplier.Value; set => _slowMultiplier.Value = value; }
	public bool UseHighestOfTwoRedCandles { get => _useHighestOfTwoRedCandles.Value; set => _useHighestOfTwoRedCandles.Value = value; }
	public bool UseEntryStopLoss { get => _useEntryStopLoss.Value; set => _useEntryStopLoss.Value = value; }
	public bool UseAllDowntrendExit { get => _useAllDowntrendExit.Value; set => _useAllDowntrendExit.Value = value; }
	public bool UseAvgPriceInLoss { get => _useAvgPriceInLoss.Value; set => _useAvgPriceInLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public Vietnamese3xSupertrendStrategy()
	{
		_fastAtrLength = Param(nameof(FastAtrLength), 10)
		.SetDisplay("Fast ATR Length", "ATR length for fast SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_fastMultiplier = Param(nameof(FastMultiplier), 1m)
		.SetDisplay("Fast Multiplier", "ATR multiplier for fast SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);
		
		_mediumAtrLength = Param(nameof(MediumAtrLength), 11)
		.SetDisplay("Medium ATR Length", "ATR length for medium SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		
		_mediumMultiplier = Param(nameof(MediumMultiplier), 2m)
		.SetDisplay("Medium Multiplier", "ATR multiplier for medium SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(1m, 4m, 0.5m);
		
		_slowAtrLength = Param(nameof(SlowAtrLength), 12)
		.SetDisplay("Slow ATR Length", "ATR length for slow SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);
		
		_slowMultiplier = Param(nameof(SlowMultiplier), 3m)
		.SetDisplay("Slow Multiplier", "ATR multiplier for slow SuperTrend", "SuperTrend")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_useHighestOfTwoRedCandles = Param(nameof(UseHighestOfTwoRedCandles), false)
		.SetDisplay("Use Highest of Two Red Candles", "Use highest high of two red candles", "Setup");
		
		_useEntryStopLoss = Param(nameof(UseEntryStopLoss), true)
		.SetDisplay("Use Entry Stop Loss", "Activate break-even stop", "Risk");
		
		_useAllDowntrendExit = Param(nameof(UseAllDowntrendExit), true)
		.SetDisplay("Use All Downtrend Exit", "Exit when all SuperTrends turn up with a red candle", "Exit");
		
		_useAvgPriceInLoss = Param(nameof(UseAvgPriceInLoss), true)
		.SetDisplay("Use Avg Price In Loss", "Exit when average entry price is above close", "Exit");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highestGreen = 0;
		_breakEvenActive = false;
		_avgEntryPrice = 0;
		_entryCount = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fast = new SuperTrend { Length = FastAtrLength, Multiplier = FastMultiplier };
		var medium = new SuperTrend { Length = MediumAtrLength, Multiplier = MediumMultiplier };
		var slow = new SuperTrend { Length = SlowAtrLength, Multiplier = SlowMultiplier };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(fast, medium, slow, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, medium);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue medVal, IIndicatorValue slowVal)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var fast = (SuperTrendIndicatorValue)fastVal;
		var medium = (SuperTrendIndicatorValue)medVal;
		var slow = (SuperTrendIndicatorValue)slowVal;
		
		var dir1 = fast.IsUpTrend ? 1 : -1;
		var dir2 = medium.IsUpTrend ? 1 : -1;
		var dir3 = slow.IsUpTrend ? 1 : -1;
		
		if (dir1 < 0 && _highestGreen == 0 && (!UseHighestOfTwoRedCandles || candle.ClosePrice < candle.OpenPrice))
		_highestGreen = candle.HighPrice;
		if (_highestGreen > 0 && (!UseHighestOfTwoRedCandles || candle.ClosePrice < candle.OpenPrice))
		_highestGreen = Math.Max(_highestGreen, candle.HighPrice);
		if (dir1 >= 0)
		_highestGreen = 0;
		
		if (UseEntryStopLoss && dir1 > 0 && dir2 < 0 && dir3 < 0 && Position > 0)
		{
			if (!_breakEvenActive && candle.LowPrice > _avgEntryPrice)
			_breakEvenActive = true;
			if (_breakEvenActive && candle.LowPrice <= _avgEntryPrice)
			{
				SellMarket(Position);
				ResetEntries();
				return;
			}
		}
		
		if (UseAllDowntrendExit && dir3 > 0 && dir2 > 0 && dir1 > 0 && candle.ClosePrice < candle.OpenPrice && Position > 0)
		{
			SellMarket(Position);
			ResetEntries();
			return;
		}
		
		if (UseAvgPriceInLoss && Position > 0 && _avgEntryPrice > candle.ClosePrice)
		{
			SellMarket(Position);
			ResetEntries();
			return;
		}
		
		if (_entryCount < 3)
		{
			if (dir3 < 0)
			{
				if (dir2 > 0 && dir1 < 0)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
				}
				else if (dir2 < 0 && candle.ClosePrice > fast.Value)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
				}
			}
			else
			{
				if (dir1 < 0 && _highestGreen > 0 && candle.ClosePrice > _highestGreen)
				{
					BuyMarket(Volume);
					AddEntry(candle.ClosePrice);
				}
			}
		}
	}
	
	private void AddEntry(decimal price)
	{
		_avgEntryPrice = (_avgEntryPrice * _entryCount + price) / (_entryCount + 1);
		_entryCount++;
	}
	
	private void ResetEntries()
	{
		_avgEntryPrice = 0;
		_entryCount = 0;
		_breakEvenActive = false;
	}
}
