using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD signal strategy with ATR-based threshold and trailing stop.
/// Opens positions when MACD crosses the signal line beyond an ATR-adjusted level.
/// </summary>
public class MacdSignalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _trailingStopTicks;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _atrLevel;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private AverageTrueRange _atr;
	private decimal _prevDelta;
	private bool _hasPrevDelta;

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Trailing stop in ticks.
	/// </summary>
	public decimal TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for MACD threshold.
	/// </summary>
	public decimal AtrLevel
	{
		get => _atrLevel.Value;
		set => _atrLevel.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MacdSignalStrategy"/>.
	/// </summary>
	public MacdSignalStrategy()
	{
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 10m)
			.SetDisplay("Take Profit", "Take profit in ticks", "Risk Management");

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 25m)
			.SetDisplay("Trailing Stop", "Trailing stop in ticks", "Risk Management");

		_fastPeriod = Param(nameof(FastPeriod), 9)
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 15)
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators");

		_signalPeriod = Param(nameof(SignalPeriod), 8)
			.SetDisplay("Signal", "Signal line period for MACD", "Indicators");

		_atrLevel = Param(nameof(AtrLevel), 0.004m)
			.SetDisplay("ATR Level", "ATR multiplier for threshold", "Logic");

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

		_macd = null;
		_atr = null;
		_prevDelta = 0m;
		_hasPrevDelta = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		_atr = new() { Length = 200 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Absolute),
			stopLoss: TrailingStopTicks > 0 ? new Unit(TrailingStopTicks * step, UnitTypes.Absolute) : null,
			isStopTrailing: TrailingStopTicks > 0);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr.Process(candle);
		if (!atrValue.IsFinal)
		{
			var tmp = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			_prevDelta = tmp.Macd is decimal m && tmp.Signal is decimal s ? m - s : 0m;
			_hasPrevDelta = true;
			return;
		}

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var delta = macd - signal;
		var rr = atrValue.ToDecimal() * AtrLevel;

		if (!_hasPrevDelta)
		{
			_prevDelta = delta;
			_hasPrevDelta = true;
			return;
		}

		var prevDelta = _prevDelta;
		_prevDelta = delta;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && delta > rr && prevDelta <= rr)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && delta < -rr && prevDelta >= -rr)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0 && delta < 0)
		{
			ClosePosition();
		}
		else if (Position < 0 && delta > 0)
		{
			ClosePosition();
		}
	}
}
