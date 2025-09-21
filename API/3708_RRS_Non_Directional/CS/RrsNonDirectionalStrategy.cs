using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translation of the MetaTrader "RRS Non-Directional" expert advisor.
/// The strategy recreates the randomised entry modes, virtual stop/target logic and trailing management using StockSharp's netting model.
/// </summary>
public class RrsNonDirectionalStrategy : Strategy
{
	private readonly StrategyParam<RrsTradingMode> _tradingMode;
	private readonly StrategyParam<bool> _allowNewTrades;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<RrsStopMode> _stopMode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<RrsTakeMode> _takeMode;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<RrsTrailingMode> _trailingMode;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingGapPoints;
	private readonly StrategyParam<RrsRiskMode> _riskMode;
	private readonly StrategyParam<decimal> _moneyInRisk;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<string> _tradeComment;

	private readonly Random _random = new();

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal _pointSize;
	private decimal _tickValue;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private string _statusMessage = "Initializing";
	private Sides? _lastClosedSide;
	private decimal _previousPosition;

	/// <summary>
	/// Initializes a new instance of the <see cref="RrsNonDirectionalStrategy"/> class.
	/// </summary>
	public RrsNonDirectionalStrategy()
	{
		_tradingMode = Param(nameof(TradingMode), RrsTradingMode.HedgeStyle)
		.SetDisplay("Trading Strategy", "Entry style reproduced from the MT4 extern Trading_Strategy", "General")
		.SetCanOptimize(true);

		_allowNewTrades = Param(nameof(AllowNewTrades), true)
		.SetDisplay("Enable Trading", "Master switch that mirrors the New_Trade extern", "General")
		.SetCanOptimize(false);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Base volume used for market orders", "General")
		.SetCanOptimize(true);

		_stopMode = Param(nameof(StopMode), RrsStopMode.Virtual)
		.SetDisplay("Stop-Loss Type", "Chooses between virtual or classic stop-loss handling", "Risk")
		.SetCanOptimize(false);

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
		.SetDisplay("Stop-Loss (points)", "MetaTrader points converted with the instrument price step", "Risk")
		.SetCanOptimize(true);

		_takeMode = Param(nameof(TakeMode), RrsTakeMode.Virtual)
		.SetDisplay("Take-Profit Type", "Chooses between virtual or classic take-profit handling", "Risk")
		.SetCanOptimize(false);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
		.SetDisplay("Take-Profit (points)", "MetaTrader points converted with the instrument price step", "Risk")
		.SetCanOptimize(true);

		_trailingMode = Param(nameof(TrailingMode), RrsTrailingMode.Virtual)
		.SetDisplay("Trailing Type", "Switch between virtual and classic trailing implementation", "Risk")
		.SetCanOptimize(false);

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 30)
		.SetDisplay("Trailing Start (points)", "Distance in points before the trailing stop activates", "Risk")
		.SetCanOptimize(true);

		_trailingGapPoints = Param(nameof(TrailingGapPoints), 30)
		.SetDisplay("Trailing Gap (points)", "Distance maintained behind the best price once trailing is active", "Risk")
		.SetCanOptimize(true);

		_riskMode = Param(nameof(RiskMode), RrsRiskMode.BalancePercentage)
		.SetDisplay("Risk Mode", "Determines how MoneyInRisk is interpreted", "Risk")
		.SetCanOptimize(false);

