
using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Defines which positions should be processed by the strategy.
/// </summary>
public enum CloseAgentMode
{
	/// <summary>
	/// Only process positions opened manually or by other strategies.
	/// </summary>
	Manual,

	/// <summary>
	/// Only process positions opened by this strategy instance.
	/// </summary>
	Auto,

	/// <summary>
	/// Process all positions regardless of origin.
	/// </summary>
	Both,
}

/// <summary>
/// Defines how indicator data should be sampled for signal evaluation.
/// </summary>
public enum CloseAgentOperationMode
{
	/// <summary>
	/// Evaluate signals using the latest forming candle values.
	/// </summary>
	LiveBar,

	/// <summary>
	/// Evaluate signals using only closed candles.
	/// </summary>
	NewBar,
}

/// <summary>
/// Closes open positions when price stretches beyond Bollinger Bands and RSI reaches extreme values.
/// Implements the logic of the original CloseAgent MQL tool.
/// </summary>
public class CloseAgentStrategy : Strategy
{
	private const int RsiLength = 13;
	private const int BollingerLength = 21;
	private const decimal RsiOverbought = 70m;
	private const decimal RsiOversold = 30m;

	private static readonly PropertyInfo StrategyIdProperty = typeof(Position).GetProperty("StrategyId");

	private readonly StrategyParam<CloseAgentMode> _closeMode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<CloseAgentOperationMode> _operationMode;
	private readonly StrategyParam<decimal> _closeAllTarget;
	private readonly StrategyParam<bool> _enableAlerts;

	private RelativeStrengthIndex _rsi;
	private BollingerBands _bands;
	private readonly Queue<(DateTimeOffset time, decimal close, decimal rsi, decimal upper, decimal lower)> _history = new();
	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _lastProcessedPrice;

