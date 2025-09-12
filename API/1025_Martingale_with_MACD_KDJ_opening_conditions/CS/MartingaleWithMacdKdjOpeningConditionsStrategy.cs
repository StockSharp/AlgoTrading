using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy with MACD and KDJ crossover entries.
/// </summary>
public class MartingaleWithMacdKdjOpeningConditionsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _initialOrder;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalSmoothing;
	private readonly StrategyParam<int> _kdjLength;
	private readonly StrategyParam<int> _kdjSmoothK;
	private readonly StrategyParam<int> _kdjSmoothD;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<int> _maxAdditions;
	private readonly StrategyParam<decimal> _addPositionPercent;
	private readonly StrategyParam<decimal> _reboundPercent;
	private readonly StrategyParam<decimal> _addMultiplier;
	private readonly StrategyParam<decimal> _takeProfitTrigger;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevMacdDiff;
	private decimal _prevStochDiff;
	private bool _isFirst = true;
	
	private decimal _baseOrderSize;
	private int _additions;
	private decimal _nextAddPrice;
	private decimal _extremePrice;
	private bool _pending;
	private decimal _entryPrice;
	private decimal _bestPrice;
	
	public decimal InitialOrder { get => _initialOrder.Value; set => _initialOrder.Value = value; }
	public int MacdFastLength { get => _macdFastLength.Value; set => _macdFastLength.Value = value; }
	public int MacdSlowLength { get => _macdSlowLength.Value; set => _macdSlowLength.Value = value; }
	public int MacdSignalSmoothing { get => _macdSignalSmoothing.Value; set => _macdSignalSmoothing.Value = value; }
	public int KdjLength { get => _kdjLength.Value; set => _kdjLength.Value = value; }
	public int KdjSmoothK { get => _kdjSmoothK.Value; set => _kdjSmoothK.Value = value; }
	public int KdjSmoothD { get => _kdjSmoothD.Value; set => _kdjSmoothD.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }
	public int MaxAdditions { get => _maxAdditions.Value; set => _maxAdditions.Value = value; }
	public decimal AddPositionPercent { get => _addPositionPercent.Value; set => _addPositionPercent.Value = value; }
	public decimal ReboundPercent { get => _reboundPercent.Value; set => _reboundPercent.Value = value; }
	public decimal AddMultiplier { get => _addMultiplier.Value; set => _addMultiplier.Value = value; }
	public decimal TakeProfitTrigger { get => _takeProfitTrigger.Value; set => _takeProfitTrigger.Value = value; }
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public MartingaleWithMacdKdjOpeningConditionsStrategy()
	{
		_initialOrder = Param(nameof(InitialOrder), 150m)
		.SetNotNegative()
		.SetDisplay("Initial Order", "Initial order amount", "Trading");
		_macdFastLength = Param(nameof(MacdFastLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast Length", "Fast EMA length for MACD", "Indicators");
		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow Length", "Slow EMA length for MACD", "Indicators");
		_macdSignalSmoothing = Param(nameof(MacdSignalSmoothing), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal Smoothing", "Signal line length for MACD", "Indicators");
		_kdjLength = Param(nameof(KdjLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("KDJ Length", "Stochastic length", "Indicators");
		_kdjSmoothK = Param(nameof(KdjSmoothK), 3)
		.SetGreaterThanZero()
		.SetDisplay("KDJ Smooth K", "Smoothing for %K", "Indicators");
		_kdjSmoothD = Param(nameof(KdjSmoothD), 3)
		.SetGreaterThanZero()
		.SetDisplay("KDJ Smooth D", "Smoothing for %D", "Indicators");
		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long Trades", "Allow long positions", "General");
		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short Trades", "Allow short positions", "General");
		_maxAdditions = Param(nameof(MaxAdditions), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Additions", "Maximum number of additions", "Martingale");
		_addPositionPercent = Param(nameof(AddPositionPercent), 1m)
		.SetNotNegative()
		.SetDisplay("Add Position Percent", "Percent move against position to trigger pending add", "Martingale");
		_reboundPercent = Param(nameof(ReboundPercent), 0.5m)
		.SetNotNegative()
		.SetDisplay("Rebound Percent", "Percent rebound required to add", "Martingale");
		_addMultiplier = Param(nameof(AddMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Add Multiplier", "Multiplier for each additional order", "Martingale");
		_takeProfitTrigger = Param(nameof(TakeProfitTrigger), 2m)
		.SetNotNegative()
		.SetDisplay("Take Profit Trigger", "Percent profit target", "Risk");
		_trailingStopPercent = Param(nameof(TrailingStopPercent), 0.3m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop Percent", "Percent for trailing stop", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 6m)
		.SetNotNegative()
		.SetDisplay("Stop Loss Percent", "Percent for stop loss", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_prevMacdDiff = _prevStochDiff = 0m;
		_isFirst = true;
		ResetState();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalSmoothing }
		};
		
		var stoch = new Stochastic
		{
			Length = KdjLength,
			KPeriod = KdjSmoothK,
			DPeriod = KdjSmoothD
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(macd, stoch, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var stochTyped = (StochasticValue)stochValue;
		
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;
		
		if (stochTyped.K is not decimal k || stochTyped.D is not decimal d)
		return;
		
		var price = candle.ClosePrice;
		
		var macdDiff = macd - signal;
		var stochDiff = k - d;
		var crossUp = !_isFirst && _prevMacdDiff <= 0m && macdDiff > 0m && _prevStochDiff <= 0m && stochDiff > 0m;
		var crossDown = !_isFirst && _prevMacdDiff >= 0m && macdDiff < 0m && _prevStochDiff >= 0m && stochDiff < 0m;
		
		_prevMacdDiff = macdDiff;
		_prevStochDiff = stochDiff;
		_isFirst = false;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position == 0)
		{
			if (EnableLong && crossUp)
			{
				var qty = InitialOrder / price;
				BuyMarket(qty);
				
				_entryPrice = price;
				_baseOrderSize = qty;
				_additions = 0;
				_nextAddPrice = price * (1m - AddPositionPercent / 100m);
				_pending = false;
				_bestPrice = price;
			}
			else if (EnableShort && crossDown)
			{
				var qty = InitialOrder / price;
				SellMarket(qty);
				
				_entryPrice = price;
				_baseOrderSize = qty;
				_additions = 0;
				_nextAddPrice = price * (1m + AddPositionPercent / 100m);
				_pending = false;
				_bestPrice = price;
			}
			
			return;
		}
		
		if (Position > 0)
		ProcessLong(price);
		else if (Position < 0)
		ProcessShort(price);
	}
	
	private void ProcessLong(decimal price)
	{
		if (_additions < MaxAdditions)
		{
			if (price < _nextAddPrice && !_pending)
			{
				_extremePrice = price;
				_pending = true;
			}
			
			if (_pending)
			{
				if (price > _extremePrice * (1m + ReboundPercent / 100m))
				{
					var addQty = _baseOrderSize * (decimal)Math.Pow((double)AddMultiplier, _additions + 1);
					var curPos = Position;
					
					BuyMarket(addQty);
					
					_entryPrice = (_entryPrice * curPos + price * addQty) / (curPos + addQty);
					_additions++;
					_pending = false;
					_nextAddPrice = Math.Min(_nextAddPrice, price) * (1m - AddPositionPercent / 100m);
				}
				else
				{
					_extremePrice = Math.Min(_extremePrice, price);
				}
			}
		}
		
		_bestPrice = Math.Max(_bestPrice, price);
		var trail = _bestPrice * (1m - TrailingStopPercent / 100m);
		var takeProfit = _entryPrice * (1m + TakeProfitTrigger / 100m);
		var stopLoss = _entryPrice * (1m - StopLossPercent / 100m);
		
		if (price >= takeProfit || price <= stopLoss || price <= trail)
		{
			SellMarket(Position);
			ResetState();
		}
	}
	
	private void ProcessShort(decimal price)
	{
		if (_additions < MaxAdditions)
		{
			if (price > _nextAddPrice && !_pending)
			{
				_extremePrice = price;
				_pending = true;
			}
			
			if (_pending)
			{
				if (price < _extremePrice * (1m - ReboundPercent / 100m))
				{
					var addQty = _baseOrderSize * (decimal)Math.Pow((double)AddMultiplier, _additions + 1);
					var curPos = Math.Abs(Position);
					
					SellMarket(addQty);
					
					_entryPrice = (_entryPrice * curPos + price * addQty) / (curPos + addQty);
					_additions++;
					_pending = false;
					_nextAddPrice = Math.Max(_nextAddPrice, price) * (1m + AddPositionPercent / 100m);
				}
				else
				{
					_extremePrice = Math.Max(_extremePrice, price);
				}
			}
		}
		
		_bestPrice = Math.Min(_bestPrice, price);
		var trail = _bestPrice * (1m + TrailingStopPercent / 100m);
		var takeProfit = _entryPrice * (1m - TakeProfitTrigger / 100m);
		var stopLoss = _entryPrice * (1m + StopLossPercent / 100m);
		
		if (price <= takeProfit || price >= stopLoss || price >= trail)
		{
			BuyMarket(-Position);
			ResetState();
		}
	}
	
	private void ResetState()
	{
		_baseOrderSize = 0m;
		_additions = 0;
		_nextAddPrice = 0m;
		_extremePrice = 0m;
		_pending = false;
		_entryPrice = 0m;
		_bestPrice = 0m;
	}
}

