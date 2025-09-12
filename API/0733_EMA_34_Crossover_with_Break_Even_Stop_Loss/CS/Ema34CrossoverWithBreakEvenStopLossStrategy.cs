using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA 34 crossover with break-even stop loss.
/// It enters long when price crosses above EMA and moves stop to break-even at 3R.
/// </summary>
public class Ema34CrossoverWithBreakEvenStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _breakEvenMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevEma;
	private decimal _prevLow;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _risk;
	private bool _inTrade;
	private bool _isFirst = true;
	private bool _isBreakEven;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal TakeProfitMultiplier { get => _takeProfitMultiplier.Value; set => _takeProfitMultiplier.Value = value; }
	public decimal BreakEvenMultiplier { get => _breakEvenMultiplier.Value; set => _breakEvenMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ema34CrossoverWithBreakEvenStopLossStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 34)
			.SetDisplay("EMA Period", "Period for EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 1);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 10m)
			.SetDisplay("Take Profit RR", "Risk reward multiplier for take profit", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2m, 20m, 1m);

		_breakEvenMultiplier = Param(nameof(BreakEvenMultiplier), 3m)
			.SetDisplay("Break Even RR", "Risk reward to move stop to break even", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_inTrade)
		{
		if (!_isFirst && candle.ClosePrice > emaValue && _prevClose <= _prevEma)
		{
		_entryPrice = candle.ClosePrice;
		_stopLoss = _prevLow;
		_risk = _entryPrice - _stopLoss;
		_takeProfit = _entryPrice + _risk * TakeProfitMultiplier;
		BuyMarket();
		_inTrade = true;
		_isBreakEven = false;
		}
		}
		else
		{
		if (!_isBreakEven && candle.ClosePrice >= _entryPrice + _risk * BreakEvenMultiplier)
		{
		_stopLoss = _entryPrice;
		_isBreakEven = true;
		}

		if (candle.LowPrice <= _stopLoss)
		{
		SellMarket();
		_inTrade = false;
		}
		else if (candle.HighPrice >= _takeProfit)
		{
		SellMarket();
		_inTrade = false;
		}
		}

		_prevClose = candle.ClosePrice;
		_prevEma = emaValue;
		_prevLow = candle.LowPrice;
		_isFirst = false;
	}
}
