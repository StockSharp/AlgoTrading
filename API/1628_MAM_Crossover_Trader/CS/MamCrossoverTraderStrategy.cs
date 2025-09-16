using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;
/// <summary>
/// MAM crossover using SMA of close and open prices.
/// </summary>

public class MamCrossoverTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage? _closeSma;
	private SimpleMovingAverage? _openSma;

	private decimal? _prevDiff1; // difference at previous bar
	private decimal? _prevDiff2; // difference two bars ago

	public MamCrossoverTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");

		_stopLossTicks = Param(nameof(StopLossTicks), 40)
			.SetDisplay("Stop Loss (ticks)", "Protective stop in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 190)
			.SetDisplay("Take Profit (ticks)", "Profit target in ticks", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeSma = new SimpleMovingAverage { Length = MaPeriod };
		_openSma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeSma, ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openSmaValue = _openSma!.Process(candle.OpenPrice);
		if (!openSmaValue.IsFinal || !openSmaValue.TryGetValue(out decimal openSma))
			return;

		var diff = closeSma - openSma; // positive when close average above open

		if (_prevDiff1.HasValue && _prevDiff2.HasValue) // wait for enough history
		{
			var crossUp = _prevDiff2 < 0 && _prevDiff1 > 0 && diff > 0; // bullish pattern
			var crossDown = _prevDiff2 > 0 && _prevDiff1 < 0 && diff < 0; // bearish pattern

			if (crossUp)
			{
				if (Position < 0)
					ClosePosition();
				if (Position == 0)
					BuyMarket();
			}
			else if (crossDown)
			{
				if (Position > 0)
					ClosePosition();
				if (Position == 0)
					SellMarket();
			}
		}

		_prevDiff2 = _prevDiff1; // shift history
		_prevDiff1 = diff;
	}
}
