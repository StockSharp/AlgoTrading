using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend combined with MACD crossover. Goes long when price is above Supertrend line and MACD crosses above its signal. Exits the long position on opposite crossover while price is below Supertrend.
/// </summary>
public class SupertrendMacdCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevMacdAbove;

	/// <summary>
	/// ATR period for Supertrend.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="SupertrendMacdCrossoverStrategy"/>.
	/// </summary>
	public SupertrendMacdCrossoverStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend");

		_factor = Param(nameof(Factor), 3m)
			.SetDisplay("Factor", "ATR multiplier for Supertrend", "Supertrend");

		_fastLength = Param(nameof(FastLength), 12)
			.SetDisplay("Fast Length", "Fast EMA for MACD", "MACD");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Slow EMA for MACD", "MACD");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("Signal Length", "Signal line for MACD", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevMacdAbove = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(st, macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, st);
			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var st = stValue.ToDecimal();
		var directionUp = candle.ClosePrice > st;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var macdAbove = macd > signal;
		var crossUp = macdAbove && !_prevMacdAbove;
		var crossDown = !macdAbove && _prevMacdAbove;

		if (directionUp && crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (!directionUp && crossDown && Position > 0)
			SellMarket(Position);

		_prevMacdAbove = macdAbove;
	}
}
