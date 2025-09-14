
using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe strategy based on MACD histogram, moving averages, Williams %R and RSI.
/// </summary>
public class SmartAssTradeV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStopStep;

	private readonly Dictionary<int, TimeframeState> _states = new();
	private decimal? _prevWpr;
	private decimal? _currWpr;
	private decimal? _prevRsi;
	private decimal? _currRsi;

	private class TimeframeState
	{
		public decimal? PrevMacd;
		public decimal? CurrMacd;
		public decimal? PrevMa;
		public decimal? CurrMa;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Trailing step in price units.
	/// </summary>
	public decimal TrailingStopStep { get => _trailingStopStep.Value; set => _trailingStopStep.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="SmartAssTradeV2Strategy"/>.
	/// </summary>
	public SmartAssTradeV2Strategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume", "General");

		_takeProfit = Param(nameof(TakeProfit), 35m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk Management");

		_stopLoss = Param(nameof(StopLoss), 62m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk Management");

		_trailingStop = Param(nameof(TrailingStop), 30m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk Management");

		_trailingStopStep = Param(nameof(TrailingStopStep), 1m)
			.SetDisplay("Trailing Step", "Trailing stop step", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var tf in new[] { 1, 5, 15, 30, 60 })
			yield return (Security, TimeSpan.FromMinutes(tf).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		foreach (var tf in new[] { 1, 5, 15, 30, 60 })
		{
			var macd = new MovingAverageConvergenceDivergence
			{
				ShortLength = 12,
				LongLength = 26,
				SignalLength = 9
			};

			var ma = new SimpleMovingAverage
			{
				Length = 20
			};

			_states[tf] = new TimeframeState();

			var subscription = SubscribeCandles(TimeSpan.FromMinutes(tf).TimeFrame());

			if (tf == 30)
			{
				var wpr = new WilliamsR
				{
					Length = 26
				};

				var rsi = new RelativeStrengthIndex
				{
					Length = 14
				};

				subscription
				.Bind(macd, ma, wpr, rsi, (candle, macdVal, maVal, wprVal, rsiVal) => Process30(candle, tf, macdVal, maVal, wprVal, rsiVal))
				.Start();
			}
			else
			{
				subscription
				.Bind(macd, ma, (candle, macdVal, maVal) => ProcessTf(candle, tf, macdVal, maVal))
				.Start();
			}
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price),
			isTrailingStop: UseTrailingStop,
			trailingStop: new Unit(TrailingStop, UnitTypes.Price),
			trailingStopStep: new Unit(TrailingStopStep, UnitTypes.Price));
	}

	private void ProcessTf(ICandleMessage candle, int tf, decimal macdVal, decimal maVal)
	{
		var state = _states[tf];
		state.PrevMacd = state.CurrMacd;
		state.CurrMacd = macdVal;
		state.PrevMa = state.CurrMa;
		state.CurrMa = maVal;
	}

	private void Process30(ICandleMessage candle, int tf, decimal macdVal, decimal maVal, decimal wprVal, decimal rsiVal)
	{
		ProcessTf(candle, tf, macdVal, maVal);

		_prevWpr = _currWpr;
		_currWpr = wprVal;
		_prevRsi = _currRsi;
		_currRsi = rsiVal;

		if (candle.State != CandleStates.Finished)
			return;

		TryTrade(candle);
	}

	private void TryTrade(ICandleMessage candle)
	{
		int osb = 0, oss = 0, upm = 0, dnm = 0;

		foreach (var st in _states.Values)
		{
			if (st.PrevMacd != null && st.CurrMacd != null)
			{
				if (st.CurrMacd > st.PrevMacd)
					osb++;
				else if (st.CurrMacd < st.PrevMacd)
					oss++;
			}

			if (st.PrevMa != null && st.CurrMa != null)
			{
				if (st.CurrMa > st.PrevMa)
					upm++;
				else if (st.CurrMa < st.PrevMa)
					dnm++;
			}
		}

		bool upward = osb >= 4 && upm >= 4;
		bool downward = oss >= 4 && dnm >= 4;
		int codB = osb == 5 && upm == 5 ? 2 : upward ? 1 : 0;
		int codS = oss == 5 && dnm == 5 ? 2 : downward ? 1 : 0;

		if (_prevWpr == null || _currWpr == null || _prevRsi == null || _currRsi == null)
			return;

		bool wprmb = _currWpr < 90m && _currWpr > _prevWpr && _currRsi < 77m && _currRsi > _prevRsi;
		bool wprms = _currWpr > 10m && _currWpr < _prevWpr && _currRsi > 23m && _currRsi < _prevRsi;

		if (Position == 0)
		{
			if (upward && codB != 0 && wprmb)
				BuyMarket(Volume);
			else if (downward && codS != 0 && wprms)
				SellMarket(Volume);
		}
	}
}
