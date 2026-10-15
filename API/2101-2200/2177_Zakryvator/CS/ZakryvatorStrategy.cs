using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that opens positions using SMA crossover and closes them
/// when unrealized loss exceeds a volume-based threshold ("Zakryvator" = position closer on loss).
/// </summary>
public class ZakryvatorStrategy : Strategy
{
	private decimal _entryPrice;
	private decimal _lastPrice;
	private bool _prevShortAboveLong;

	private readonly SimpleMovingAverage _smaShort = new() { Length = 50 };
	private readonly SimpleMovingAverage _smaLong = new() { Length = 150 };

	private readonly StrategyParam<int> _shortPeriod;
	private readonly StrategyParam<int> _longPeriod;
	private readonly StrategyParam<decimal> _lossThreshold;

	/// <summary>Short SMA period.</summary>
	public int ShortPeriod { get => _shortPeriod.Value; set => _shortPeriod.Value = value; }

	/// <summary>Long SMA period.</summary>
	public int LongPeriod { get => _longPeriod.Value; set => _longPeriod.Value = value; }

	/// <summary>Maximum unrealized loss before closing position.</summary>
	public decimal LossThreshold { get => _lossThreshold.Value; set => _lossThreshold.Value = value; }

	/// <summary>Constructor.</summary>
	public ZakryvatorStrategy()
	{
		_shortPeriod = Param(nameof(ShortPeriod), 50)
			.SetDisplay("Short SMA", "Short SMA period for entry signal", "Entry");
		_longPeriod = Param(nameof(LongPeriod), 150)
			.SetDisplay("Long SMA", "Long SMA period for entry signal", "Entry");
		_lossThreshold = Param(nameof(LossThreshold), 500m)
			.SetDisplay("Loss Threshold", "Max unrealized loss before closing position", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, TimeSpan.FromMinutes(5).TimeFrame())];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_smaShort.Length = ShortPeriod;
		_smaLong.Length = LongPeriod;

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());

		subscription
			.Bind(_smaShort, _smaLong, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortSma, decimal longSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_smaShort.IsFormed || !_smaLong.IsFormed)
			return;

		_lastPrice = candle.ClosePrice;

		var shortAboveLong = shortSma > longSma;

		// Check loss threshold for open position
		if (Position != 0 && _entryPrice != 0m)
		{
			var openPnL = Position * (_lastPrice - _entryPrice);

			if (openPnL <= -LossThreshold)
			{
				// Close on loss
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_entryPrice = 0m;
				_prevShortAboveLong = shortAboveLong;
				return;
			}
		}

		// SMA crossover entry/exit logic
		var crossUp = shortAboveLong && !_prevShortAboveLong;
		var crossDown = !shortAboveLong && _prevShortAboveLong;

		if (crossUp)
		{
			if (Position < 0)
			{
				BuyMarket();
				_entryPrice = 0m;
			}

			if (Position == 0)
			{
				BuyMarket();
				_entryPrice = _lastPrice;
			}
		}
		else if (crossDown)
		{
			if (Position > 0)
			{
				SellMarket();
				_entryPrice = 0m;
			}

			if (Position == 0)
			{
				SellMarket();
				_entryPrice = _lastPrice;
			}
		}

		_prevShortAboveLong = shortAboveLong;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0m;
		_lastPrice = 0m;
		_prevShortAboveLong = false;

		_smaShort.Reset();
		_smaLong.Reset();
	}
}
