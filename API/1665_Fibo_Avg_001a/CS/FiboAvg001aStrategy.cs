using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossover of two smoothed moving averages using a Fibonacci offset.
/// </summary>
public class FiboAvg001aStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fiboNumPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<int> _percentMm;
	private readonly StrategyParam<decimal> _lotSize;
	
	private SmoothedMovingAverage? _fastMa;
	private SmoothedMovingAverage? _slowMa;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _trailPrice;
	private decimal _priceStep;
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Additional length added to the slow moving average.
	/// </summary>
	public int FiboNumPeriod { get => _fiboNumPeriod.Value; set => _fiboNumPeriod.Value = value; }
	
	/// <summary>
	/// Base moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	
	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	
	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	
	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	
	/// <summary>
	/// Enable simple money management.
	/// </summary>
	public bool UseMoneyManagement { get => _useMoneyManagement.Value; set => _useMoneyManagement.Value = value; }
	
	/// <summary>
	/// Percentage of portfolio used when money management is enabled.
	/// </summary>
	public int PercentMm { get => _percentMm.Value; set => _percentMm.Value = value; }
	
	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal LotSize { get => _lotSize.Value; set => _lotSize.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="FiboAvg001aStrategy"/>.
	/// </summary>
	public FiboAvg001aStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_fiboNumPeriod = Param(nameof(FiboNumPeriod), 11)
		.SetGreaterThanZero()
		.SetDisplay("Fibo Period", "Additional length for slow MA", "Indicator");
		
		_maPeriod = Param(nameof(MaPeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Base moving average period", "Indicator");
		
		_trailingStop = Param(nameof(TrailingStop), 140m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop", "Trailing stop in price steps", "Risk");
		
		_takeProfit = Param(nameof(TakeProfit), 999m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit in price steps", "Risk");
		
		_stopLoss = Param(nameof(StopLoss), 399m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");
		
		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Use MM", "Enable money management", "Volume");
		
		_percentMm = Param(nameof(PercentMm), 10)
		.SetGreaterThanZero()
		.SetDisplay("Percent MM", "Portfolio percentage for volume", "Volume");
		
		_lotSize = Param(nameof(LotSize), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Default order volume", "Volume");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_priceStep = Security.PriceStep ?? 1m;
		
		_fastMa = new SmoothedMovingAverage { Length = MaPeriod };
		_slowMa = new SmoothedMovingAverage { Length = MaPeriod + FiboNumPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, ProcessCandle)
		.Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var price = candle.ClosePrice;
		
		// Long entry condition
		if (_prevFast < _prevSlow && fast > slow && Position <= 0)
		{
			var volume = GetVolume(price);
			BuyMarket(volume + (Position < 0 ? Math.Abs(Position) : 0));
			
			_entryPrice = price;
			_stopPrice = price - StopLoss * _priceStep;
			_takePrice = price + TakeProfit * _priceStep;
			_trailPrice = price - TrailingStop * _priceStep;
		}
		// Short entry condition
		else if (_prevFast > _prevSlow && fast < slow && Position >= 0)
		{
			var volume = GetVolume(price);
			SellMarket(volume + (Position > 0 ? Position : 0));
			
			_entryPrice = price;
			_stopPrice = price + StopLoss * _priceStep;
			_takePrice = price - TakeProfit * _priceStep;
			_trailPrice = price + TrailingStop * _priceStep;
		}
		
		// Manage long position
		if (Position > 0)
		{
			if (price <= _stopPrice || price >= _takePrice)
			SellMarket(Position);
			else
			{
				var newTrail = price - TrailingStop * _priceStep;
				if (newTrail > _trailPrice)
				_trailPrice = newTrail;
				
				if (price <= _trailPrice)
				SellMarket(Position);
			}
		}
		// Manage short position
		else if (Position < 0)
		{
			if (price >= _stopPrice || price <= _takePrice)
			BuyMarket(Math.Abs(Position));
			else
			{
				var newTrail = price + TrailingStop * _priceStep;
				if (newTrail < _trailPrice)
				_trailPrice = newTrail;
				
				if (price >= _trailPrice)
				BuyMarket(Math.Abs(Position));
			}
		}
		
		_prevFast = fast;
		_prevSlow = slow;
	}
	
	private decimal GetVolume(decimal price)
	{
		if (!UseMoneyManagement || price <= 0 || Portfolio is null)
		return LotSize;
		
		var equity = Portfolio.CurrentValue;
		var amount = equity * PercentMm / 100m;
		var volume = amount / price;
		
		return volume;
	}
}
