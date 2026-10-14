using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual Supertrend MACD strategy.
/// The MACD line is the fast minus the slow moving average of the close (OscillatorMaType) and the histogram is the MACD line minus
/// its signal average (SignalMaType). Long: close above both Supertrend lines and a positive histogram. Short: close below both lines
/// and a negative histogram. A long closes when the close falls below either line or the histogram turns negative, a short when the
/// close rises above either line or the histogram turns positive. TradeDirection limits the sides that may be opened.
/// </summary>
public class DualSupertrendMacdStrategy : Strategy
{
	/// <summary>
	/// Moving average types of the MACD.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,
	}

	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<MaTypes> _oscillatorMaType;
	private readonly StrategyParam<MaTypes> _signalMaType;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _signalMa;

	/// <summary>
	/// Fast MACD period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow MACD period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal MACD period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Moving average type of the MACD line.
	/// </summary>
	public MaTypes OscillatorMaType
	{
		get => _oscillatorMaType.Value;
		set => _oscillatorMaType.Value = value;
	}

	/// <summary>
	/// Moving average type of the signal line.
	/// </summary>
	public MaTypes SignalMaType
	{
		get => _signalMaType.Value;
		set => _signalMaType.Value = value;
	}

	/// <summary>
	/// ATR period of the first Supertrend.
	/// </summary>
	public int AtrPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	/// <summary>
	/// Factor of the first Supertrend.
	/// </summary>
	public decimal Factor1
	{
		get => _factor1.Value;
		set => _factor1.Value = value;
	}

	/// <summary>
	/// ATR period of the second Supertrend.
	/// </summary>
	public int AtrPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	/// <summary>
	/// Factor of the second Supertrend.
	/// </summary>
	public decimal Factor2
	{
		get => _factor2.Value;
		set => _factor2.Value = value;
	}

	/// <summary>
	/// Sides that may be opened: Long, Short or Both.
	/// </summary>
	public string TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
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
	public DualSupertrendMacdStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast MACD period", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow MACD period", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal MACD period", "MACD");

		_oscillatorMaType = Param(nameof(OscillatorMaType), MaTypes.Exponential)
			.SetDisplay("Oscillator MA Type", "Moving average type of the MACD line", "MACD");

		_signalMaType = Param(nameof(SignalMaType), MaTypes.Exponential)
			.SetDisplay("Signal MA Type", "Moving average type of the signal line", "MACD");

		_atrPeriod1 = Param(nameof(AtrPeriod1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period 1", "ATR period of the first Supertrend", "Supertrend");

		_factor1 = Param(nameof(Factor1), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Factor 1", "Factor of the first Supertrend", "Supertrend");

		_atrPeriod2 = Param(nameof(AtrPeriod2), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period 2", "ATR period of the second Supertrend", "Supertrend");

		_factor2 = Param(nameof(Factor2), 5.0m)
			.SetGreaterThanZero()
			.SetDisplay("Factor 2", "Factor of the second Supertrend", "Supertrend");

		_tradeDirection = Param(nameof(TradeDirection), "Both")
			.SetDisplay("Trade Direction", "Sides that may be opened: Long, Short or Both", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private static IIndicator CreateMa(MaTypes type, int length)
	{
		return type == MaTypes.Simple
			? new SimpleMovingAverage { Length = length }
			: new ExponentialMovingAverage { Length = length };
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = CreateMa(OscillatorMaType, MacdFast);
		var slowMa = CreateMa(OscillatorMaType, MacdSlow);
		_signalMa = CreateMa(SignalMaType, MacdSignal);
		var superTrend1 = new SuperTrend { Length = AtrPeriod1, Multiplier = Factor1 };
		var superTrend2 = new SuperTrend { Length = AtrPeriod2, Multiplier = Factor2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, superTrend1, superTrend2, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend1);
			DrawIndicator(area, superTrend2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue st1Value, IIndicatorValue st2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed)
			return;

		var macd = fastValue.GetValue<decimal>() - slowValue.GetValue<decimal>();
		var signalValue = _signalMa.Process(new DecimalIndicatorValue(_signalMa, macd, candle.OpenTime) { IsFinal = true });

		if (!signalValue.IsFormed || !st1Value.IsFormed || !st2Value.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var histogram = macd - signalValue.GetValue<decimal>();
		var st1 = st1Value.GetValue<decimal>();
		var st2 = st2Value.GetValue<decimal>();
		var close = candle.ClosePrice;

		var direction = TradeDirection;
		var allowLong = direction.EqualsIgnoreCase("Both") || direction.EqualsIgnoreCase("Long");
		var allowShort = direction.EqualsIgnoreCase("Both") || direction.EqualsIgnoreCase("Short");

		var longEntry = close > st1 && close > st2 && histogram > 0;
		var shortEntry = close < st1 && close < st2 && histogram < 0;
		var longExit = close < st1 || close < st2 || histogram < 0;
		var shortExit = close > st1 || close > st2 || histogram > 0;

		if (Position > 0)
		{
			if (!longExit)
				return;

			if (shortEntry && allowShort)
				SellMarket(Volume + Position);
			else
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (!shortExit)
				return;

			if (longEntry && allowLong)
				BuyMarket(Volume - Position);
			else
				BuyMarket(-Position);
		}
		else if (longEntry && allowLong)
			BuyMarket(Volume);
		else if (shortEntry && allowShort)
			SellMarket(Volume);
	}
}
