using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator flips.
/// Detects when Supertrend direction changes and trades accordingly.
/// </summary>
public class TradingViewSupertrendFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSupertrendValue;
	private bool _prevIsUpTrend;
	private bool _hasPrevValues;

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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the TradingView Supertrend Flip strategy.
	/// </summary>
	public TradingViewSupertrendFlipStrategy()
	{
		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetDisplay("Supertrend Period", "Period for Supertrend calculation", "Indicators")
			.SetOptimize(7, 14, 1);

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 4.0m)
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend", "Indicators")
			.SetOptimize(3.0m, 5.0m, 0.5m);

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
		_prevSupertrendValue = default;
		_prevIsUpTrend = default;
		_hasPrevValues = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var supertrend = new SuperTrend
		{
			Length = SupertrendPeriod,
			Multiplier = SupertrendMultiplier
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (supertrendValue == 0)
			return;

		// Determine trend direction
		var isUpTrend = candle.ClosePrice > supertrendValue;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevIsUpTrend = isUpTrend;
			_prevSupertrendValue = supertrendValue;
			return;
		}

		// Detect flip
		var isFlippedBullish = isUpTrend && !_prevIsUpTrend;
		var isFlippedBearish = !isUpTrend && _prevIsUpTrend;

		_prevIsUpTrend = isUpTrend;
		_prevSupertrendValue = supertrendValue;

		if (isFlippedBullish && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (isFlippedBearish && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}
