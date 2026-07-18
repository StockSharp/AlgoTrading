namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Specific Day Time strategy: SMA crossover with time filter.
/// Trades only during specific hours, using fast/slow SMA crossover.
/// </summary>
public class SpecificDayTimeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _tradeHour;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int TradeHour { get => _tradeHour.Value; set => _tradeHour.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public SpecificDayTimeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Fast SMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Slow SMA period", "Indicators");
		_tradeHour = Param(nameof(TradeHour), 12)
			.SetDisplay("Trade Hour", "Hour when entries are allowed", "Trading")
			.SetRange(0, 23);
		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_cooldown = 0;
		var fast = new SimpleMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_cooldown > 0)
			_cooldown--;

		var bullishTrend = fastValue > slowValue;
		var bearishTrend = fastValue < slowValue;

		if (_cooldown == 0 && candle.OpenTime.Hour == TradeHour)
		{
			if (bullishTrend && Position <= 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (bearishTrend && Position >= 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (_cooldown == 0)
		{
			if (Position > 0 && bearishTrend)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0 && bullishTrend)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
