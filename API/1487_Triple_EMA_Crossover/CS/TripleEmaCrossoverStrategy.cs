using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple EMA crossover with stop loss and take profit.
/// </summary>
public class TripleEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _sma1Period;
	private readonly StrategyParam<int> _sma2Period;
	private readonly StrategyParam<int> _sma3Period;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma1;
	private SimpleMovingAverage _sma2;
	private SimpleMovingAverage _sma3;

	private decimal _prevSma1;
	private decimal _prevSma2;

	public TripleEmaCrossoverStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_sma1Period = Param(nameof(Sma1Period), 9)
			.SetDisplay("SMA1 Period", "Period for short SMA", "Indicators");

		_sma2Period = Param(nameof(Sma2Period), 21)
			.SetDisplay("SMA2 Period", "Period for middle SMA", "Indicators");

		_sma3Period = Param(nameof(Sma3Period), 55)
			.SetDisplay("SMA3 Period", "Period for long SMA", "Indicators");

		_stopLossTicks = Param(nameof(StopLossTicks), 200)
			.SetDisplay("Stop Loss (ticks)", "Stop loss in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 200)
			.SetDisplay("Take Profit (ticks)", "Take profit in ticks", "Risk");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Sma1Period
	{
		get => _sma1Period.Value;
		set => _sma1Period.Value = value;
	}

	public int Sma2Period
	{
		get => _sma2Period.Value;
		set => _sma2Period.Value = value;
	}

	public int Sma3Period
	{
		get => _sma3Period.Value;
		set => _sma3Period.Value = value;
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma1 = new SimpleMovingAverage { Length = Sma1Period };
		_sma2 = new SimpleMovingAverage { Length = Sma2Period };
		_sma3 = new SimpleMovingAverage { Length = Sma3Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma1, _sma2, _sma3, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma1);
			DrawIndicator(area, _sma2);
			DrawIndicator(area, _sma3);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
			stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma1, decimal sma2, decimal sma3)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var uptrend = sma1 > sma2 && sma2 > sma3;
		var downtrend = sma1 < sma2 && sma2 < sma3;

		var longCross = _prevSma1 <= _prevSma2 && sma1 > sma2 && uptrend;
		var shortCross = _prevSma1 >= _prevSma2 && sma1 < sma2 && downtrend;

		if (longCross && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCross && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && candle.ClosePrice < sma1)
			SellMarket(Position);
		else if (Position < 0 && candle.ClosePrice > sma1)
			BuyMarket(-Position);

		_prevSma1 = sma1;
		_prevSma2 = sma2;
	}
}
