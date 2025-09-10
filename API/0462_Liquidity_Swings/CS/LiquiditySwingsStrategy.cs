using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Swings strategy.
/// Uses recent pivot highs and lows as resistance and support levels.
/// Enters when price crosses these levels with 1:2 risk-reward.
/// </summary>
public class LiquiditySwingsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _stopLossBuffer;

	private readonly List<decimal> _highBuffer = new();
	private readonly List<decimal> _lowBuffer = new();

	private decimal _resistance;
	private decimal _support;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _prevHigh;
	private decimal _prevLow;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Pivot lookback period.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Stop loss buffer percent.
	/// </summary>
	public decimal StopLossBuffer
	{
		get => _stopLossBuffer.Value;
		set => _stopLossBuffer.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LiquiditySwingsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookback = Param(nameof(Lookback), 5)
		.SetGreaterThanZero()
		.SetDisplay("Pivot Lookback", "Pivot detection lookback", "General")
		.SetCanOptimize(true)
		.SetOptimize(3, 10, 1);

		_stopLossBuffer = Param(nameof(StopLossBuffer), 0.5m)
		.SetRange(0.1m, 5m)
		.SetDisplay("Stop Loss Buffer %", "Additional stop loss buffer", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.0m, 0.1m);
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

		_highBuffer.Clear();
		_lowBuffer.Clear();
		_resistance = default;
		_support = default;
		_entryPrice = default;
		_stopPrice = default;
		_takeProfitPrice = default;
		_prevHigh = default;
		_prevLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		UpdatePivotLevels(candle);

		var buySignal = _prevLow < _support && candle.LowPrice > _support && candle.ClosePrice < _resistance;
		var sellSignal = _prevHigh > _resistance && candle.HighPrice < _resistance && candle.ClosePrice > _support;

		var slLong = _support * (1 - StopLossBuffer / 100m);
		var slShort = _resistance * (1 + StopLossBuffer / 100m);
		var stopLong = candle.ClosePrice < _support;
		var stopShort = candle.ClosePrice > _resistance;

		if (buySignal && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = stopLong ? _support : slLong;
			_takeProfitPrice = _entryPrice + 2m * (_entryPrice - _stopPrice);
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_stopPrice = stopShort ? _resistance : slShort;
			_takeProfitPrice = _entryPrice - 2m * (_stopPrice - _entryPrice);
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.ClosePrice < _support)
			SellMarket(Math.Abs(Position));
			else if (candle.HighPrice >= _takeProfitPrice)
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.ClosePrice > _resistance)
			BuyMarket(Math.Abs(Position));
			else if (candle.LowPrice <= _takeProfitPrice)
			BuyMarket(Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}

	private void UpdatePivotLevels(ICandleMessage candle)
	{
		var size = Lookback * 2 + 1;

		_highBuffer.Add(candle.HighPrice);
		_lowBuffer.Add(candle.LowPrice);

		if (_highBuffer.Count > size)
		_highBuffer.RemoveAt(0);

		if (_lowBuffer.Count > size)
		_lowBuffer.RemoveAt(0);

		if (_highBuffer.Count == size)
		{
			var center = Lookback;
			var candidate = _highBuffer[center];
			var isPivot = true;

			for (var i = 0; i < size; i++)
			{
				if (i == center)
				continue;
				if (_highBuffer[i] >= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
			_resistance = candidate;
		}

		if (_lowBuffer.Count == size)
		{
			var center = Lookback;
			var candidate = _lowBuffer[center];
			var isPivot = true;

			for (var i = 0; i < size; i++)
			{
				if (i == center)
				continue;
				if (_lowBuffer[i] <= candidate)
				{
					isPivot = false;
					break;
				}
			}

			if (isPivot)
			_support = candidate;
		}
	}
}
