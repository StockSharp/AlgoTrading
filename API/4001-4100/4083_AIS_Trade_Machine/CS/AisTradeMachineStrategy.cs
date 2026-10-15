using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AIS Trade Machine: EMA crossover strategy with ATR-based risk management.
/// Entry on EMA cross confirmed by RSI, exit on reversal or ATR stop.
/// </summary>
public class AisTradeMachineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopMultiplier;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public AisTradeMachineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_stopMultiplier = Param(nameof(StopMultiplier), 2.0m)
			.SetDisplay("Stop Multiplier", "ATR multiplier for stop.", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal StopMultiplier
	{
		get => _stopMultiplier.Value;
		set => _stopMultiplier.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_stopPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_stopPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;
		var stopDist = atrVal * StopMultiplier;

		var bullishCross = _prevFast <= _prevSlow && fastVal > slowVal;
		var bearishCross = _prevFast >= _prevSlow && fastVal < slowVal;

		// Stop management
		if (Position > 0 && _stopPrice > 0 && close <= _stopPrice)
		{
			SellMarket();
			_entryPrice = 0;
			_stopPrice = 0;
		}
		else if (Position < 0 && _stopPrice > 0 && close >= _stopPrice)
		{
			BuyMarket();
			_entryPrice = 0;
			_stopPrice = 0;
		}

		// Exit on opposite cross
		if (Position > 0 && bearishCross)
		{
			SellMarket();
			_entryPrice = 0;
			_stopPrice = 0;
		}
		else if (Position < 0 && bullishCross)
		{
			BuyMarket();
			_entryPrice = 0;
			_stopPrice = 0;
		}

		// Trail stop
		if (Position > 0)
		{
			var trail = close - stopDist;
			if (trail > _stopPrice) _stopPrice = trail;
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			var trail = close + stopDist;
			if (trail < _stopPrice) _stopPrice = trail;
		}

		// Entry on cross + RSI confirmation
		if (Position == 0)
		{
			if (bullishCross && rsiVal > 50)
			{
				_entryPrice = close;
				_stopPrice = close - stopDist;
				BuyMarket();
			}
			else if (bearishCross && rsiVal < 50)
			{
				_entryPrice = close;
				_stopPrice = close + stopDist;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
