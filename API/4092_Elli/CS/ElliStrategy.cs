using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elli: EMA crossover with ATR momentum confirmation.
/// Fast EMA above slow EMA = bullish, below = bearish.
/// Entry when ATR expansion confirms trend strength.
/// </summary>
public class ElliStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevAtr;
	private decimal _entryPrice;

	public ElliStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 19)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 60)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for momentum.", "Indicators");
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

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_prevAtr = 0;
		_entryPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0 || atrVal <= 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			_prevAtr = atrVal;
			return;
		}

		var close = candle.ClosePrice;

		// Exit: stop or take based on ATR
		if (Position > 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (fastVal < slowVal)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			else if (fastVal > slowVal)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: EMA crossover with ATR expansion
		if (Position == 0)
		{
			var atrRising = atrVal > _prevAtr;

			if (_prevFast <= _prevSlow && fastVal > slowVal && atrRising)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (_prevFast >= _prevSlow && fastVal < slowVal && atrRising)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
		_prevAtr = atrVal;
	}
}
