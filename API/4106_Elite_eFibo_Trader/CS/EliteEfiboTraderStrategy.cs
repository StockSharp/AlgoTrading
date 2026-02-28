using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Elite eFibo Trader: MA crossover with RSI filter and grid averaging.
/// Adds to winning positions on pullbacks using Fibonacci-style scaling.
/// </summary>
public class EliteEfiboTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private int _addCount;

	public EliteEfiboTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast MA", "Fast SMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow MA", "Slow SMA period.", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI filter period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_addCount = 0;

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
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

		// Position management
		if (Position > 0)
		{
			// Take profit or stop
			if (close >= _entryPrice + atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
				_addCount = 0;
			}
			else if (close <= _entryPrice - atrVal * 2m)
			{
				SellMarket();
				_entryPrice = 0;
				_addCount = 0;
			}
			else if (_addCount < 2 && close <= _entryPrice - atrVal * 0.8m && rsiVal < 40)
			{
				// Fibonacci add: buy more on pullback
				_entryPrice = (_entryPrice + close) / 2m;
				_addCount++;
				BuyMarket();
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
				_addCount = 0;
			}
			else if (close >= _entryPrice + atrVal * 2m)
			{
				BuyMarket();
				_entryPrice = 0;
				_addCount = 0;
			}
			else if (_addCount < 2 && close >= _entryPrice + atrVal * 0.8m && rsiVal > 60)
			{
				_entryPrice = (_entryPrice + close) / 2m;
				_addCount++;
				SellMarket();
			}
		}

		// Entry: MA crossover with RSI confirmation
		if (Position == 0)
		{
			if (_prevFast <= _prevSlow && fastVal > slowVal && rsiVal > 50)
			{
				_entryPrice = close;
				_addCount = 0;
				BuyMarket();
			}
			else if (_prevFast >= _prevSlow && fastVal < slowVal && rsiVal < 50)
			{
				_entryPrice = close;
				_addCount = 0;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
