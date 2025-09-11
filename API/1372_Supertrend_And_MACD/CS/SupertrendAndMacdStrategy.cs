using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend and MACD strategy.
/// </summary>
public class SupertrendAndMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _stopLookback;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevDiff;

	/// <summary>
	/// ATR period for Supertrend.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Multiplier for Supertrend.
	/// </summary>
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// Lookback period for stop calculation.
	/// </summary>
	public int StopLookback { get => _stopLookback.Value; set => _stopLookback.Value = value; }

	/// <summary>
	/// MACD fast MA length.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow MA length.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SupertrendAndMacdStrategy"/>.
	/// </summary>
	public SupertrendAndMacdStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
		.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")
		.SetCanOptimize(true);

		_factor = Param(nameof(Factor), 3m)
		.SetDisplay("Factor", "Multiplier for Supertrend", "Supertrend")
		.SetCanOptimize(true);

		_emaPeriod = Param(nameof(EmaPeriod), 200)
		.SetDisplay("EMA Period", "Period for EMA filter", "EMA")
		.SetCanOptimize(true);

		_stopLookback = Param(nameof(StopLookback), 10)
		.SetDisplay("Stop Lookback", "Bars for stop calculation", "Risk")
		.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
		.SetDisplay("Fast Length", "MACD fast MA length", "MACD")
		.SetCanOptimize(true);

		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetDisplay("Slow Length", "MACD slow MA length", "MACD")
		.SetCanOptimize(true);

		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetDisplay("Signal Length", "MACD signal length", "MACD")
		.SetCanOptimize(true);

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
		_prevDiff = 0m;
		_highest = null;
		_lowest = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = MacdFast },
				LongMa = new ExponentialMovingAverage { Length = MacdSlow }
			},
			SignalMa = new ExponentialMovingAverage { Length = MacdSignal }
		};

		_highest = new Highest { Length = StopLookback };
		_lowest = new Lowest { Length = StopLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(supertrend, ema, macd, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawIndicator(area, ema);
			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue emaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var st = stValue.ToDecimal();
		var ema = emaValue.ToDecimal();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;

		var diff = macd - signal;

		var highest = _highest.Process(candle).ToDecimal();
		var lowest = _lowest.Process(candle).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevDiff = diff;
		return;
		}

		var longCondition = candle.ClosePrice > st && macd > signal && candle.ClosePrice > ema;
		var shortCondition = candle.ClosePrice < st && macd < signal && candle.ClosePrice < ema;

		if (Position == 0)
		{
			if (longCondition)
			BuyMarket();
			else if (shortCondition)
			SellMarket();
		}
		else if (Position > 0)
		{
			if ((_prevDiff <= 0m && diff > 0m) || candle.LowPrice <= lowest - 1m)
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			if ((_prevDiff >= 0m && diff < 0m) || candle.HighPrice >= highest + 1m)
			BuyMarket(Math.Abs(Position));
		}

		_prevDiff = diff;
	}
}
