using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Up Gap with Delay strategy.
/// Goes long after a gap up if sufficient bars have passed since previous trade.
/// Exits after holding for specified number of bars.
/// </summary>
public class UpGapWithDelayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<int> _delayPeriods;
	private readonly StrategyParam<int> _holdingPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private long? _entryIndex;
	private long _currentIndex;

	/// <summary>
	/// Gap size threshold in percent.
	/// </summary>
	public decimal GapThreshold
	{
		get => _gapThreshold.Value;
		set => _gapThreshold.Value = value;
	}

	/// <summary>
	/// Bars to wait before next entry.
	/// </summary>
	public int DelayPeriods
	{
		get => _delayPeriods.Value;
		set => _delayPeriods.Value = value;
	}

	/// <summary>
	/// Bars to hold position.
	/// </summary>
	public int HoldingPeriods
	{
		get => _holdingPeriods.Value;
		set => _holdingPeriods.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="UpGapWithDelayStrategy"/>.
	/// </summary>
	public UpGapWithDelayStrategy()
	{
		_gapThreshold = Param(nameof(GapThreshold), 1m)
			.SetDisplay("Gap Threshold (%)", "Minimum gap size", "General")
			.SetCanOptimize(true);

		_delayPeriods = Param(nameof(DelayPeriods), 0)
			.SetDisplay("Delay Periods", "Bars to wait", "General")
			.SetCanOptimize(true);

		_holdingPeriods = Param(nameof(HoldingPeriods), 7)
			.SetGreaterThanZero()
			.SetDisplay("Holding Periods", "Bars to hold", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevClose = default;
		_entryIndex = default;
		_currentIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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

		_currentIndex++;

		if (_prevClose != null)
		{
			var gapSize = (candle.OpenPrice - _prevClose.Value) / _prevClose.Value * 100m;
			var upGap = gapSize >= GapThreshold;
			var canEnter = upGap && (_entryIndex == null || _currentIndex > _entryIndex + DelayPeriods);

			if (canEnter && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
				_entryIndex = _currentIndex;
			}

			if (_entryIndex != null && _currentIndex >= _entryIndex + DelayPeriods + HoldingPeriods && Position > 0)
			{
				SellMarket(Position);
				_entryIndex = null;
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
