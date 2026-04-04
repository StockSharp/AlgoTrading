using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that reacts to fast price shifts (candle-to-candle high/low jumps)
/// and closes trades on profit target or adverse move (stop loss).
/// </summary>
public class LuckyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _shiftPct;
	private readonly StrategyParam<decimal> _profitPct;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private bool _isReady;

	/// <summary>
	/// Minimum percentage shift in high/low to trigger entry.
	/// </summary>
	public decimal ShiftPct
	{
		get => _shiftPct.Value;
		set => _shiftPct.Value = value;
	}

	/// <summary>
	/// Profit target as percentage of entry price.
	/// </summary>
	public decimal ProfitPct
	{
		get => _profitPct.Value;
		set => _profitPct.Value = value;
	}

	/// <summary>
	/// Stop loss as percentage of entry price.
	/// </summary>
	public decimal StopPct
	{
		get => _stopPct.Value;
		set => _stopPct.Value = value;
	}

	/// <summary>
	/// Switch to invert the trading direction.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public LuckyStrategy()
	{
		_shiftPct = Param(nameof(ShiftPct), 1.5m)
			.SetDisplay("Shift %", "Minimum percentage shift in high/low to trigger entry", "Trading")
			.SetOptimize(0.5m, 3.0m, 0.5m);

		_profitPct = Param(nameof(ProfitPct), 2.0m)
			.SetDisplay("Profit %", "Profit target as percentage of entry price", "Risk management")
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_stopPct = Param(nameof(StopPct), 3.0m)
			.SetDisplay("Stop %", "Stop loss as percentage of entry price", "Risk management")
			.SetOptimize(1.0m, 5.0m, 0.5m);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse mode", "Invert the direction of new trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle timeframe", "General");
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

		_previousHigh = null;
		_previousLow = null;
		_entryPrice = 0m;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (!_isReady)
		{
			_previousHigh = high;
			_previousLow = low;
			_isReady = true;
			return;
		}

		// Try to close existing position first
		TryClosePosition(close);

		// Only open new positions if flat
		if (Position == 0 && _previousHigh.HasValue && _previousLow.HasValue)
		{
			var prevH = _previousHigh.Value;
			var prevL = _previousLow.Value;

			// Check for upward breakout: high moved up sharply relative to previous high
			if (prevH > 0m && (high - prevH) / prevH * 100m >= ShiftPct)
			{
				if (Reverse)
					OpenShort(close);
				else
					OpenLong(close);
			}
			// Check for downward breakdown: low moved down sharply relative to previous low
			else if (prevL > 0m && (prevL - low) / prevL * 100m >= ShiftPct)
			{
				if (Reverse)
					OpenLong(close);
				else
					OpenShort(close);
			}
		}

		_previousHigh = high;
		_previousLow = low;
	}

	private void OpenLong(decimal price)
	{
		BuyMarket(Volume);
		_entryPrice = price;
	}

	private void OpenShort(decimal price)
	{
		SellMarket(Volume);
		_entryPrice = price;
	}

	private void TryClosePosition(decimal currentPrice)
	{
		if (Position == 0 || _entryPrice <= 0m)
			return;

		if (Position > 0)
		{
			var pctChange = (currentPrice - _entryPrice) / _entryPrice * 100m;

			if (pctChange >= ProfitPct || pctChange <= -StopPct)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var pctChange = (_entryPrice - currentPrice) / _entryPrice * 100m;

			if (pctChange >= ProfitPct || pctChange <= -StopPct)
				BuyMarket(Math.Abs(Position));
		}
	}
}