	/// <summary>
	/// Determines which positions should be handled by the strategy.
	/// </summary>
	public CloseAgentMode CloseMode
	{
		get => _closeMode.Value;
		set => _closeMode.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate Bollinger Bands and RSI.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Chooses whether signals rely on live or closed candle data.
	/// </summary>
	public CloseAgentOperationMode OperationMode
	{
		get => _operationMode.Value;
		set => _operationMode.Value = value;
	}

	/// <summary>
	/// Profit target that triggers a full position liquidation.
	/// </summary>
	public decimal CloseAllTarget
	{
		get => _closeAllTarget.Value;
		set => _closeAllTarget.Value = value;
	}

	/// <summary>
	/// Enables informational alerts when trades are closed.
	/// </summary>
	public bool EnableAlerts
	{
		get => _enableAlerts.Value;
		set => _enableAlerts.Value = value;
	}

	/// <summary>
	/// Initializes parameters with defaults inspired by the original MQL script.
	/// </summary>
	public CloseAgentStrategy()
	{
		_closeMode = Param(nameof(CloseMode), CloseAgentMode.Both)
		.SetDisplay("Close Mode", "Choose which positions are eligible for closing", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for indicators", "General");

		_operationMode = Param(nameof(OperationMode), CloseAgentOperationMode.LiveBar)
		.SetDisplay("Operation Mode", "Use forming candles or closed candles for signals", "Signals");

		_closeAllTarget = Param(nameof(CloseAllTarget), 0m)
		.SetDisplay("Close All Target", "Profit level that closes every monitored position", "Risk")
		.SetNotNegative();

		_enableAlerts = Param(nameof(EnableAlerts), true)
		.SetDisplay("Enable Alerts", "Log a message whenever a position is closed", "Notifications");
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

		_rsi = null;
		_bands = null;
		_history.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
		_lastProcessedPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength,
		};

		_bands = new BollingerBands
		{
			Length = BollingerLength,
			Width = 2m,
		};

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, _bands, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue != null)
		_bestBid = Convert.ToDecimal(bidValue);

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue != null)
		_bestAsk = Convert.ToDecimal(askValue);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal middle, decimal upper, decimal lower)
	{
		if (_rsi is null || _bands is null)
		return;

		if (OperationMode == CloseAgentOperationMode.NewBar && candle.State != CandleStates.Finished)
		return;

		if (!_rsi.IsFormed || !_bands.IsFormed)
		return;

		_history.Enqueue((candle.CloseTime, candle.ClosePrice, rsiValue, upper, lower));

		var shift = OperationMode == CloseAgentOperationMode.NewBar ? 1 : 0;
		var maxItems = Math.Max(shift + 2, 3);
		while (_history.Count > maxItems)
		_history.Dequeue();

		if (!TryGetSnapshot(shift, out var snapshot))
		return;

		_lastProcessedPrice = snapshot.close;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var sourcePositions = Portfolio?.Positions;
		if (sourcePositions == null)
		return;

		var positions = new List<Position>();
		foreach (var position in sourcePositions)
		{
			if (position.Security == Security)
			positions.Add(position);
		}

		if (positions.Count == 0)
		return;

		var bidPrice = GetBidPrice(snapshot.close);
		var askPrice = GetAskPrice(snapshot.close);

		if (CloseAllTarget > 0m && PnL >= CloseAllTarget)
		{
			if (EnableAlerts)
			LogInfo($"Closing all positions at {PnL:0.##} profit.");

			foreach (var position in positions)
			{
				if (!ShouldProcessPosition(position))
				continue;

				ClosePosition(position, bidPrice, askPrice, "Total profit target reached");
			}

			return;
		}

		foreach (var position in positions)
		{
			if (!ShouldProcessPosition(position))
			continue;

			var volume = position.CurrentValue;
			if (volume > 0m)
			{
				if (bidPrice > snapshot.upper && snapshot.rsi > RsiOverbought && bidPrice > position.AveragePrice)
				ClosePosition(position, bidPrice, askPrice, "Long exit triggered");
			}
			else if (volume < 0m)
			{
				if (askPrice < snapshot.lower && snapshot.rsi < RsiOversold && askPrice < position.AveragePrice)
				ClosePosition(position, bidPrice, askPrice, "Short exit triggered");
			}
		}
	}

	private bool TryGetSnapshot(int shift, out (DateTimeOffset time, decimal close, decimal rsi, decimal upper, decimal lower) snapshot)
	{
		if (_history.Count <= shift)
		{
			snapshot = default;
			return false;
		}

		var targetIndex = _history.Count - 1 - shift;
		var currentIndex = 0;
		foreach (var item in _history)
		{
			if (currentIndex == targetIndex)
			{
				snapshot = item;
				return true;
			}

			currentIndex++;
		}

		snapshot = default;
		return false;
	}

	private decimal GetBidPrice(decimal fallback)
	{
		return _bestBid > 0m ? _bestBid : fallback;
	}

	private decimal GetAskPrice(decimal fallback)
	{
		return _bestAsk > 0m ? _bestAsk : fallback;
	}

	private bool ShouldProcessPosition(Position position)
	{
		switch (CloseMode)
		{
		case CloseAgentMode.Both:
			return true;
		case CloseAgentMode.Auto:
			{
				var strategyId = TryGetStrategyId(position);
				return !string.IsNullOrEmpty(strategyId) && string.Equals(strategyId, Id, StringComparison.OrdinalIgnoreCase);
			}
		case CloseAgentMode.Manual:
			{
				var strategyId = TryGetStrategyId(position);
				return string.IsNullOrEmpty(strategyId) || !string.Equals(strategyId, Id, StringComparison.OrdinalIgnoreCase);
			}
		default:
			return true;
		}
	}

	private static string TryGetStrategyId(Position position)
	{
		if (StrategyIdProperty == null)
		return null;

		var value = StrategyIdProperty.GetValue(position);
		return value?.ToString();
	}

	private void ClosePosition(Position position, decimal bidPrice, decimal askPrice, string reason)
	{
		var volume = position.CurrentValue;
		var absVolume = Math.Abs(volume);
		if (absVolume <= 0m)
		return;

		var exitPrice = volume > 0m ? bidPrice : askPrice;
		if (exitPrice <= 0m)
		exitPrice = _lastProcessedPrice;

		if (EnableAlerts)
		{
			var entryPrice = position.AveragePrice;
			var profit = volume > 0m
			? (exitPrice - entryPrice) * absVolume
			: (entryPrice - exitPrice) * absVolume;
			LogInfo($"Closing {position.Security?.Id ?? Security?.Id} at {profit:0.##} profit. Reason: {reason}.");
		}

		if (volume > 0m)
		SellMarket(absVolume);
		else
		BuyMarket(absVolume);
	}
}
