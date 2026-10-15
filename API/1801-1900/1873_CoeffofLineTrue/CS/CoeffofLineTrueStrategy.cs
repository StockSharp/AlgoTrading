using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on zero crossing of a smoothed slope proxy.
/// </summary>
public class CoeffofLineTrueStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _slopePeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _slopes = [];
	private ExponentialMovingAverage _slopeProxy = null!;
	private decimal? _prevValue;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int SlopePeriod { get => _slopePeriod.Value; set => _slopePeriod.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public bool BuyPosOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellPosOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyPosClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellPosClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public CoeffofLineTrueStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_slopePeriod = Param(nameof(SlopePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slope Period", "Slope proxy length", "Parameters");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Historical bar index for signal", "Parameters");

		_buyOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_slopes.Clear();
		_slopeProxy = null!;
		_prevValue = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);
		_slopeProxy = new ExponentialMovingAverage { Length = SlopePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slopeProxy, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal proxyValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_prevValue is not decimal prevValue)
		{
			_prevValue = proxyValue;
			return;
		}

		var slope = proxyValue - prevValue;
		_prevValue = proxyValue;
		_slopes.Add(slope);
		if (_slopes.Count > SignalBar + 2)
			_slopes.RemoveAt(0);
		if (_slopes.Count <= SignalBar + 1)
			return;

		var prev = _slopes[^ (SignalBar + 1)];
		var prev2 = _slopes[^ (SignalBar + 2)];
		var buyOpen = BuyPosOpen && prev2 <= 0m && prev > 0m;
		var sellOpen = SellPosOpen && prev2 >= 0m && prev < 0m;
		var buyClose = BuyPosClose && prev2 >= 0m && prev < 0m;
		var sellClose = SellPosClose && prev2 <= 0m && prev > 0m;

		if (buyClose && Position > 0)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}

		if (sellClose && Position < 0)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}

		if (_cooldownRemaining == 0 && buyOpen)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}

		if (_cooldownRemaining == 0 && sellOpen)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
	}
}
