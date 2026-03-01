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
/// Opens positions based on SMA trend and closes when floating PnL reaches a profit threshold.
/// Simplified from the "Close all positions" utility expert.
/// </summary>
public class CloseAllPositionsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaPeriod;

	private SimpleMovingAverage _sma;
	private decimal _entryPrice;

	/// <summary>
	/// Minimum floating profit that triggers position close.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Candle type used to detect new bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// SMA period for entry signals.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CloseAllPositionsStrategy()
	{
		_profitThreshold = Param(nameof(ProfitThreshold), 10m)
			.SetDisplay("Profit Threshold", "Floating profit required to close position", "General")
			.SetOptimize(5m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for periodic checks", "General");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Moving average period for entry signals", "Indicators");
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

		// Check profit threshold for exit
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			if (ProfitThreshold > 0m && pnl >= ProfitThreshold)
			{
				LogInfo($"Floating profit {pnl:F2} reached threshold {ProfitThreshold:F2}. Closing position.");

				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				return;
			}

			// Also exit on large loss
			if (pnl <= -ProfitThreshold * 2m)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				return;
			}
		}

		// Entry: trend following
		if (Position == 0)
		{
			if (price > smaValue)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (price < smaValue)
			{
				SellMarket();
				_entryPrice = price;
			}
		}
	}
}
