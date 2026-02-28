using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that detects abnormally large candles and enters in the breakout direction.
/// Uses a simple martingale recovery: after a losing trade, the next entry is taken more aggressively.
/// </summary>
public class MartingaleBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _breakoutMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _rangeBuffer = new decimal[10];
	private int _rangeBufferCount;
	private int _rangeBufferIndex;
	private decimal _rangeBufferSum;

	private decimal _entryPrice;
	private Sides? _entrySide;
	private bool _lastWasLoss;

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public decimal BreakoutMultiplier
	{
		get => _breakoutMultiplier.Value;
		set => _breakoutMultiplier.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MartingaleBreakoutStrategy()
	{
		_lookback = Param(nameof(Lookback), 10)
			.SetDisplay("Lookback", "Number of candles for average range", "General");

		_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2m)
			.SetDisplay("Breakout Mult", "Multiplier above avg range for breakout", "General");

		_takeProfitPct = Param(nameof(TakeProfitPct), 0.5m)
			.SetDisplay("Take Profit %", "Take profit as percentage of entry price", "Trading");

		_stopLossPct = Param(nameof(StopLossPct), 0.3m)
			.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rangeBufferCount = 0;
		_rangeBufferIndex = 0;
		_rangeBufferSum = 0m;
		_entryPrice = 0m;
		_entrySide = null;
		_lastWasLoss = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var closePrice = candle.ClosePrice;

		// Check exit conditions first
		if (Position != 0 && _entryPrice > 0)
		{
			var tp = _lastWasLoss ? TakeProfitPct * 1.5m : TakeProfitPct;
			var sl = StopLossPct;

			if (_entrySide == Sides.Buy)
			{
				var pnlPct = (closePrice - _entryPrice) / _entryPrice * 100m;
				if (pnlPct >= tp || pnlPct <= -sl)
				{
					_lastWasLoss = pnlPct < 0;
					SellMarket();
					_entryPrice = 0;
					_entrySide = null;
					UpdateRangeStatistics(candle);
					return;
				}
			}
			else if (_entrySide == Sides.Sell)
			{
				var pnlPct = (_entryPrice - closePrice) / _entryPrice * 100m;
				if (pnlPct >= tp || pnlPct <= -sl)
				{
					_lastWasLoss = pnlPct < 0;
					BuyMarket();
					_entryPrice = 0;
					_entrySide = null;
					UpdateRangeStatistics(candle);
					return;
				}
			}
		}

		// Entry logic - only when flat
		if (Position == 0)
		{
			var range = candle.HighPrice - candle.LowPrice;

			if (_rangeBufferCount >= _rangeBuffer.Length)
			{
				var avgRange = _rangeBufferSum / _rangeBuffer.Length;

				if (range > avgRange * BreakoutMultiplier)
				{
					var body = candle.ClosePrice - candle.OpenPrice;

					if (body > 0 && body > range * 0.4m)
					{
						// Bullish breakout
						BuyMarket();
						_entryPrice = closePrice;
						_entrySide = Sides.Buy;
					}
					else if (body < 0 && Math.Abs(body) > range * 0.4m)
					{
						// Bearish breakout
						SellMarket();
						_entryPrice = closePrice;
						_entrySide = Sides.Sell;
					}
				}
			}
		}

		UpdateRangeStatistics(candle);
	}

	private void UpdateRangeStatistics(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;

		if (_rangeBufferCount < _rangeBuffer.Length)
		{
			_rangeBuffer[_rangeBufferIndex] = range;
			_rangeBufferSum += range;
			_rangeBufferCount++;
			_rangeBufferIndex = (_rangeBufferIndex + 1) % _rangeBuffer.Length;
			return;
		}

		_rangeBufferSum -= _rangeBuffer[_rangeBufferIndex];
		_rangeBuffer[_rangeBufferIndex] = range;
		_rangeBufferSum += range;
		_rangeBufferIndex = (_rangeBufferIndex + 1) % _rangeBuffer.Length;
	}
}
