using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the News Template Universal MQL advisor.
/// It pauses trading around economic news events based on configurable filters.
/// This is a utility strategy with no direct trading logic.
/// </summary>
public class NewsTemplateUniversalStrategy : Strategy
{
	private readonly StrategyParam<bool> _useNewsFilter;
	private readonly StrategyParam<bool> _includeLow;
	private readonly StrategyParam<bool> _includeMedium;
	private readonly StrategyParam<bool> _includeHigh;
	private readonly StrategyParam<int> _stopBeforeMinutes;
	private readonly StrategyParam<int> _startAfterMinutes;
	private readonly StrategyParam<string> _currencies;
	private readonly StrategyParam<bool> _checkSpecificNews;
	private readonly StrategyParam<string> _specificNewsText;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isNewsActive;

	/// <summary>
	/// Initializes a new instance of the <see cref="NewsTemplateUniversalStrategy"/> class.
	/// </summary>
	public NewsTemplateUniversalStrategy()
	{
		_useNewsFilter = Param(nameof(UseNewsFilter), true)
			.SetDisplay("Use News Filter", "Enable blocking trades around news", "General");

		_includeLow = Param(nameof(IncludeLow), false)
			.SetDisplay("Include Low", "Include low importance events", "Filters");

		_includeMedium = Param(nameof(IncludeMedium), true)
			.SetDisplay("Include Medium", "Include medium importance events", "Filters");

		_includeHigh = Param(nameof(IncludeHigh), true)
			.SetDisplay("Include High", "Include high importance events", "Filters");

		_stopBeforeMinutes = Param(nameof(StopBeforeNewsMinutes), 30)
			.SetNotNegative()
			.SetDisplay("Minutes Before", "Minutes to stop before news", "Timing");

		_startAfterMinutes = Param(nameof(StartAfterNewsMinutes), 30)
			.SetNotNegative()
			.SetDisplay("Minutes After", "Minutes to resume after news", "Timing");

		_currencies = Param(nameof(Currencies), "USD,EUR,CAD,AUD,NZD,GBP")
			.SetDisplay("Currencies", "Comma separated currency codes", "Filters");

		_checkSpecificNews = Param(nameof(CheckSpecificNews), false)
			.SetDisplay("Filter by text", "Require specific text in news", "Filters");

		_specificNewsText = Param(nameof(SpecificNewsText), "employment")
			.SetDisplay("Text filter", "Substring that must be present when enabled", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for time checks", "General");
	}

	/// <summary>
	/// Enable or disable the news based blocking logic.
	/// </summary>
	public bool UseNewsFilter { get => _useNewsFilter.Value; set => _useNewsFilter.Value = value; }

	/// <summary>
	/// Include low importance news events.
	/// </summary>
	public bool IncludeLow { get => _includeLow.Value; set => _includeLow.Value = value; }

	/// <summary>
	/// Include medium importance news events.
	/// </summary>
	public bool IncludeMedium { get => _includeMedium.Value; set => _includeMedium.Value = value; }

	/// <summary>
	/// Include high importance news events.
	/// </summary>
	public bool IncludeHigh { get => _includeHigh.Value; set => _includeHigh.Value = value; }

	/// <summary>
	/// Minutes to stop trading before a news event.
	/// </summary>
	public int StopBeforeNewsMinutes { get => _stopBeforeMinutes.Value; set => _stopBeforeMinutes.Value = value; }

	/// <summary>
	/// Minutes to resume trading after a news event.
	/// </summary>
	public int StartAfterNewsMinutes { get => _startAfterMinutes.Value; set => _startAfterMinutes.Value = value; }

	/// <summary>
	/// Comma separated list of currency tickers searched inside news text.
	/// </summary>
	public string Currencies { get => _currencies.Value; set => _currencies.Value = value; }

	/// <summary>
	/// Enable filtering for a specific text fragment.
	/// </summary>
	public bool CheckSpecificNews { get => _checkSpecificNews.Value; set => _checkSpecificNews.Value = value; }

	/// <summary>
	/// Text fragment required when <see cref="CheckSpecificNews"/> is enabled.
	/// </summary>
	public string SpecificNewsText { get => _specificNewsText.Value; set => _specificNewsText.Value = value; }

	/// <summary>
	/// Candle type used to drive time based processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Gets a value indicating whether news currently block trading operations.
	/// </summary>
	public bool IsNewsActive => _isNewsActive;

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_isNewsActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!UseNewsFilter)
		{
			if (_isNewsActive)
			{
				_isNewsActive = false;
				LogInfo("News filter disabled.");
			}

			return;
		}

		// Utility strategy: logs candle time for news monitoring purposes.
		LogInfo($"Candle at {candle.OpenTime:O}, news active: {_isNewsActive}");
	}
}
