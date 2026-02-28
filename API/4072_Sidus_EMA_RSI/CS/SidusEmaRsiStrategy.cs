using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sidus EMA + RSI strategy: fast EMA crosses slow EMA confirmed by RSI above/below 50.
/// </summary>
public class SidusEmaRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;

	public SidusEmaRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 12)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetDisplay("RSI Period", "RSI period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		var bullishCross = _prevFast <= _prevSlow && fastValue > slowValue;
		var bearishCross = _prevFast >= _prevSlow && fastValue < slowValue;

		// Exit existing positions on opposite cross
		if (Position > 0 && bearishCross)
		{
			SellMarket();
		}
		else if (Position < 0 && bullishCross)
		{
			BuyMarket();
		}

		// Entry on crossover confirmed by RSI
		if (Position == 0)
		{
			if (bullishCross && rsiValue > 50)
			{
				BuyMarket();
			}
			else if (bearishCross && rsiValue < 50)
			{
				SellMarket();
			}
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
