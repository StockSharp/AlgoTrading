using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Bulls/Bears power balance indicator.
/// </summary>
public class BullsBearsEyesStrategy : Strategy {
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private BullsPower _bulls;
	private BearsPower _bears;
	private int _trend;

	/// <summary>
	/// Averaging period for Bulls/Bears Power.
	/// </summary>
	public int Period {
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Overbought threshold.
	/// </summary>
	public decimal HighLevel {
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Middle level.
	/// </summary>
	public decimal MiddleLevel {
		get => _middleLevel.Value;
		set => _middleLevel.Value = value;
	}

	/// <summary>
	/// Oversold threshold.
	/// </summary>
	public decimal LowLevel {
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BullsBearsEyesStrategy"/>.
	/// </summary>
	public BullsBearsEyesStrategy() {
		_period = Param(nameof(Period), 13)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Indicator averaging period", "Parameters")
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 75m)
			.SetDisplay("High Level", "Overbought level", "Parameters")
			.SetCanOptimize(true);

		_middleLevel = Param(nameof(MiddleLevel), 50m)
			.SetDisplay("Middle Level", "Middle threshold", "Parameters");

		_lowLevel = Param(nameof(LowLevel), 25m)
			.SetDisplay("Low Level", "Oversold level", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles for analysis", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
		base.OnReseted();

		_bulls = default;
		_bears = default;
		_trend = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		StartProtection();

		_bulls = new BullsPower { Length = Period };
		_bears = new BearsPower { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_bulls, _bears, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _bulls);
			DrawIndicator(area, _bears);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsValue, decimal bearsValue) {
		if (candle.State != CandleStates.Finished)
			return;

		var sum = Math.Abs(bullsValue) + Math.Abs(bearsValue);
		var value = sum == 0 ? 50m : 50m + 50m * (bullsValue - bearsValue) / sum;

		var prevTrend = _trend;

		if (value > HighLevel)
			_trend = prevTrend <= 0 ? 2 : 1;
		else if (value < LowLevel)
			_trend = prevTrend >= 0 ? -2 : -1;
		else
			_trend = prevTrend switch { > 0 => 1, < 0 => -1, _ => 0 };

		if (_trend > 0) {
			if (prevTrend < 0 && Position < 0)
				BuyMarket(-Position);

			if (_trend == 2 && Position <= 0)
				BuyMarket(Volume + (Position < 0 ? -Position : 0m));
		} else if (_trend < 0) {
			if (prevTrend > 0 && Position > 0)
				SellMarket(Position);

			if (_trend == -2 && Position >= 0)
				SellMarket(Volume + (Position > 0 ? Position : 0m));
		}
	}
}

