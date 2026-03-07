namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Triple EMA crossover with Supertrend filter strategy (simplified 3Kilos).
/// Uses fast/slow EMA crossover for entries.
/// Uses SuperTrend indicator for trend confirmation and exits.
/// </summary>
public class ThreeKilosBtc15mStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private SuperTrend _superTrend;

	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeKilosBtc15mStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation", "General");

		_fastLength = Param(nameof(FastLength), 8)
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 21)
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
			.SetGreaterThanZero();

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastEma = null;
		_slowEma = null;
		_superTrend = null;
		_prevFast = 0;
		_prevSlow = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		_superTrend = new SuperTrend { Length = 10, Multiplier = 2m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastEma, _slowEma, _superTrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue slowVal, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_superTrend.IsFormed)
			return;

		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// SuperTrend direction
		var isUpTrend = stVal is SuperTrendIndicatorValue sv && sv.IsUpTrend;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		// EMA crossover
		var bullCross = _prevFast > 0 && _prevFast <= _prevSlow && fast > slow;
		var bearCross = _prevFast > 0 && _prevFast >= _prevSlow && fast < slow;

		// Buy: bullish EMA cross + Supertrend uptrend
		if (bullCross && isUpTrend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: bearish EMA cross + Supertrend downtrend
		else if (bearCross && !isUpTrend && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: Supertrend flips to downtrend
		else if (Position > 0 && !isUpTrend && bearCross)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: Supertrend flips to uptrend
		else if (Position < 0 && isUpTrend && bullCross)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
