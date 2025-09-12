using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified hamster-bot MRS 2 strategy.
/// </summary>
public class HamsterBotMrs2Strategy : Strategy
	{
	private readonly SimpleMovingAverage _ma = new();
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _shift;
	
	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Shift { get => _shift.Value; set => _shift.Value = value; }
	
	public HamsterBotMrs2Strategy()
	{
	_length = Param(nameof(Length), 3).SetDisplay("MA Length").SetCanOptimize();
	_shift = Param(nameof(Shift), 1m).SetDisplay("Shift").SetCanOptimize();
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);
	
	_ma.Length = Length;
	SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame())
	.Bind(_ma, OnProcess)
	.Start();
	}
	
	private void OnProcess(ICandleMessage candle, decimal ma)
	{
	if (candle.State != CandleStates.Finished)
	return;
	
	var level = ma * Shift;
	
	if (Position <= 0 && candle.ClosePrice <= level)
	BuyLimit(level);
	else if (Position >= 0 && candle.ClosePrice >= level)
	SellLimit(level);
	}
}
