using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TradePanelAutopilotStrategy : Strategy
{
	private readonly StrategyParam<bool> _autopilot;
	private readonly StrategyParam<decimal> _openThreshold;
	private readonly StrategyParam<decimal> _closeThreshold;
	private readonly StrategyParam<decimal> _lotFix;
	private readonly StrategyParam<decimal> _lotPercent;
	private readonly StrategyParam<bool> _useFixedLot;
	private readonly StrategyParam<bool> _useStopLoss;

	private readonly TimeSpan[] _timeframes = new[]
	{
		TimeSpan.FromMinutes(1),
		TimeSpan.FromMinutes(5),
		TimeSpan.FromMinutes(15),
		TimeSpan.FromMinutes(30),
		TimeSpan.FromHours(1),
		TimeSpan.FromHours(4),
		TimeSpan.FromDays(1),
		TimeSpan.FromDays(7)
	};

	private readonly CandleState[] _states;

	private sealed class CandleState
	{
		public ICandleMessage Prev;
		public ICandleMessage Curr;
		public int Buy;
		public int Sell;
	}

	public TradePanelAutopilotStrategy()
	{
		_states = new CandleState[_timeframes.Length];
		for (var i = 0; i < _states.Length; i++)
			_states[i] = new CandleState();

		_autopilot = Param(nameof(Autopilot), false).SetDisplay("Autopilot").SetCanOptimize(false);
		_openThreshold = Param(nameof(OpenThreshold), 85m).SetDisplay("Open Threshold").SetCanOptimize(true);
		_closeThreshold = Param(nameof(CloseThreshold), 55m).SetDisplay("Close Threshold").SetCanOptimize(true);
		_lotFix = Param(nameof(LotFixed), 0.01m).SetDisplay("Fixed Lot").SetCanOptimize(true);
		_lotPercent = Param(nameof(LotPercent), 0.01m).SetDisplay("Lot Percent").SetCanOptimize(true);
		_useFixedLot = Param(nameof(UseFixedLot), false).SetDisplay("Use Fixed Lot").SetCanOptimize(false);
		_useStopLoss = Param(nameof(UseStopLoss), false).SetDisplay("Use Stop Loss").SetCanOptimize(false);
	}

	public bool Autopilot { get => _autopilot.Value; set => _autopilot.Value = value; }
	public decimal OpenThreshold { get => _openThreshold.Value; set => _openThreshold.Value = value; }
	public decimal CloseThreshold { get => _closeThreshold.Value; set => _closeThreshold.Value = value; }
	public decimal LotFixed { get => _lotFix.Value; set => _lotFix.Value = value; }
	public decimal LotPercent { get => _lotPercent.Value; set => _lotPercent.Value = value; }
	public bool UseFixedLot { get => _useFixedLot.Value; set => _useFixedLot.Value = value; }
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseStopLoss)
			StartProtection();

		for (var i = 0; i < _timeframes.Length; i++)
		{
			var tf = _timeframes[i];
			var index = i;
			var subscription = SubscribeCandles(tf);
			subscription
				.Bind(c => ProcessCandle(index, c))
				.Start();
		}
	}

	private void ProcessCandle(int index, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var state = _states[index];
		state.Prev = state.Curr;
		state.Curr = candle;

		if (state.Prev is null)
			return;

		(state.Buy, state.Sell) = CalculateSignal(state.Prev, state.Curr);
		UpdateSignals();
	}

	private static (int buy, int sell) CalculateSignal(ICandleMessage prev, ICandleMessage curr)
	{
		var buy = 0;
		var sell = 0;

		if (curr.OpenPrice > prev.OpenPrice) buy++; else sell++;
		if (curr.HighPrice > prev.HighPrice) buy++; else sell++;
		if (curr.LowPrice > prev.LowPrice) buy++; else sell++;

		var currHl2 = (curr.HighPrice + curr.LowPrice) / 2m;
		var prevHl2 = (prev.HighPrice + prev.LowPrice) / 2m;
		if (currHl2 > prevHl2) buy++; else sell++;

		if (curr.ClosePrice > prev.ClosePrice) buy++; else sell++;

		var currHlc3 = (curr.HighPrice + curr.LowPrice + curr.ClosePrice) / 3m;
		var prevHlc3 = (prev.HighPrice + prev.LowPrice + prev.ClosePrice) / 3m;
		if (currHlc3 > prevHlc3) buy++; else sell++;

		var currHlcc4 = (curr.HighPrice + curr.LowPrice + curr.ClosePrice + curr.ClosePrice) / 4m;
		var prevHlcc4 = (prev.HighPrice + prev.LowPrice + prev.ClosePrice + prev.ClosePrice) / 4m;
		if (currHlcc4 > prevHlcc4) buy++; else sell++;

		return (buy, sell);
	}

	private void UpdateSignals()
	{
		var buy = 0;
		var sell = 0;

		foreach (var s in _states)
		{
			buy += s.Buy;
			sell += s.Sell;
		}

		if (buy + sell == 0)
			return;

	var buyPercent = (decimal)buy / (buy + sell) * 100m;
	var sellPercent = 100m - buyPercent;

		if (buy + sell == 0)
			return;

		var buyPercent = (decimal)buy / (buy + sell) * 100m;
		var sellPercent = 100m - buyPercent;

		if (!Autopilot)
			return;

		if (Position > 0 && buyPercent < CloseThreshold)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && sellPercent < CloseThreshold)
		{
			BuyMarket(-Position);
			return;
		}

		if (Position == 0)
		{
			var volume = GetVolume();

			if (buyPercent > OpenThreshold)
			{
				BuyMarket(volume);
			}
			else if (sellPercent > OpenThreshold)
			{
				SellMarket(volume);
			}
		}
	}

	private decimal GetVolume()
	{
		if (UseFixedLot)
			return LotFixed;

		var portfolioValue = Portfolio?.CurrentValue ?? 0m;
		var volume = portfolioValue * LotPercent / 100m;
		return volume > 0 ? volume : LotFixed;
	}
}
