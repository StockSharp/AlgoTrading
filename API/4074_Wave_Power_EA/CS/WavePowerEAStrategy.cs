using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Wave Power strategy using RSI + EMA crossover for entry
/// with grid-like averaging on drawdown.
/// </summary>
public class WavePowerEAStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _gridStepPercent;
	private readonly StrategyParam<int> _maxGridOrders;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private int _gridCount;

	public WavePowerEAStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 12)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period.", "Indicators");

		_gridStepPercent = Param(nameof(GridStepPercent), 0.5m)
			.SetDisplay("Grid Step %", "Price move % to add to position.", "Grid");

		_maxGridOrders = Param(nameof(MaxGridOrders), 5)
			.SetDisplay("Max Grid Orders", "Maximum averaging orders.", "Grid");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal GridStepPercent
	{
		get => _gridStepPercent.Value;
		set => _gridStepPercent.Value = value;
	}

	public int MaxGridOrders
	{
		get => _maxGridOrders.Value;
		set => _maxGridOrders.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_gridCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new ExponentialMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;
		var bullishCross = _prevFast <= _prevSlow && fastVal > slowVal;
		var bearishCross = _prevFast >= _prevSlow && fastVal < slowVal;

		// Exit on opposite cross
		if (Position > 0 && bearishCross)
		{
			SellMarket();
			_gridCount = 0;
			_entryPrice = 0;
		}
		else if (Position < 0 && bullishCross)
		{
			BuyMarket();
			_gridCount = 0;
			_entryPrice = 0;
		}

		// Grid averaging: add to position if price moved against us
		if (Position > 0 && _entryPrice > 0 && _gridCount < MaxGridOrders)
		{
			var dropPercent = (_entryPrice - close) / _entryPrice * 100;
			if (dropPercent >= GridStepPercent * (_gridCount + 1))
			{
				BuyMarket();
				_gridCount++;
			}
		}
		else if (Position < 0 && _entryPrice > 0 && _gridCount < MaxGridOrders)
		{
			var risePercent = (close - _entryPrice) / _entryPrice * 100;
			if (risePercent >= GridStepPercent * (_gridCount + 1))
			{
				SellMarket();
				_gridCount++;
			}
		}

		// New entry
		if (Position == 0)
		{
			if (bullishCross && rsiVal > 50)
			{
				_entryPrice = close;
				_gridCount = 0;
				BuyMarket();
			}
			else if (bearishCross && rsiVal < 50)
			{
				_entryPrice = close;
				_gridCount = 0;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
