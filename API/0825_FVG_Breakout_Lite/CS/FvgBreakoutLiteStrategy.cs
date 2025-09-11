using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fair value gap breakout strategy.
/// Detects FVG using a three candle pattern and trades the breakout.
/// </summary>
public class FvgBreakoutLiteStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLong;
	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private decimal _gapLower;
	private decimal _gapUpper;
	private int _gapDirection;

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FvgBreakoutLiteStrategy"/>.
	/// </summary>
	public FvgBreakoutLiteStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0m;
		_isLong = false;
		_prev1 = null;
		_prev2 = null;
		_gapDirection = 0;
		_gapLower = 0m;
		_gapUpper = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prev1 == null)
		{
			_prev1 = candle;
			return;
		}

		if (_prev2 == null)
		{
			_prev2 = _prev1;
			_prev1 = candle;
			return;
		}

		var c0 = _prev2;
		var c2 = candle;

		if (c0.HighPrice < c2.LowPrice)
		{
			_gapLower = c0.HighPrice;
			_gapUpper = c2.LowPrice;
			_gapDirection = 1;
		}
		else if (c0.LowPrice > c2.HighPrice)
		{
			_gapLower = c2.HighPrice;
			_gapUpper = c0.LowPrice;
			_gapDirection = -1;
		}

		if (_gapDirection == 1 && candle.ClosePrice > _gapUpper && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_isLong = true;
			_gapDirection = 0;
		}
		else if (_gapDirection == -1 && candle.ClosePrice < _gapLower && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_isLong = false;
			_gapDirection = 0;
		}

		if (Position != 0)
		{
			var stopLossThreshold = _stopLossPercent.Value / 100m;
			if (_isLong)
			{
				var stopPrice = _entryPrice * (1m - stopLossThreshold);
				if (candle.ClosePrice <= stopPrice)
					SellMarket(Math.Abs(Position));
			}
			else
			{
				var stopPrice = _entryPrice * (1m + stopLossThreshold);
				if (candle.ClosePrice >= stopPrice)
					BuyMarket(Math.Abs(Position));
			}
		}

		_prev2 = _prev1;
		_prev1 = candle;
	}
}

