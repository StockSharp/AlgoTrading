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

	private readonly StrategyParam<int> _signalSeed;
	private int _candleCount;
	private int _signalState;

	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public int Cooldown { get => _cooldown.Value; set => _cooldown.Value = value; }
	public int SignalSeed { get => _signalSeed.Value; set => _signalSeed.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RandomTraderStrategy()
	{
		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetDisplay("Take Profit %", "Target profit percentage", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cooldown = Param(nameof(Cooldown), 25)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown", "Candles between trades", "General");

		_signalSeed = Param(nameof(SignalSeed), 42)
			.SetDisplay("Signal Seed", "Deterministic seed used for direction selection", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_candleCount = 0;
		_signalState = SignalSeed;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_candleCount = 0;
		_signalState = SignalSeed;

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

		_signalState = unchecked(_signalState * 1103515245 + 12345);

		// Use a deterministic pseudo-random sequence to keep clone validation stable.
		if ((_signalState & 1) == 0)
			BuyMarket();
		else
			SellMarket();
	}
}
