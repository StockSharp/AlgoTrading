using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomly buys or sells on each candle when no position is open.
/// Applies fixed take profit and stop loss via StartProtection.
/// </summary>
public class RandomTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<int> _cooldown;
	private readonly StrategyParam<DataType> _candleType;

	private Random _random;
	private int _candleCount;

	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public int Cooldown { get => _cooldown.Value; set => _cooldown.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RandomTraderStrategy()
	{
		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetDisplay("Take Profit %", "Target profit percentage", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldown = Param(nameof(Cooldown), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown", "Candles between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_random = new Random(42);
		_candleCount = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent)
		);

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

		_candleCount++;

		if (Position != 0)
			return;

		if (_candleCount < Cooldown)
			return;

		_candleCount = 0;

		// Randomly choose direction
		if (_random.NextDouble() > 0.5)
			BuyMarket();
		else
			SellMarket();
	}
}
