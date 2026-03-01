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
/// Market monitoring strategy that tracks price metrics and trades on significant
/// spread changes. Simplified from the MetaTrader "Symbol Swap" display panel.
/// </summary>
public class SymbolSwapStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _spreadThreshold;

	private SimpleMovingAverage _sma;
	private decimal _entryPrice;

	/// <summary>
	/// Candle type for monitoring.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// SMA period for trend detection.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Price deviation threshold for entry signals.
	/// </summary>
	public decimal SpreadThreshold
	{
		get => _spreadThreshold.Value;
		set => _spreadThreshold.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SymbolSwapStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for signals", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Moving average period", "Indicators");

		_spreadThreshold = Param(nameof(SpreadThreshold), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Spread Threshold", "Price deviation from SMA to trigger entry", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = SmaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		var price = candle.ClosePrice;

		// Exit on mean reversion
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			// Exit on profit or loss threshold
			if (pnl >= SpreadThreshold || pnl <= -SpreadThreshold * 2m)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				return;
			}
		}

		// Entry on deviation
		if (Position == 0)
		{
			var deviation = price - smaValue;

			if (deviation > SpreadThreshold)
			{
				SellMarket();
				_entryPrice = price;
			}
			else if (deviation < -SpreadThreshold)
			{
				BuyMarket();
				_entryPrice = price;
			}
		}
	}
}
