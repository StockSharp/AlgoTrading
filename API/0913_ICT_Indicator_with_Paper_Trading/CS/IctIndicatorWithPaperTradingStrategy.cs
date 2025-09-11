using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple ICT order block strategy with paper trading exit.
/// </summary>
public class IctIndicatorWithPaperTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal? _orderBlockHigh;
	private decimal? _orderBlockLow;
	private decimal? _prevOrderBlockHigh;
	private decimal? _prevOrderBlockLow;
	private decimal? _prevHigh;
	private decimal? _prevPrevHigh;
	private decimal? _prevLow;
	private decimal? _prevPrevLow;
	private decimal? _prevClose;
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of the <see cref="IctIndicatorWithPaperTradingStrategy"/> class.
	/// </summary>
	public IctIndicatorWithPaperTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_orderBlockHigh = null;
		_orderBlockLow = null;
		_prevOrderBlockHigh = null;
		_prevOrderBlockLow = null;
		_prevHigh = null;
		_prevPrevHigh = null;
		_prevLow = null;
		_prevPrevLow = null;
		_prevClose = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (_prevHigh is decimal prevHigh && _prevPrevHigh is decimal prevPrevHigh && prevHigh <= prevPrevHigh && candle.HighPrice > prevHigh)
		_orderBlockHigh = candle.HighPrice;
		
		if (_prevLow is decimal prevLow && _prevPrevLow is decimal prevPrevLow && prevLow <= prevPrevLow && candle.LowPrice > prevLow)
		_orderBlockLow = candle.LowPrice;
		
		var buySignal = _prevClose is decimal prevClose && _prevOrderBlockHigh is decimal prevObh && _orderBlockHigh is decimal obh && prevClose <= prevObh && candle.ClosePrice > obh;
		var sellSignal = _prevClose is decimal prevClose2 && _prevOrderBlockLow is decimal prevObl && _orderBlockLow is decimal obl && prevObl <= prevClose2 && obl > candle.ClosePrice;
		
		if (buySignal && Position <= 0)
		BuyMarket();
		else if (sellSignal && Position > 0)
		SellMarket(Math.Abs(Position));
		
		_prevPrevHigh = _prevHigh;
		_prevHigh = candle.HighPrice;
		_prevPrevLow = _prevLow;
		_prevLow = candle.LowPrice;
		_prevOrderBlockHigh = _orderBlockHigh;
		_prevOrderBlockLow = _orderBlockLow;
		_prevClose = candle.ClosePrice;
	}
}
