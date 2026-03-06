using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend distance mean reversion strategy.
/// Trades large deviations of price from Supertrend and exits when the distance returns to its recent average.
/// </summary>
public class SupertrendDistanceMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _supertrend;
	private decimal[] _distanceHistory;
	private int _currentIndex;
	private int _filledCount;
	private int _cooldown;

	/// <summary>
	/// ATR period for Supertrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend calculation.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Lookback period for distance statistics.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier for mean reversion detection.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Cooldown bars between orders.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Initializes a new instance of <see cref="SupertrendDistanceMeanReversionStrategy"/>.
	/// </summary>
	public SupertrendDistanceMeanReversionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for Supertrend calculation", "Supertrend")
			.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Supertrend")
			.SetOptimize(1m, 5m, 0.5m);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Lookback period for distance statistics", "Strategy Parameters")
			.SetOptimize(10, 50, 5);

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")
			.SetOptimize(1m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 1200)
			.SetRange(1, 5000)
			.SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_supertrend = null;
		_currentIndex = default;
		_filledCount = default;
		_cooldown = default;
		_distanceHistory = new decimal[LookbackPeriod];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier };
		_distanceHistory = new decimal[LookbackPeriod];
		_currentIndex = 0;
		_filledCount = 0;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_supertrend, ProcessSupertrend)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessSupertrend(ICandleMessage candle, decimal supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_supertrend.IsFormed)
			return;

		var distance = Math.Abs(candle.ClosePrice - supertrendValue);

		_distanceHistory[_currentIndex] = distance;
		_currentIndex = (_currentIndex + 1) % LookbackPeriod;

		if (_filledCount < LookbackPeriod)
			_filledCount++;

		if (_filledCount < LookbackPeriod)
			return;

		var avgDistance = 0m;
		var sumSq = 0m;

		for (var i = 0; i < LookbackPeriod; i++)
			avgDistance += _distanceHistory[i];

		avgDistance /= LookbackPeriod;

		for (var i = 0; i < LookbackPeriod; i++)
		{
			var diff = _distanceHistory[i] - avgDistance;
			sumSq += diff * diff;
		}

		var stdDistance = (decimal)Math.Sqrt((double)(sumSq / LookbackPeriod));

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var extendedThreshold = avgDistance + stdDistance * DeviationMultiplier;
		var priceAboveSupertrend = candle.ClosePrice > supertrendValue;
		var priceBelowSupertrend = candle.ClosePrice < supertrendValue;

		if (Position == 0)
		{
			if (distance > extendedThreshold)
			{
				if (priceAboveSupertrend)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
				else if (priceBelowSupertrend)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
			}
		}
		else if (Position > 0 && (distance <= avgDistance || priceAboveSupertrend))
		{
			SellMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && (distance <= avgDistance || priceBelowSupertrend))
		{
			BuyMarket(Math.Abs(Position));
			_cooldown = CooldownBars;
		}
	}
}
