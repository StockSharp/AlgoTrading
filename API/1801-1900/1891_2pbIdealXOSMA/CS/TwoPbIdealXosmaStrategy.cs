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
/// Strategy that uses MACD histogram slope to generate entry and exit signals.
/// Buys when histogram turns up, sells when it turns down.
/// </summary>
public class TwoPbIdealXosmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHist;
	private int _barsSinceTrade;

	/// <summary>Fast MA period for MACD calculation.</summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	/// <summary>Slow MA period for MACD calculation.</summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	/// <summary>Signal line period for MACD.</summary>
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	/// <summary>Minimum number of bars between entries.</summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	/// <summary>Candle type used for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TwoPbIdealXosmaStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicator")
			.SetOptimize(5, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicator")
			.SetOptimize(20, 60, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal", "Signal line period", "Indicator")
			.SetOptimize(5, 20, 1);

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_prevHist = null;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = FastPeriod;
		macd.Macd.LongMa.Length = SlowPeriod;
		macd.SignalMa.Length = SignalPeriod;

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (macdValue.Macd is not decimal macdLine || macdValue.Signal is not decimal signal)
			return;

		var histogram = macdLine - signal;
		_barsSinceTrade++;

		if (_prevHist is not null)
		{
			var buySignal = _prevHist <= 0m && histogram > 0m && macdLine > 0m;
			var sellSignal = _prevHist >= 0m && histogram < 0m && macdLine < 0m;

			if (buySignal && Position <= 0 && _barsSinceTrade >= CooldownBars)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_barsSinceTrade = 0;
			}
			else if (sellSignal && Position >= 0 && _barsSinceTrade >= CooldownBars)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_barsSinceTrade = 0;
			}
		}

		_prevHist = histogram;
	}
}
