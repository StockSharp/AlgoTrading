using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading using the Schaff Trend Cycle indicator.
/// Opens long positions when the cycle rises above the high level
/// and short positions when it falls below the low level.
/// </summary>
public class ColorSchaffTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _cycle;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;
	private StochasticOscillator _stoch1;
	private StochasticOscillator _stoch2;
	private decimal? _prevStc;

	/// <summary>
	/// Fast EMA period for MACD calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Length of stochastic cycles.
	/// </summary>
	public int Cycle
	{
		get => _cycle.Value;
		set => _cycle.Value = value;
	}

	/// <summary>
	/// Upper threshold for the Schaff Trend Cycle.
	/// </summary>
	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for the Schaff Trend Cycle.
	/// </summary>
	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorSchaffTrendCycleStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 23)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 50)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicator");

		_cycle = Param(nameof(Cycle), 10)
			.SetDisplay("Cycle", "Stochastic cycle length", "Indicator");

		_highLevel = Param(nameof(HighLevel), 60m)
			.SetDisplay("High Level", "Overbought level", "Indicator");

		_lowLevel = Param(nameof(LowLevel), -60m)
			.SetDisplay("Low Level", "Oversold level", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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

		_macd?.Reset();
		_stoch1?.Reset();
		_stoch2?.Reset();
		_prevStc = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = FastPeriod,
			Slow = SlowPeriod,
			Signal = 1
		};

		_stoch1 = new StochasticOscillator
		{
			Length = Cycle,
			K = { Length = 1 },
			D = { Length = 1 }
		};

		_stoch2 = new StochasticOscillator
		{
			Length = Cycle,
			K = { Length = 1 },
			D = { Length = 1 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var macdVal = (MovingAverageConvergenceDivergenceValue)_macd.Process(candle.ClosePrice, candle.OpenTime, true);
		if (macdVal.Macd is not decimal macd)
			return;

		var st1Val = (StochasticOscillatorValue)_stoch1.Process(macd, candle.OpenTime, true);
		if (st1Val.K is not decimal st1)
			return;

		var st2Val = (StochasticOscillatorValue)_stoch2.Process(st1, candle.OpenTime, true);
		if (st2Val.K is not decimal k)
			return;

		var stc = k * 2m - 100m;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevStc = stc;
			return;
		}

		if (_prevStc is decimal prev)
		{
			if (prev <= HighLevel && stc > HighLevel && stc > prev)
			{
				if (Position < 0)
					ClosePosition();
				if (Position <= 0)
					BuyMarket();
			}
			else if (prev >= LowLevel && stc < LowLevel && stc < prev)
			{
				if (Position > 0)
					ClosePosition();
				if (Position >= 0)
					SellMarket();
			}
		}

		_prevStc = stc;
	}
}
