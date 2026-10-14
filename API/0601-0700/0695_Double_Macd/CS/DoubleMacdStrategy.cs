using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double MACD strategy.
/// Two MACDs of different speeds are built from moving averages of the selected types: each MACD line is the fast average of the
/// close minus the slow one, and its signal line is an average of the same type over the MACD line. The strategy goes long when both
/// MACD lines are above their signal lines and short when both are below, reversing an opposite position. A percent stop loss limits
/// the loss.
/// </summary>
public class DoubleMacdStrategy : Strategy
{
	/// <summary>
	/// Moving average types of the MACDs.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Ema,

		/// <summary>
		/// Simple moving average.
		/// </summary>
		Sma,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Wma,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smma,
	}

	private readonly StrategyParam<int> _fastLength1;
	private readonly StrategyParam<int> _slowLength1;
	private readonly StrategyParam<int> _signalLength1;
	private readonly StrategyParam<MaTypes> _maType1;
	private readonly StrategyParam<int> _fastLength2;
	private readonly StrategyParam<int> _slowLength2;
	private readonly StrategyParam<int> _signalLength2;
	private readonly StrategyParam<MaTypes> _maType2;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _signal1;
	private IIndicator _signal2;

	/// <summary>
	/// Fast period of the first MACD.
	/// </summary>
	public int FastLength1
	{
		get => _fastLength1.Value;
		set => _fastLength1.Value = value;
	}

	/// <summary>
	/// Slow period of the first MACD.
	/// </summary>
	public int SlowLength1
	{
		get => _slowLength1.Value;
		set => _slowLength1.Value = value;
	}

	/// <summary>
	/// Signal period of the first MACD.
	/// </summary>
	public int SignalLength1
	{
		get => _signalLength1.Value;
		set => _signalLength1.Value = value;
	}

	/// <summary>
	/// Moving average type of the first MACD.
	/// </summary>
	public MaTypes MaType1
	{
		get => _maType1.Value;
		set => _maType1.Value = value;
	}

	/// <summary>
	/// Fast period of the second MACD.
	/// </summary>
	public int FastLength2
	{
		get => _fastLength2.Value;
		set => _fastLength2.Value = value;
	}

	/// <summary>
	/// Slow period of the second MACD.
	/// </summary>
	public int SlowLength2
	{
		get => _slowLength2.Value;
		set => _slowLength2.Value = value;
	}

	/// <summary>
	/// Signal period of the second MACD.
	/// </summary>
	public int SignalLength2
	{
		get => _signalLength2.Value;
		set => _signalLength2.Value = value;
	}

	/// <summary>
	/// Moving average type of the second MACD.
	/// </summary>
	public MaTypes MaType2
	{
		get => _maType2.Value;
		set => _maType2.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
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
	public DoubleMacdStrategy()
	{
		_fastLength1 = Param(nameof(FastLength1), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length 1", "Fast period of the first MACD", "MACD 1");

		_slowLength1 = Param(nameof(SlowLength1), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length 1", "Slow period of the first MACD", "MACD 1");

		_signalLength1 = Param(nameof(SignalLength1), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length 1", "Signal period of the first MACD", "MACD 1");

		_maType1 = Param(nameof(MaType1), MaTypes.Ema)
			.SetDisplay("MA Type 1", "Moving average type of the first MACD", "MACD 1");

		_fastLength2 = Param(nameof(FastLength2), 24)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length 2", "Fast period of the second MACD", "MACD 2");

		_slowLength2 = Param(nameof(SlowLength2), 52)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length 2", "Slow period of the second MACD", "MACD 2");

		_signalLength2 = Param(nameof(SignalLength2), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length 2", "Signal period of the second MACD", "MACD 2");

		_maType2 = Param(nameof(MaType2), MaTypes.Ema)
			.SetDisplay("MA Type 2", "Moving average type of the second MACD", "MACD 2");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private static IIndicator CreateMa(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.Sma => new SimpleMovingAverage { Length = length },
			MaTypes.Wma => new WeightedMovingAverage { Length = length },
			MaTypes.Smma => new SmoothedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast1 = CreateMa(MaType1, FastLength1);
		var slow1 = CreateMa(MaType1, SlowLength1);
		var fast2 = CreateMa(MaType2, FastLength2);
		var slow2 = CreateMa(MaType2, SlowLength2);
		_signal1 = CreateMa(MaType1, SignalLength1);
		_signal2 = CreateMa(MaType2, SignalLength2);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast1, slow1, fast2, slow2, ProcessCandle)
			.Start();

		StartProtection(new Unit(), StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fast1Value, IIndicatorValue slow1Value, IIndicatorValue fast2Value, IIndicatorValue slow2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fast1Value.IsFormed || !slow1Value.IsFormed || !fast2Value.IsFormed || !slow2Value.IsFormed)
			return;

		var macd1 = fast1Value.GetValue<decimal>() - slow1Value.GetValue<decimal>();
		var macd2 = fast2Value.GetValue<decimal>() - slow2Value.GetValue<decimal>();

		var signal1Value = _signal1.Process(new DecimalIndicatorValue(_signal1, macd1, candle.OpenTime) { IsFinal = true });
		var signal2Value = _signal2.Process(new DecimalIndicatorValue(_signal2, macd2, candle.OpenTime) { IsFinal = true });

		if (!signal1Value.IsFormed || !signal2Value.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var signal1 = signal1Value.GetValue<decimal>();
		var signal2 = signal2Value.GetValue<decimal>();

		var bullish = macd1 > signal1 && macd2 > signal2;
		var bearish = macd1 < signal1 && macd2 < signal2;

		if (bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
