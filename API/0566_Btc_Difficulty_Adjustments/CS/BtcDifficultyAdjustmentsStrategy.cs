using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC Difficulty Adjustments Strategy - trades on mining difficulty changes.
/// </summary>
public class BtcDifficultyAdjustmentsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _thresholdMode;
	private readonly StrategyParam<decimal> _threshold;

	private decimal? _prev1;
	private decimal? _prev2;
	private decimal? _prev3;
	private decimal? _prev4;
	private bool _prevChanged;

	/// <summary>
	/// Candle type for difficulty data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable threshold mode.
	/// </summary>
	public bool ThresholdMode
	{
		get => _thresholdMode.Value;
		set => _thresholdMode.Value = value;
	}

	/// <summary>
	/// Percentage threshold for difficulty change.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BtcDifficultyAdjustmentsStrategy"/> class.
	/// </summary>
	public BtcDifficultyAdjustmentsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_thresholdMode = Param(nameof(ThresholdMode), false)
			.SetDisplay("Threshold Mode", "Enable threshold filtering", "Trading");

		_threshold = Param(nameof(Threshold), 10m)
			.SetDisplay("Threshold %", "Minimum percentage change", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var difficulty = candle.ClosePrice;

		var equal = _prev1.HasValue && difficulty == _prev1.Value;
		var higher = _prev1.HasValue && difficulty > _prev1.Value;
		var lower = _prev1.HasValue && difficulty < _prev1.Value;
		var changed = _prev1.HasValue && !equal;

		decimal change = 0m;
		if (equal && _prevChanged)
		{
			if (_prev1.HasValue && _prev2.HasValue && _prev1 == _prev2)
				change = ((_prev1.Value - difficulty) / _prev1.Value) * 100m;
			else if (_prev2.HasValue && _prev3.HasValue && _prev2 == _prev3)
				change = ((_prev2.Value - difficulty) / _prev2.Value) * 100m;
			else if (_prev3.HasValue && _prev4.HasValue && _prev3 == _prev4)
				change = ((_prev3.Value - difficulty) / _prev3.Value) * 100m;
		}

		if (ThresholdMode)
		{
			if (Math.Abs(change) >= Threshold)
			{
				if (change < 0 && Position <= 0)
					BuyMarket();
				else if (change > 0 && Position >= 0)
					SellMarket();
			}
		}
		else
		{
			if (higher && Position <= 0)
				BuyMarket();
			else if (lower && Position >= 0)
				SellMarket();
		}

		_prevChanged = changed;
		_prev4 = _prev3;
		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = difficulty;
	}
}
