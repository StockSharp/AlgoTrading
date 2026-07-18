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
/// Strategy based on Supertrend indicator and ADX for trend strength confirmation.
/// 
/// Entry criteria:
/// Long: Price > Supertrend && ADX > 25 (uptrend with strong movement)
/// Short: Price < Supertrend && ADX > 25 (downtrend with strong movement)
/// 
/// Exit criteria:
/// Long: Price < Supertrend (price falls below Supertrend)
/// Short: Price > Supertrend (price rises above Supertrend)
/// </summary>
public class SupertrendAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastSupertrend;
	private bool _isAboveSupertrend;
	private int _cooldown;

	/// <summary>
	/// Period for Supertrend calculation.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Period for ADX calculation.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for ADX to confirm trend strength.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public SupertrendAdxStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "Period for ATR calculation in Supertrend", "Indicators")
			
			.SetOptimize(5, 20, 5);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Indicators")
			
			.SetOptimize(1.0m, 5.0m, 1.0m);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			
			.SetOptimize(7, 21, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 30m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators")
			
			.SetOptimize(20m, 30m, 5m);

		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetRange(1, 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_lastSupertrend = 0;
		_isAboveSupertrend = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		var dummyEma = new ExponentialMovingAverage { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, dummyEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stVal, IIndicatorValue dummyVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stVal is not SuperTrendIndicatorValue st)
			return;
		var isUpTrend = st.IsUpTrend;
		var trendChanged = isUpTrend != _isAboveSupertrend && _lastSupertrend > 0;

		if (_cooldown > 0)
			_cooldown--;

		if (_cooldown == 0 && trendChanged)
		{
			if (isUpTrend && Position <= 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (!isUpTrend && Position >= 0)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}

		_lastSupertrend = 1;
		_isAboveSupertrend = isUpTrend;
	}
}