		_moneyInRisk = Param(nameof(MoneyInRisk), 5m)
		.SetDisplay("Money In Risk", "Either a percent of balance or an absolute currency amount", "Risk")
		.SetCanOptimize(true);

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 50)
		.SetDisplay("Max Spread (points)", "Maximum allowed spread expressed in MetaTrader points", "Filters")
		.SetCanOptimize(true);

		_slippagePoints = Param(nameof(SlippagePoints), 3)
		.SetDisplay("Slippage (points)", "Displayed for completeness, market exits ignore this value", "Filters")
		.SetCanOptimize(false);

		_tradeComment = Param(nameof(TradeComment), "RRS")
		.SetDisplay("Trade Comment", "Tag attached to every market order", "General")
		.SetCanOptimize(false);
	}

	/// <summary>
	/// Selected entry mode from the original EA.
	/// </summary>
	public RrsTradingMode TradingMode
	{
		get => _tradingMode.Value;
		set => _tradingMode.Value = value;
	}

	/// <summary>
	/// Enables or disables new market entries.
	/// </summary>
	public bool AllowNewTrades
	{
		get => _allowNewTrades.Value;
		set => _allowNewTrades.Value = value;
	}

	/// <summary>
	/// Base volume for market orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss handling mode.
	/// </summary>
	public RrsStopMode StopMode
	{
		get => _stopMode.Value;
		set => _stopMode.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit handling mode.
	/// </summary>
	public RrsTakeMode TakeMode
	{
		get => _takeMode.Value;
		set => _takeMode.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing management mode.
	/// </summary>
	public RrsTrailingMode TrailingMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Trailing activation distance in points.
	/// </summary>
	public int TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Trailing gap maintained once armed.
	/// </summary>
	public int TrailingGapPoints
	{
		get => _trailingGapPoints.Value;
		set => _trailingGapPoints.Value = value;
	}

	/// <summary>
	/// Risk management interpretation for <see cref="MoneyInRisk"/>.
	/// </summary>
	public RrsRiskMode RiskMode
	{
		get => _riskMode.Value;
		set => _riskMode.Value = value;
	}

	/// <summary>
	/// Risk threshold expressed either as percent of balance or absolute currency amount.
	/// </summary>
	public decimal MoneyInRisk
	{
		get => _moneyInRisk.Value;
		set => _moneyInRisk.Value = value;
	}

	/// <summary>
	/// Maximum accepted spread before suppressing new trades.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Informational slippage setting shown on the UI.
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Comment passed to every generated order.
	/// </summary>
	public string TradeComment
	{
		get => _tradeComment.Value;
		set => _tradeComment.Value = value;
	}

	/// <summary>
	/// Human readable status updated during processing.
	/// </summary>
	public string StatusMessage => _statusMessage;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_pointSize = 0m;
		_tickValue = 0m;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_statusMessage = "Reset";
		_lastClosedSide = null;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
		_pointSize = 0.0001m;

		_tickValue = Security?.StepPrice ?? 0m;
		if (_tickValue <= 0m)
		_tickValue = 1m;

		if (TradingMode == RrsTradingMode.AutoSwap)
		InitializeAutoSwap();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		StartProtection();
	}

	private void InitializeAutoSwap()
	{
		TradingMode = RrsTradingMode.HedgeStyle;
		LogInfo("AutoSwap mode falls back to HedgeStyle because swap rates are not available through Level1 data.");
	}

	private void ProcessLevel1(Level1ChangeMessage change)
	{
		var fields = change.Changes;

		if (fields.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
		_lastBid = bid;

		if (fields.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
		_lastAsk = ask;

		if (_lastBid is null || _lastAsk is null)
		return;

		ManageOpenPosition();
		ApplyRiskCut();
		TryOpenTrade();
	}

	private void ManageOpenPosition()
	{
		var bid = _lastBid;
		var ask = _lastAsk;
		if (bid is null || ask is null)
		return;

		if (Position > 0m)
		{
			if (_longStopPrice is not null && bid <= _longStopPrice)
			ClosePosition("Stop-loss hit");
			else if (_longTakePrice is not null && bid >= _longTakePrice)
			ClosePosition("Take-profit hit");
			else
			UpdateTrailingForLong(bid.Value);
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is not null && ask >= _shortStopPrice)
			ClosePosition("Stop-loss hit");
			else if (_shortTakePrice is not null && ask <= _shortTakePrice)
			ClosePosition("Take-profit hit");
			else
			UpdateTrailingForShort(ask.Value);
		}
	}

	private void UpdateTrailingForLong(decimal bid)
	{
		if (TrailingMode == RrsTrailingMode.Disabled)
		return;

		var activation = GetPriceDistance(TrailingStartPoints);
		var gap = GetPriceDistance(TrailingGapPoints);
		if (activation <= 0m || gap <= 0m)
		return;

		var entryPrice = PositionPrice ?? 0m;
		if (entryPrice <= 0m)
		return;

		if (bid >= entryPrice + activation)
		{
			var candidate = bid - gap;
			if (_longTrailingStop is null || candidate > _longTrailingStop)
			{
				_longTrailingStop = candidate;
				_longStopPrice = candidate;
			}
		}

		if (_longTrailingStop is not null && bid <= _longTrailingStop)
		ClosePosition("Trailing stop");
	}

	private void UpdateTrailingForShort(decimal ask)
	{
		if (TrailingMode == RrsTrailingMode.Disabled)
		return;

		var activation = GetPriceDistance(TrailingStartPoints);
		var gap = GetPriceDistance(TrailingGapPoints);
		if (activation <= 0m || gap <= 0m)
		return;

		var entryPrice = PositionPrice ?? 0m;
		if (entryPrice <= 0m)
		return;

		if (ask <= entryPrice - activation)
		{
			var candidate = ask + gap;
			if (_shortTrailingStop is null || candidate < _shortTrailingStop)
			{
				_shortTrailingStop = candidate;
				_shortStopPrice = candidate;
			}
		}

		if (_shortTrailingStop is not null && ask >= _shortTrailingStop)
		ClosePosition("Trailing stop");
	}

	private void ApplyRiskCut()
	{
		if (Position == 0m)
		return;

		var bid = _lastBid;
		var ask = _lastAsk;
		if (bid is null || ask is null)
		return;

		var unrealized = CalculateUnrealizedPnL(bid.Value, ask.Value);
		var threshold = GetRiskThreshold();
		if (unrealized <= threshold)
		{
			ClosePosition("Risk control");
			_statusMessage = "Risk stop activated";
		}
	}

	private decimal CalculateUnrealizedPnL(decimal bid, decimal ask)
	{
		if (Position == 0m)
		return 0m;

		var entryPrice = PositionPrice ?? 0m;
		if (entryPrice <= 0m)
		return 0m;

		var currentPrice = Position > 0m ? bid : ask;
		var priceDiff = currentPrice - entryPrice;
		var direction = Position > 0m ? 1m : -1m;
		if (_pointSize <= 0m || _tickValue <= 0m)
		return priceDiff * direction * Position;

		var steps = priceDiff / _pointSize;
		return steps * _tickValue * Position * direction;
	}

	private decimal GetRiskThreshold()
	{
		var balance = GetPortfolioValue();
		if (RiskMode == RrsRiskMode.BalancePercentage)
		return -Math.Abs(balance * (MoneyInRisk / 100m));

		return -Math.Abs(MoneyInRisk);
	}

	private void TryOpenTrade()
	{
		if (!AllowNewTrades)
		{
			_statusMessage = "Trading disabled";
			return;
		}

		if (_lastBid is null || _lastAsk is null)
		return;

		var spread = _lastAsk.Value - _lastBid.Value;
		var spreadPoints = _pointSize > 0m ? spread / _pointSize : spread;
		if (spreadPoints > MaxSpreadPoints)
		{
			_statusMessage = "Spread filter";
			return;
		}

		if (Position > 0m)
		{
			_statusMessage = "Long position active";
			return;
		}

		if (Position < 0m)
		{
			_statusMessage = "Short position active";
			return;
		}

		switch (TradingMode)
		{
			case RrsTradingMode.HedgeStyle:
			case RrsTradingMode.AutoSwap:
				OpenHedgeReplacement();
				break;
			case RrsTradingMode.BuyOrder:
				OpenLong();
				break;
			case RrsTradingMode.SellOrder:
				OpenShort();
				break;
			case RrsTradingMode.BuySellRandom:
				if (_random.Next(0, 2) == 0)
				OpenLong();
				else
				OpenShort();
				break;
			case RrsTradingMode.BuySell:
				if (_lastClosedSide == Sides.Sell || _lastClosedSide is null)
				OpenLong();
				else
				OpenShort();
				break;
		}
	}

	private void OpenHedgeReplacement()
	{
		if (_lastClosedSide == Sides.Buy)
		{
			OpenShort();
			return;
		}

		OpenLong();
	}

	private void OpenLong()
	{
		var order = BuyMarket(TradeVolume, comment: TradeComment);
		if (order != null)
		{
			_statusMessage = "Opened long";
			PrepareProtectionForLong();
		}
	}

	private void OpenShort()
	{
		var order = SellMarket(TradeVolume, comment: TradeComment);
		if (order != null)
		{
			_statusMessage = "Opened short";
			PrepareProtectionForShort();
		}
	}

	private void PrepareProtectionForLong()
	{
		var entryPrice = PositionPrice ?? Security?.LastPrice ?? 0m;
		if (entryPrice <= 0m)
		return;

		var stopDistance = GetPriceDistance(StopLossPoints);
		var takeDistance = GetPriceDistance(TakeProfitPoints);

		_longStopPrice = StopMode == RrsStopMode.Disabled || stopDistance <= 0m ? null : entryPrice - stopDistance;
		_longTakePrice = TakeMode == RrsTakeMode.Disabled || takeDistance <= 0m ? null : entryPrice + takeDistance;
		_longTrailingStop = null;
	}

	private void PrepareProtectionForShort()
	{
		var entryPrice = PositionPrice ?? Security?.LastPrice ?? 0m;
		if (entryPrice <= 0m)
		return;

		var stopDistance = GetPriceDistance(StopLossPoints);
		var takeDistance = GetPriceDistance(TakeProfitPoints);

		_shortStopPrice = StopMode == RrsStopMode.Disabled || stopDistance <= 0m ? null : entryPrice + stopDistance;
		_shortTakePrice = TakeMode == RrsTakeMode.Disabled || takeDistance <= 0m ? null : entryPrice - takeDistance;
		_shortTrailingStop = null;
	}

	private decimal GetPriceDistance(int points)
	{
		if (points <= 0)
		return 0m;

		var point = _pointSize > 0m ? _pointSize : 0.0001m;
		return points * point;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (_previousPosition > 0m && Position == 0m)
		_lastClosedSide = Sides.Buy;
		else if (_previousPosition < 0m && Position == 0m)
		_lastClosedSide = Sides.Sell;

		_previousPosition = Position;

		if (Position == 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			_longTakePrice = null;
			_shortTakePrice = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue ?? 0m;
		if (current > 0m)
		return current;

		var begin = Portfolio?.BeginValue ?? 0m;
		return begin > 0m ? begin : current;
	}
}

/// <summary>
/// Entry modes reproduced from the MT4 enumerations.
/// </summary>
public enum RrsTradingMode
{
	HedgeStyle,
	BuySellRandom,
	BuySell,
	AutoSwap,
	BuyOrder,
	SellOrder,
}

/// <summary>
/// Stop-loss handling options.
/// </summary>
public enum RrsStopMode
{
	Disabled,
	Virtual,
	Classic,
}

/// <summary>
/// Take-profit handling options.
/// </summary>
public enum RrsTakeMode
{
	Disabled,
	Virtual,
	Classic,
}

/// <summary>
/// Trailing management options.
/// </summary>
public enum RrsTrailingMode
{
	Disabled,
	Virtual,
	Classic,
}

/// <summary>
/// Interpretation modes for the risk threshold.
/// </summary>
public enum RrsRiskMode
{
	BalancePercentage,
	FixedMoney,
}
