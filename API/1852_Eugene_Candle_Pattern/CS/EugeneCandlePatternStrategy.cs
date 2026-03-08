using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle pattern strategy with breakout confirmation.
/// </summary>
public class EugeneCandlePatternStrategy : Strategy
{
	private readonly StrategyParam<int> _sl;
	private readonly StrategyParam<int> _tp;
	private readonly StrategyParam<bool> _inv;
	private readonly StrategyParam<DataType> _cType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _minBodyPercent;

	private readonly ICandleMessage[] _recent = new ICandleMessage[4];
	private decimal _stop;
	private decimal _take;
	private int _cooldownRemaining;

	public int StopLossPoints { get => _sl.Value; set => _sl.Value = value; }
	public int TakeProfitPoints { get => _tp.Value; set => _tp.Value = value; }
	public bool InvertSignals { get => _inv.Value; set => _inv.Value = value; }
	public DataType CandleType { get => _cType.Value; set => _cType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal MinBodyPercent { get => _minBodyPercent.Value; set => _minBodyPercent.Value = value; }

	public EugeneCandlePatternStrategy()
	{
		_sl = Param(nameof(StopLossPoints), 500).SetDisplay("Stop Loss (points)", "Stop loss in price steps, 0 - disabled", "Risk");
		_tp = Param(nameof(TakeProfitPoints), 800).SetDisplay("Take Profit (points)", "Take profit in price steps, 0 - disabled", "Risk");
		_inv = Param(nameof(InvertSignals), false).SetDisplay("Invert Signals", "Swap buy and sell signals", "General");
		_cType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
		_cooldownBars = Param(nameof(CooldownBars), 4).SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
		_minBodyPercent = Param(nameof(MinBodyPercent), 0.0015m).SetDisplay("Minimum Body %", "Minimum candle body size relative to close price", "Filters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		Array.Clear(_recent, 0, _recent.Length);
		_stop = 0m;
		_take = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		CheckStops(candle);
		_recent[3] = _recent[2];
		_recent[2] = _recent[1];
		_recent[1] = _recent[0];
		_recent[0] = candle;

		if (_recent[3] is null)
			return;

		ComputeSignals(out var openBuy, out var openSell, out var closeBuy, out var closeSell);
		if (InvertSignals)
		{
			(openBuy, openSell) = (openSell, openBuy);
			(closeBuy, closeSell) = (closeSell, closeBuy);
		}

		if (Position > 0 && closeBuy)
			ClosePosition();
		else if (Position < 0 && closeSell)
			ClosePosition();

		if (_cooldownRemaining > 0)
			return;

		if (Position <= 0 && openBuy && !openSell)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			SetStops(candle.ClosePrice, true);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position >= 0 && openSell && !openBuy)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			SetStops(candle.ClosePrice, false);
			_cooldownRemaining = CooldownBars;
		}
	}

	private void ComputeSignals(out bool openBuy, out bool openSell, out bool closeBuy, out bool closeSell)
	{
		var current = _recent[0];
		var prev = _recent[1];
		var prev2 = _recent[2];
		openBuy = false;
		openSell = false;
		closeBuy = false;
		closeSell = false;

		var prevBody = Math.Abs(prev.ClosePrice - prev.OpenPrice);
		var currentBody = Math.Abs(current.ClosePrice - current.OpenPrice);
		var prevBodyPercent = prev.ClosePrice != 0m ? prevBody / prev.ClosePrice : 0m;
		var currentBodyPercent = current.ClosePrice != 0m ? currentBody / current.ClosePrice : 0m;
		var bullishSetup = prev.ClosePrice < prev.OpenPrice && prev.LowPrice > prev2.LowPrice;
		var bearishSetup = prev.ClosePrice > prev.OpenPrice && prev.HighPrice < prev2.HighPrice;
		var bullishBreakout = current.ClosePrice > prev.HighPrice && current.ClosePrice > current.OpenPrice;
		var bearishBreakout = current.ClosePrice < prev.LowPrice && current.ClosePrice < current.OpenPrice;

		openBuy = bullishSetup && bullishBreakout && prevBodyPercent >= MinBodyPercent && currentBodyPercent >= MinBodyPercent;
		openSell = bearishSetup && bearishBreakout && prevBodyPercent >= MinBodyPercent && currentBodyPercent >= MinBodyPercent;
		closeBuy = current.ClosePrice < prev.LowPrice;
		closeSell = current.ClosePrice > prev.HighPrice;
	}

	private void SetStops(decimal price, bool longPos)
	{
		var step = Security.PriceStep ?? 1m;
		if (longPos)
		{
			_stop = StopLossPoints > 0 ? price - step * StopLossPoints : 0m;
			_take = TakeProfitPoints > 0 ? price + step * TakeProfitPoints : 0m;
		}
		else
		{
			_stop = StopLossPoints > 0 ? price + step * StopLossPoints : 0m;
			_take = TakeProfitPoints > 0 ? price - step * TakeProfitPoints : 0m;
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket();
		else if (Position < 0)
			BuyMarket();

		_stop = 0m;
		_take = 0m;
		_cooldownRemaining = CooldownBars;
	}

	private void CheckStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if ((_stop != 0m && candle.LowPrice <= _stop) || (_take != 0m && candle.HighPrice >= _take))
				ClosePosition();
		}
		else if (Position < 0)
		{
			if ((_stop != 0m && candle.HighPrice >= _stop) || (_take != 0m && candle.LowPrice <= _take))
				ClosePosition();
		}
	}
}

