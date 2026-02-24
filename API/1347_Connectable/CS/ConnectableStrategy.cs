using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Connectable strategy that trades based on EMA crossover signals
/// with percent-based stop-loss and take-profit management.
/// </summary>
public class ConnectableStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _prevReady;

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

	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ConnectableStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA length", "General");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow EMA", "Slow EMA length", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Percent of entry price for stop loss", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Percent of entry price for take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevReady = false;
		_entryPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check SL/TP for existing position
		if (Position != 0 && _entryPrice > 0)
		{
			var sl = StopLossPercent / 100m;
			var tp = TakeProfitPercent / 100m;

			if (_isLong)
			{
				if (candle.LowPrice <= _entryPrice * (1m - sl) || candle.HighPrice >= _entryPrice * (1m + tp))
				{
					SellMarket();
					_entryPrice = 0;
					_prevFast = fastValue;
					_prevSlow = slowValue;
					_prevReady = true;
					return;
				}
			}
			else
			{
				if (candle.HighPrice >= _entryPrice * (1m + sl) || candle.LowPrice <= _entryPrice * (1m - tp))
				{
					BuyMarket();
					_entryPrice = 0;
					_prevFast = fastValue;
					_prevSlow = slowValue;
					_prevReady = true;
					return;
				}
			}
		}

		if (!_prevReady)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_prevReady = true;
			return;
		}

		// EMA crossover signals
		if (_prevFast <= _prevSlow && fastValue > slowValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_isLong = true;
		}
		else if (_prevFast >= _prevSlow && fastValue < slowValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_isLong = false;
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
