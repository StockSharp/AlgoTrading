using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover with MACD signal trend confirmation.
/// </summary>
public class Emagic1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSignal;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Enable stop loss protection.
	/// </summary>
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing stop in price units.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public Emagic1Strategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length for fast EMA", "Indicators")
			
			.SetOptimize(5, 15, 1);

		_slowEmaLength = Param(nameof(SlowEmaLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length for slow EMA", "Indicators")
			
			.SetOptimize(10, 20, 1);

		_macdFast = Param(nameof(MacdFast), 10)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast period for MACD", "Indicators")
			
			.SetOptimize(5, 15, 1);

		_macdSlow = Param(nameof(MacdSlow), 32)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow period for MACD", "Indicators")
			
			.SetOptimize(20, 40, 1);

		_macdSignal = Param(nameof(MacdSignal), 4)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal period for MACD", "Indicators")
			
			.SetOptimize(3, 10, 1);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss protection", "Risk");
		_stopLoss = Param(nameof(StopLoss), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 11m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop in price units", "Risk");

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
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new EMA { Length = FastEmaLength };
		// Original strategy used open price for slow EMA; close price is used here for simplicity.
		var slowEma = new EMA { Length = SlowEmaLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { macd, fastEma, slowEma }, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: UseStopLoss ? new Unit(StopLoss, UnitTypes.Absolute) : null,
			takeProfit: UseTakeProfit ? new Unit(TakeProfit, UnitTypes.Absolute) : null,
			isStopTrailing: UseTrailingStop);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, macd);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[0];
		var signal = macdTyped.Signal;
		var fastEma = values[1].IsEmpty ? (decimal?)null : values[1].GetValue<decimal>();
		var slowEma = values[2].IsEmpty ? (decimal?)null : values[2].GetValue<decimal>();

		if (fastEma is null || slowEma is null || signal is null)
			return;

		var signalVal = signal.Value;
		var longSignal = fastEma.Value > slowEma.Value && _prevSignal is decimal prev && prev < signalVal;
		var shortSignal = fastEma.Value < slowEma.Value && _prevSignal is decimal prev2 && signalVal < prev2;

		if (longSignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevSignal = signal;
	}
}
