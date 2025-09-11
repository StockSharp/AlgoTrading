using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy trading breakouts of high or low levels from selected timeframe.
/// </summary>
public class HighLowBreakoutStatisticalAnalysisStrategy : Strategy
{
	public enum EntryOptions
	{
		LongAtHigh,
		ShortAtHigh,
		LongAtLow,
		ShortAtLow
	}

	public enum TimeframeOptions
	{
		Daily,
		Weekly,
		Monthly
	}

	private readonly StrategyParam<EntryOptions> _entryOption;
	private readonly StrategyParam<TimeframeOptions> _timeframeOption;
	private readonly StrategyParam<int> _holdingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _highLevel;
	private decimal _lowLevel;
	private decimal _prevClose;
	private bool _hasPrevClose;
	private bool _levelsInitialized;
	private int _barIndex;
	private int _entryBarIndex = -1;

	/// <summary>
	/// Entry option.
	/// </summary>
	public EntryOptions EntryOption
	{
		get => _entryOption.Value;
		set => _entryOption.Value = value;
	}

	/// <summary>
	/// Timeframe used for levels.
	/// </summary>
	public TimeframeOptions TimeframeOption
	{
		get => _timeframeOption.Value;
		set => _timeframeOption.Value = value;
	}

	/// <summary>
	/// Holding period in bars.
	/// </summary>
	public int HoldingPeriod
	{
		get => _holdingPeriod.Value;
		set => _holdingPeriod.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public HighLowBreakoutStatisticalAnalysisStrategy()
	{
		_entryOption = Param(nameof(EntryOption), EntryOptions.LongAtHigh)
			.SetDisplay("Entry Option", "Entry option", "General");

		_timeframeOption = Param(nameof(TimeframeOption), TimeframeOptions.Daily)
			.SetDisplay("Timeframe", "Timeframe for levels", "General");

		_holdingPeriod = Param(nameof(HoldingPeriod), 5)
			.SetDisplay("Holding Period", "Holding period in bars", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to trade", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, GetLevelDataType())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highLevel = default;
		_lowLevel = default;
		_prevClose = default;
		_hasPrevClose = false;
		_levelsInitialized = false;
		_barIndex = 0;
		_entryBarIndex = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var levelType = GetLevelDataType();

		var tradeSubscription = SubscribeCandles(CandleType);
		tradeSubscription
			.Bind(ProcessCandle)
			.Start();

		var levelSubscription = SubscribeCandles(levelType);
		levelSubscription
			.Bind(ProcessLevels)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSubscription);
			DrawOwnTrades(area);
		}
	}

	private DataType GetLevelDataType()
	{
		return TimeframeOption switch
		{
			TimeframeOptions.Daily => TimeSpan.FromDays(1).TimeFrame(),
			TimeframeOptions.Weekly => TimeSpan.FromDays(7).TimeFrame(),
			TimeframeOptions.Monthly => TimeSpan.FromDays(30).TimeFrame(),
			_ => TimeSpan.FromDays(1).TimeFrame(),
		};
	}

	private void ProcessLevels(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highLevel = candle.HighPrice;
		_lowLevel = candle.LowPrice;
		_levelsInitialized = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !_levelsInitialized)
			return;

		_barIndex++;

		if (Position != 0 && _entryBarIndex >= 0 && _barIndex - _entryBarIndex >= HoldingPeriod)
		{
			ClosePosition();
			_entryBarIndex = -1;
		}

		var crossedAboveHigh = _hasPrevClose && _prevClose <= _highLevel && candle.ClosePrice > _highLevel;
		var crossedBelowHigh = _hasPrevClose && _prevClose >= _highLevel && candle.ClosePrice < _highLevel;
		var crossedAboveLow = _hasPrevClose && _prevClose <= _lowLevel && candle.ClosePrice > _lowLevel;
		var crossedBelowLow = _hasPrevClose && _prevClose >= _lowLevel && candle.ClosePrice < _lowLevel;

		switch (EntryOption)
		{
			case EntryOptions.LongAtHigh when crossedAboveHigh && Position <= 0:
				BuyMarket();
				_entryBarIndex = _barIndex;
				break;
			case EntryOptions.ShortAtHigh when crossedBelowHigh && Position >= 0:
				SellMarket();
				_entryBarIndex = _barIndex;
				break;
			case EntryOptions.LongAtLow when crossedAboveLow && Position <= 0:
				BuyMarket();
				_entryBarIndex = _barIndex;
				break;
			case EntryOptions.ShortAtLow when crossedBelowLow && Position >= 0:
				SellMarket();
				_entryBarIndex = _barIndex;
				break;
		}

		_prevClose = candle.ClosePrice;
		_hasPrevClose = true;
	}
}
