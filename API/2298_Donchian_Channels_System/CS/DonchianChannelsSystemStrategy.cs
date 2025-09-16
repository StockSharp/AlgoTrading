using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Channels System strategy.
/// Opens long on breakout above shifted upper band and short on breakout below shifted lower band.
/// </summary>
public class DonchianChannelsSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<DataType> _candleType;
	
	private readonly Queue<decimal> _upperBuffer = new();
	private readonly Queue<decimal> _lowerBuffer = new();
	private decimal _prevClose;
	
	/// <summary>
	/// Lookback period for Donchian Channel.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}
	
	/// <summary>
	/// Bars offset for breakout evaluation.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}
	
	/// <summary>
	/// Candle timeframe for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize parameters for the strategy.
	/// </summary>
	public DonchianChannelsSystemStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
		.SetDisplay("Donchian Period", "Lookback period for Donchian Channel", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);
		
		_shift = Param(nameof(Shift), 2)
		.SetDisplay("Shift", "Bars offset for breakout evaluation", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candle timeframe for analysis", "General");
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
		_upperBuffer.Clear();
		_lowerBuffer.Clear();
		_prevClose = default;
		
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var donchian = new DonchianChannels { Length = DonchianPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(donchian, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var dc = (DonchianChannelsValue)donchianValue;
		
		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
		return;
		
		_upperBuffer.Enqueue(upper);
		_lowerBuffer.Enqueue(lower);
		
		if (_upperBuffer.Count > Shift + 1)
		{
			_upperBuffer.Dequeue();
			_lowerBuffer.Dequeue();
		}
		
		if (_upperBuffer.Count <= Shift)
		{
			_prevClose = candle.ClosePrice;
			return;
		}
		
		var shiftedUpper = _upperBuffer.Peek();
		var shiftedLower = _lowerBuffer.Peek();
		
		var upBreak = candle.ClosePrice > shiftedUpper && _prevClose <= shiftedUpper;
		var dnBreak = candle.ClosePrice < shiftedLower && _prevClose >= shiftedLower;
		
		var volume = Volume + Math.Abs(Position);
		
		if (upBreak && Position <= 0)
		{
			BuyMarket(volume);
			LogInfo($"Buy signal: close {candle.ClosePrice} > shifted upper {shiftedUpper}");
		}
		else if (dnBreak && Position >= 0)
		{
			SellMarket(volume);
			LogInfo($"Sell signal: close {candle.ClosePrice} < shifted lower {shiftedLower}");
		}
		
		_prevClose = candle.ClosePrice;
	}
}
