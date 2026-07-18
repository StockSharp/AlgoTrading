using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Histogram Reversal strategy.
/// Enters long when MACD histogram (MACD - Signal) crosses above zero.
/// Enters short when MACD histogram crosses below zero.
/// Uses cooldown to control trade frequency.
/// </summary>
public class MacdHistogramReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevHistogram;
	private int _cooldown;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MacdHistogramReversalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetRange(8, 16)
			.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetRange(20, 30)
			.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetRange(7, 13)
			.SetDisplay("Signal Period", "Signal line period", "MACD");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevHistogram = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHistogram = null;
		_cooldown = 0;

		var macdHist = new MovingAverageConvergenceDivergenceHistogram
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macdHist, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macdHist);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdIv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdIv.IsFormed)
			return;

		var mv = (IMacdHistogramValue)macdIv;

		if (mv.Macd is not decimal macdVal || mv.Signal is not decimal signalVal)
			return;

		var histogram = macdVal - signalVal;

		if (_prevHistogram == null)
		{
			_prevHistogram = histogram;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevHistogram = histogram;
			return;
		}

		var crossedAboveZero = _prevHistogram < 0 && histogram >= 0;
		var crossedBelowZero = _prevHistogram > 0 && histogram <= 0;

		if (Position == 0 && crossedAboveZero)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && crossedBelowZero)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && crossedBelowZero)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && crossedAboveZero)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevHistogram = histogram;
	}
}
