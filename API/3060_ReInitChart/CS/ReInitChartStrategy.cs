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
/// Strategy that mimics the MetaTrader ReInitChart utility by resetting indicators on demand.
/// It resets a moving average either manually or on a timer and uses the SMA for simple trend-following entries.
/// </summary>
public class ReInitChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<bool> _autoRefreshEnabled;
	private readonly StrategyParam<TimeSpan> _autoRefreshInterval;
	private readonly StrategyParam<bool> _manualRefreshRequest;
	private readonly StrategyParam<string> _refreshCommandName;
	private readonly StrategyParam<string> _refreshCommandText;
	private readonly StrategyParam<string> _textColorName;
	private readonly StrategyParam<string> _backgroundColorName;

	private SimpleMovingAverage _sma = null!;
	private bool _manualRefreshArmed;
	private DateTimeOffset? _nextAutoRefreshTime;

	/// <summary>
	/// Primary candle type used to drive the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the moving average that is recalculated after each refresh.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Enables or disables automatic refresh events.
	/// </summary>
	public bool AutoRefreshEnabled
	{
		get => _autoRefreshEnabled.Value;
		set => _autoRefreshEnabled.Value = value;
	}

	/// <summary>
	/// Interval between automatic refresh operations.
	/// </summary>
	public TimeSpan AutoRefreshInterval
	{
		get => _autoRefreshInterval.Value;
		set => _autoRefreshInterval.Value = value;
	}

	/// <summary>
	/// Manual refresh flag that emulates the original MetaTrader button press.
	/// </summary>
	public bool ManualRefreshRequest
	{
		get => _manualRefreshRequest.Value;
		set => _manualRefreshRequest.Value = value;
	}

	/// <summary>
	/// Identifier of the refresh command, matching the MetaTrader button name.
	/// </summary>
	public string RefreshCommandName
	{
		get => _refreshCommandName.Value;
		set => _refreshCommandName.Value = value;
	}

	/// <summary>
	/// Text displayed for the refresh command.
	/// </summary>
	public string RefreshCommandText
	{
		get => _refreshCommandText.Value;
		set => _refreshCommandText.Value = value;
	}

	/// <summary>
	/// Text color descriptor preserved from the original script.
	/// </summary>
	public string TextColorName
	{
		get => _textColorName.Value;
		set => _textColorName.Value = value;
	}

	/// <summary>
	/// Background color descriptor preserved from the original script.
	/// </summary>
	public string BackgroundColorName
	{
		get => _backgroundColorName.Value;
		set => _backgroundColorName.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ReInitChartStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the primary chart subscription.", "Data");

		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Number of candles for the recalculated moving average.", "Reinitialization");

		_autoRefreshEnabled = Param(nameof(AutoRefreshEnabled), false)
			.SetDisplay("Auto Refresh", "Enable periodic indicator reinitialization.", "Reinitialization");

		_autoRefreshInterval = Param(nameof(AutoRefreshInterval), TimeSpan.FromMinutes(5))
			.SetDisplay("Refresh Interval", "Time interval between automatic refresh operations.", "Reinitialization");

		_manualRefreshRequest = Param(nameof(ManualRefreshRequest), false)
			.SetDisplay("Manual Refresh", "Set to true to trigger a one-time indicator reset.", "Reinitialization");

		_refreshCommandName = Param(nameof(RefreshCommandName), "ButtonReDraw")
			.SetDisplay("Command Name", "Identifier that matches the MetaTrader button name.", "Appearance");

		_refreshCommandText = Param(nameof(RefreshCommandText), "Recalculate")
			.SetDisplay("Command Text", "Text shown on logs to mirror the MetaTrader button caption.", "Appearance");

		_textColorName = Param(nameof(TextColorName), "NavajoWhite")
			.SetDisplay("Text Color", "Original MetaTrader button text color.", "Appearance");

		_backgroundColorName = Param(nameof(BackgroundColorName), "SlateGray")
			.SetDisplay("Background Color", "Original MetaTrader button background color.", "Appearance");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_manualRefreshArmed = false;
		_nextAutoRefreshTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage
		{
			Length = SmaLength,
		};

		// Subscribe to the primary candle stream and bind the moving average.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		// Draw candles, indicator and executed trades when charting is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		if (AutoRefreshEnabled)
		{
			_nextAutoRefreshTime = time + AutoRefreshInterval;
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Handle manual or automatic refresh triggers using the candle close time.
		CheckManualRefresh(candle.CloseTime);
		CheckAutoRefresh(candle.CloseTime);

		// Synchronize the moving average length if the parameter changed on the fly.
		if (_sma.Length != SmaLength)
		{
			_sma.Length = SmaLength;
			_sma.Reset();
			this.LogInfo($"[{RefreshCommandName}] SMA length changed to {SmaLength}. Indicator state reset.");
			return;
		}

		if (!_sma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Volume <= 0)
			return;

		// Simple SMA trend-following logic that keeps a single position open.
		if (candle.ClosePrice > smaValue)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
			}

			if (Position == 0)
			{
				BuyMarket(Volume);
			}
		}
		else if (candle.ClosePrice < smaValue)
		{
			if (Position > 0)
			{
				SellMarket(Position);
			}

			if (Position == 0)
			{
				SellMarket(Volume);
			}
		}
	}

	private void CheckManualRefresh(DateTimeOffset currentTime)
	{
		if (ManualRefreshRequest)
		{
			if (!_manualRefreshArmed)
			{
				PerformRefresh(currentTime, "manual request");
				ManualRefreshRequest = false;
				_manualRefreshArmed = true;
			}
		}
		else
		{
			_manualRefreshArmed = false;
		}
	}

	private void CheckAutoRefresh(DateTimeOffset currentTime)
	{
		if (!AutoRefreshEnabled)
		{
			_nextAutoRefreshTime = null;
			return;
		}

		if (_nextAutoRefreshTime is null)
			_nextAutoRefreshTime = currentTime + AutoRefreshInterval;

		if (currentTime < _nextAutoRefreshTime.Value)
			return;

		PerformRefresh(currentTime, "automatic schedule");
		_nextAutoRefreshTime = currentTime + AutoRefreshInterval;
	}

	private void PerformRefresh(DateTimeOffset time, string reason)
	{
		// Reset the moving average so that future candles rebuild the indicator state.
		_sma.Reset();

		this.LogInfo($"[{RefreshCommandName}] {RefreshCommandText} triggered by {reason} at {time:O}. TextColor={TextColorName}, BackgroundColor={BackgroundColorName}.");
	}
}

