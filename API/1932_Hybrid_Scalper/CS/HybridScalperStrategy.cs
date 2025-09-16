using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hybrid scalping strategy based on Stochastic Oscillator, RSI and Bollinger Bands.
/// </summary>
public class HybridScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaFastPeriod;
	private readonly StrategyParam<int> _emaSlowPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<bool> _tradeMonday;
	private readonly StrategyParam<bool> _tradeTuesday;
	private readonly StrategyParam<bool> _tradeWednesday;
	private readonly StrategyParam<bool> _tradeThursday;
	private readonly StrategyParam<bool> _tradeFriday;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for trend detection.
	/// </summary>
	public int EmaFastPeriod
	{
		get => _emaFastPeriod.Value;
		set => _emaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for trend detection.
	/// </summary>
	public int EmaSlowPeriod
	{
		get => _emaSlowPeriod.Value;
		set => _emaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BbDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	/// <summary>
	/// Allow trading on Monday.
	/// </summary>
	public bool TradeMonday
	{
		get => _tradeMonday.Value;
		set => _tradeMonday.Value = value;
	}

	/// <summary>
	/// Allow trading on Tuesday.
	/// </summary>
	public bool TradeTuesday
	{
		get => _tradeTuesday.Value;
		set => _tradeTuesday.Value = value;
	}

	/// <summary>
	/// Allow trading on Wednesday.
	/// </summary>
	public bool TradeWednesday
	{
		get => _tradeWednesday.Value;
		set => _tradeWednesday.Value = value;
	}

	/// <summary>
	/// Allow trading on Thursday.
	/// </summary>
	public bool TradeThursday
	{
		get => _tradeThursday.Value;
		set => _tradeThursday.Value = value;
	}

	/// <summary>
	/// Allow trading on Friday.
	/// </summary>
	public bool TradeFriday
	{
		get => _tradeFriday.Value;
		set => _tradeFriday.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HybridScalperStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 7)
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true);

		_emaFastPeriod = Param(nameof(EmaFastPeriod), 21)
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
			.SetCanOptimize(true);

		_emaSlowPeriod = Param(nameof(EmaSlowPeriod), 89)
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
			.SetCanOptimize(true);

		_bbPeriod = Param(nameof(BbPeriod), 50)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
			.SetCanOptimize(true);

		_bbDeviation = Param(nameof(BbDeviation), 4m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
			.SetCanOptimize(true);

		_tradeMonday = Param(nameof(TradeMonday), true)
			.SetDisplay("Trade Monday", "Allow trading on Monday", "Schedule");

		_tradeTuesday = Param(nameof(TradeTuesday), true)
			.SetDisplay("Trade Tuesday", "Allow trading on Tuesday", "Schedule");

		_tradeWednesday = Param(nameof(TradeWednesday), true)
			.SetDisplay("Trade Wednesday", "Allow trading on Wednesday", "Schedule");

		_tradeThursday = Param(nameof(TradeThursday), true)
			.SetDisplay("Trade Thursday", "Allow trading on Thursday", "Schedule");

		_tradeFriday = Param(nameof(TradeFriday), true)
			.SetDisplay("Trade Friday", "Allow trading on Friday", "Schedule");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stochastic = new StochasticOscillator
		{
			K = { Length = 5 },
			D = { Length = 3 },
		};
		var emaFast = new ExponentialMovingAverage { Length = EmaFastPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = EmaSlowPeriod };
		var bollinger = new BollingerBands
		{
			Length = BbPeriod,
			Width = BbDeviation,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, stochastic, emaFast, emaSlow, bollinger, ProcessIndicators)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, stochastic);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochValue, IIndicatorValue emaFastValue, IIndicatorValue emaSlowValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsTradingDay(candle.OpenTime.UtcDateTime.DayOfWeek))
			return;

		var rsi = rsiValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK || stoch.D is not decimal stochD)
			return;

		var emaFast = emaFastValue.ToDecimal();
		var emaSlow = emaSlowValue.ToDecimal();

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower || bb.MovingAverage is not decimal middle)
			return;

		var width = upper - lower;
		if (width > 0.00262m || width < 0.00045m)
			return;

		if (stochK < 20 && stochD < stochK && rsi < 25 && emaFast > emaSlow && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (stochK > 80 && stochD < stochK && rsi > 85 && emaFast < emaSlow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}

	private bool IsTradingDay(DayOfWeek day)
	{
		return day switch
		{
			DayOfWeek.Monday => TradeMonday,
			DayOfWeek.Tuesday => TradeTuesday,
			DayOfWeek.Wednesday => TradeWednesday,
			DayOfWeek.Thursday => TradeThursday,
			DayOfWeek.Friday => TradeFriday,
			_ => false,
		};
	}
}
