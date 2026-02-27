using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on MACD signal line crossing the zero level.
/// </summary>
public class MacdWaterlineCrossExpectatorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _rrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private bool _shouldBuy;
	private bool _hasPrev;
	private decimal _prevSignal;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
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
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier.
	/// </summary>
	public decimal RRMultiplier
	{
		get => _rrMultiplier.Value;
		set => _rrMultiplier.Value = value;
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
	/// Initializes <see cref="MacdWaterlineCrossExpectatorStrategy"/>.
	/// </summary>
	public MacdWaterlineCrossExpectatorStrategy()
	{
		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("Signal", "Signal line period", "Indicators");
		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetGreaterThanZero();
		_rrMultiplier = Param(nameof(RRMultiplier), 2m)
			.SetDisplay("RR Multiplier", "Risk reward multiplier", "Risk")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle", "Candle time frame", "General");
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
		_shouldBuy = true;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			takeProfit: new Unit(StopLossPct * RRMultiplier, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent)
		);

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !macdValue.IsFormed)
			return;

		var typed = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		var signal = typed.Signal;

		if (signal == null)
			return;

		var signalVal = signal.Value;

		if (!_hasPrev)
		{
			_prevSignal = signalVal;
			_hasPrev = true;
			return;
		}

		var crossedAbove = _prevSignal < 0 && signalVal > 0;
		var crossedBelow = _prevSignal > 0 && signalVal < 0;

		if (crossedAbove && _shouldBuy)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_shouldBuy = false;
		}
		else if (crossedBelow && !_shouldBuy)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_shouldBuy = true;
		}

		_prevSignal = signalVal;
	}
}
