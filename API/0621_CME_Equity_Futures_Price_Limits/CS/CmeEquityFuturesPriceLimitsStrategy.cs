using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that calculates CME equity futures price limit levels.
/// </summary>
public class CmeEquityFuturesPriceLimitsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _manualReference;
	private readonly StrategyParam<bool> _showLimitDownLevels;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _offsetHour;

	private decimal _reference;
	private int _prevHour = -1;

	/// <summary>
	/// Manual reference price override (0 to disable).
	/// </summary>
	public decimal ManualReference
	{
		get => _manualReference.Value;
		set => _manualReference.Value = value;
	}

	/// <summary>
	/// Show -7/-13/-20% limit-down levels.
	/// </summary>
	public bool ShowLimitDownLevels
	{
		get => _showLimitDownLevels.Value;
		set => _showLimitDownLevels.Value = value;
	}

	/// <summary>
	/// Hour to capture the daily reference price.
	/// </summary>
	public int OffsetHour
	{
		get => _offsetHour.Value;
		set => _offsetHour.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CmeEquityFuturesPriceLimitsStrategy()
	{
		_manualReference = Param(nameof(ManualReference), 0m)
			.SetDisplay("Manual Reference", "Manual reference price (0 to disable)", "General");

		_showLimitDownLevels = Param(nameof(ShowLimitDownLevels), true)
			.SetDisplay("Show Limit Down Levels", "Log -7/-13/-20% levels", "General");

		_offsetHour = Param(nameof(OffsetHour), 20)
			.SetDisplay("Reference Hour", "Hour to capture reference price", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_reference = 0m;
		_prevHour = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		if (hour == OffsetHour && _prevHour != OffsetHour)
		{
			_reference = ManualReference != 0m ? ManualReference : candle.OpenPrice;

			var limitUp = _reference * 1.05m;
			var limitDown = _reference * 0.95m;

			LogInfo($"Reference: {_reference}, +5%: {limitUp}, -5%: {limitDown}");

			if (ShowLimitDownLevels)
			{
				var limit7 = _reference * 0.93m;
				var limit13 = _reference * 0.87m;
				var limit20 = _reference * 0.80m;

				LogInfo($"-7%: {limit7}, -13%: {limit13}, -20%: {limit20}");
			}
		}

		_prevHour = hour;
	}
}

