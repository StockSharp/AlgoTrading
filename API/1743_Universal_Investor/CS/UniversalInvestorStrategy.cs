using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA and LWMA crossover with trend confirmation.
/// Opens long when LWMA is above EMA and both are rising.
/// Opens short when LWMA is below EMA and both are falling.
/// Closes position on opposite crossover.
/// </summary>
public class UniversalInvestorStrategy : Strategy
{
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<int> _decreaseFactor;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _prevEma;
	private decimal? _prevLwma;
	private decimal _entryPrice;
	private bool _isLong;
	private int _lossCount;
	
	/// <summary>
	/// Moving average calculation period.
	/// </summary>
	public int MovingPeriod { get => _movingPeriod.Value; set => _movingPeriod.Value = value; }
	
	/// <summary>
	/// Lot reduction factor after losing trades. 0 disables reduction.
	/// </summary>
	public int DecreaseFactor { get => _decreaseFactor.Value; set => _decreaseFactor.Value = value; }
	
	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="UniversalInvestorStrategy"/> class.
	/// </summary>
	public UniversalInvestorStrategy()
	{
		_movingPeriod = Param(nameof(MovingPeriod), 23)
		.SetGreaterThanZero()
		.SetDisplay("Moving Period", "Smoothing period for EMA and LWMA", "Indicators")
		.SetCanOptimize(true);
		
		_decreaseFactor = Param(nameof(DecreaseFactor), 0)
		.SetDisplay("Decrease Factor", "Lot reduction factor after losses", "Risk Management")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_prevEma = _prevLwma = null;
		_entryPrice = 0;
		_isLong = false;
		_lossCount = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var ema = new ExponentialMovingAverage { Length = MovingPeriod };
		var lwma = new WeightedMovingAverage { Length = MovingPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ema, lwma, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, lwma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal lwmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (_prevEma is null || _prevLwma is null)
		{
			_prevEma = emaValue;
			_prevLwma = lwmaValue;
			return;
		}
		
		var openBuy = lwmaValue > emaValue && lwmaValue > _prevLwma && emaValue > _prevEma;
		var openSell = lwmaValue < emaValue && lwmaValue < _prevLwma && emaValue < _prevEma;
		var closeBuy = lwmaValue < emaValue;
		var closeSell = lwmaValue > emaValue;
		
		if (Position > 0)
		{
			if (closeBuy)
			{
				ClosePosition();
				UpdateLossCount(candle.ClosePrice);
			}
		}
		else if (Position < 0)
		{
			if (closeSell)
			{
				ClosePosition();
				UpdateLossCount(candle.ClosePrice);
			}
		}
		else
		{
			if (openBuy && !openSell && !closeBuy)
			{
				BuyMarket(GetTradeVolume());
				_entryPrice = candle.ClosePrice;
				_isLong = true;
			}
			else if (openSell && !openBuy && !closeSell)
			{
				SellMarket(GetTradeVolume());
				_entryPrice = candle.ClosePrice;
				_isLong = false;
			}
		}
		
		_prevEma = emaValue;
		_prevLwma = lwmaValue;
	}
	
	private decimal GetTradeVolume()
	{
		if (DecreaseFactor <= 0 || _lossCount <= 1)
		return Volume;
		
		var volume = Volume - Volume * _lossCount / DecreaseFactor;
		return volume > 0 ? volume : Volume;
	}
	
	private void UpdateLossCount(decimal closePrice)
	{
		var profit = _isLong ? closePrice - _entryPrice : _entryPrice - closePrice;
		if (profit < 0)
		_lossCount++;
		else if (profit > 0)
		_lossCount = 0;
		
		_entryPrice = 0;
	}
}
