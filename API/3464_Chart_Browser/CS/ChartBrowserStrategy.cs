using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum ChartBrowserTab
{
	Charts,
	Indicators,
	Experts,
	Scripts
}

public class ChartBrowserStrategy : Strategy
{
	private readonly StrategyParam<DataType> _chartCandleType;
	private readonly StrategyParam<DataType> _indicatorCandleType;
	private readonly StrategyParam<int> _indicatorLength;
	private readonly StrategyParam<ChartBrowserTab> _activeTab;
	private readonly StrategyParam<TimeSpan> _summaryInterval;
	private readonly StrategyParam<bool> _autoLogLatestValues;

	private MarketDataSubscription? _chartSubscription;
	private MarketDataSubscription? _indicatorSubscription;

	private ICandleMessage? _lastChartCandle;
	private decimal? _lastIndicatorValue;
	private DateTimeOffset? _lastIndicatorTime;
	private DateTimeOffset? _lastSummaryTime;

	private int _totalOrders;
	private int _totalTrades;

	public ChartBrowserStrategy()
	{
		_chartCandleType = Param(nameof(ChartCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Chart Candle Type", "Timeframe used for the charts tab", "General");

		_indicatorCandleType = Param(nameof(IndicatorCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Indicator Candle Type", "Timeframe used to calculate the sample indicator", "General");

		_indicatorLength = Param(nameof(IndicatorLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Indicator Length", "Period of the SMA displayed on the indicators tab", "Indicator");

		_activeTab = Param(nameof(ActiveTab), ChartBrowserTab.Charts)
			.SetDisplay("Active Tab", "Tab that should be highlighted when new data arrives", "Display");

		_summaryInterval = Param(nameof(SummaryInterval), TimeSpan.FromMinutes(1))
			.SetDisplay("Summary Interval", "How often the strategy logs the complete summary", "Display");

		_autoLogLatestValues = Param(nameof(AutoLogLatestValues), true)
			.SetDisplay("Auto Log Latest Values", "Log active tab information when new data arrives", "Display");
	}

	public DataType ChartCandleType
	{
		get => _chartCandleType.Value;
		set => _chartCandleType.Value = value;
	}

	public DataType IndicatorCandleType
	{
		get => _indicatorCandleType.Value;
		set => _indicatorCandleType.Value = value;
	}

	public int IndicatorLength
	{
		get => _indicatorLength.Value;
		set => _indicatorLength.Value = value;
	}

	public ChartBrowserTab ActiveTab
	{
		get => _activeTab.Value;
		set
		{
			if (_activeTab.Value == value)
				return;

			_activeTab.Value = value;

			if (ProcessState == ProcessStates.Started)
			{
				LogInfo($"Active tab changed to {value}.");
				LogInfo(BuildTabDescription(value));
			}
		}
	}

	public TimeSpan SummaryInterval
	{
		get => _summaryInterval.Value;
		set => _summaryInterval.Value = value;
	}

	public bool AutoLogLatestValues
	{
		get => _autoLogLatestValues.Value;
		set => _autoLogLatestValues.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var security = Security;

		if (security == null)
			yield break;

		yield return (security, ChartCandleType);

		if (IndicatorCandleType != ChartCandleType)
			yield return (security, IndicatorCandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_chartSubscription?.Dispose();
		_chartSubscription = null;

		_indicatorSubscription?.Dispose();
		_indicatorSubscription = null;

		_lastChartCandle = null;
		_lastIndicatorValue = null;
		_lastIndicatorTime = null;
		_lastSummaryTime = null;

		_totalOrders = 0;
		_totalTrades = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
		{
			LogError("Security is not assigned to the strategy.");
			Stop();
			return;
		}

		_totalOrders = 0;
		_totalTrades = 0;
		_lastChartCandle = null;
		_lastIndicatorValue = null;
		_lastIndicatorTime = null;
		_lastSummaryTime = null;

		var chartSubscription = SubscribeCandles(ChartCandleType);
		_chartSubscription = chartSubscription;
		chartSubscription.Bind(OnChartCandle).Start();

		var indicator = new SMA
		{
			Length = IndicatorLength
		};

		var indicatorSubscription = SubscribeCandles(IndicatorCandleType);
		_indicatorSubscription = indicatorSubscription;
		indicatorSubscription.Bind(indicator, OnIndicatorCandle).Start();

		LogInfo($"Chart browser initialized. Chart TF: {ChartCandleType}. Indicator TF: {IndicatorCandleType}. SMA length: {IndicatorLength}.");
		LogInfo(BuildSummaryMessage());

		if (SummaryInterval > TimeSpan.Zero)
			Timer.Start(SummaryInterval, OnSummaryTimer);
	}

	protected override void OnStopped()
	{
		base.OnStopped();

		Timer.Stop();

		_chartSubscription?.Dispose();
		_chartSubscription = null;

		_indicatorSubscription?.Dispose();
		_indicatorSubscription = null;
	}

	protected override void OnOrderRegistered(Order order)
	{
		base.OnOrderRegistered(order);

		if (order.Security != Security)
			return;

		_totalOrders++;

		if (AutoLogLatestValues && ActiveTab == ChartBrowserTab.Experts)
			LogInfo(BuildTabDescription(ChartBrowserTab.Experts));
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order?.Security != Security)
			return;

		_totalTrades++;

		if (AutoLogLatestValues && ActiveTab == ChartBrowserTab.Experts)
			LogInfo(BuildTabDescription(ChartBrowserTab.Experts));
	}

	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (AutoLogLatestValues && ActiveTab == ChartBrowserTab.Experts)
			LogInfo(BuildTabDescription(ChartBrowserTab.Experts));
	}

	private void OnChartCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastChartCandle = candle;

		if (AutoLogLatestValues && ActiveTab == ChartBrowserTab.Charts)
			LogInfo(BuildTabDescription(ChartBrowserTab.Charts));
	}

	private void OnIndicatorCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastIndicatorValue = smaValue;
		_lastIndicatorTime = candle.OpenTime;

		if (AutoLogLatestValues && ActiveTab == ChartBrowserTab.Indicators)
			LogInfo(BuildTabDescription(ChartBrowserTab.Indicators));
	}

	private void OnSummaryTimer()
	{
		if (SummaryInterval <= TimeSpan.Zero)
			return;

		_lastSummaryTime = CurrentTime;
		LogInfo(BuildSummaryMessage());
	}

	private string BuildSummaryMessage()
	{
		var separator = Environment.NewLine + "- ";
		var summary = $"Chart browser summary at {CurrentTime:O}:{separator}{BuildTabDescription(ChartBrowserTab.Charts)}" +
			$"{separator}{BuildTabDescription(ChartBrowserTab.Indicators)}" +
			$"{separator}{BuildTabDescription(ChartBrowserTab.Experts)}" +
			$"{separator}{BuildTabDescription(ChartBrowserTab.Scripts)}";

		return summary;
	}

	private string BuildTabDescription(ChartBrowserTab tab)
	{
		switch (tab)
		{
			case ChartBrowserTab.Charts:
			{
				if (_lastChartCandle == null)
					return $"Charts tab: waiting for the first finished {ChartCandleType} candle.";

				var candle = _lastChartCandle;
				return $"Charts tab: {ChartCandleType} candle at {candle.OpenTime:O} - O:{candle.OpenPrice} H:{candle.HighPrice} L:{candle.LowPrice} C:{candle.ClosePrice} V:{candle.TotalVolume}.";
			}
			case ChartBrowserTab.Indicators:
			{
				if (_lastIndicatorValue == null || _lastIndicatorTime == null)
					return $"Indicators tab: waiting for SMA({IndicatorLength}) on {IndicatorCandleType}.";

				return $"Indicators tab: SMA({IndicatorLength}) on {IndicatorCandleType} at {_lastIndicatorTime:O} = {_lastIndicatorValue}.";
			}
			case ChartBrowserTab.Experts:
			{
				var activeOrders = ActiveOrders.Count;
				return $"Experts tab: total orders {_totalOrders}, active orders {activeOrders}, executed trades {_totalTrades}, position {Position}.";
			}
			case ChartBrowserTab.Scripts:
			{
				var lastTime = _lastSummaryTime;
				var refreshInfo = lastTime == null ? "no summary generated yet" : $"last summary at {lastTime:O}";
				return $"Scripts tab: summary interval {SummaryInterval}. Auto logging {(AutoLogLatestValues ? "enabled" : "disabled")}, {refreshInfo}.";
			}
			default:
				return "Unknown tab.";
		}
	}
}
