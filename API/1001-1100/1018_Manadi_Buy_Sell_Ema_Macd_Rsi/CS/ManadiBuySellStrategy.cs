using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manadi Buy Sell EMA MACD RSI strategy.
/// The fast EMA crossing above the slow EMA with MACD above its signal line and RSI between RsiLowerLong and RsiUpperLong goes long.
/// The fast EMA crossing below the slow EMA with MACD below its signal line and RSI between RsiLowerShort and RsiUpperShort goes short.
/// An opposite signal reverses the position. TakeProfitPercent and StopLossPercent are fractions of the entry price (0.03 = 3%).
/// </summary>
public class ManadiBuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiUpperLong;
	private readonly StrategyParam<decimal> _rsiLowerLong;
	private readonly StrategyParam<decimal> _rsiUpperShort;
	private readonly StrategyParam<decimal> _rsiLowerShort;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Upper RSI bound for longs.
	/// </summary>
	public decimal RsiUpperLong
	{
		get => _rsiUpperLong.Value;
		set => _rsiUpperLong.Value = value;
	}

	/// <summary>
	/// Lower RSI bound for longs.
	/// </summary>
	public decimal RsiLowerLong
	{
		get => _rsiLowerLong.Value;
		set => _rsiLowerLong.Value = value;
	}

	/// <summary>
	/// Upper RSI bound for shorts.
	/// </summary>
	public decimal RsiUpperShort
	{
		get => _rsiUpperShort.Value;
		set => _rsiUpperShort.Value = value;
	}

	/// <summary>
	/// Lower RSI bound for shorts.
	/// </summary>
	public decimal RsiLowerShort
	{
		get => _rsiLowerShort.Value;
		set => _rsiLowerShort.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Take profit as a fraction of the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss as a fraction of the entry price.
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
	/// Constructor.
	/// </summary>
	public ManadiBuySellStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_rsiUpperLong = Param(nameof(RsiUpperLong), 70m)
			.SetDisplay("RSI Upper Long", "Upper RSI bound for longs", "RSI");

		_rsiLowerLong = Param(nameof(RsiLowerLong), 40m)
			.SetDisplay("RSI Lower Long", "Lower RSI bound for longs", "RSI");

		_rsiUpperShort = Param(nameof(RsiUpperShort), 60m)
			.SetDisplay("RSI Upper Short", "Upper RSI bound for shorts", "RSI");

		_rsiLowerShort = Param(nameof(RsiLowerShort), 30m)
			.SetDisplay("RSI Lower Short", "Lower RSI bound for shorts", "RSI");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA length", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA length", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line length", "MACD");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.03m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit as a fraction of the entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.015m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss as a fraction of the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, rsi, macd, ProcessCandle)
			.Start();

		// The parameters are fractions, so 0.03 is a 3% distance.
		StartProtection(
			new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			new Unit(StopLossPercent * 100m, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !rsiValue.IsFormed || !macdValue.IsFormed)
			return;

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue { Macd: decimal macdLine, Signal: decimal signalLine })
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.GetValue<decimal>();

		var longSignal = pf <= ps && fast > slow && macdLine > signalLine && rsi > RsiLowerLong && rsi < RsiUpperLong;
		var shortSignal = pf >= ps && fast < slow && macdLine < signalLine && rsi > RsiLowerShort && rsi < RsiUpperShort;

		if (longSignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortSignal && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
