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
/// Translation of the MetaTrader "RRS Non-Directional" expert advisor.
/// The strategy recreates the randomised entry modes, virtual stop/target logic and trailing management using StockSharp's netting model.
/// </summary>
public class RrsNonDirectionalStrategy : Strategy
{
	private readonly StrategyParam<RrsTradingModes> _tradingMode;
	private readonly StrategyParam<bool> _allowNewTrades;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<RrsStopModes> _stopMode;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<RrsTakeModes> _takeMode;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<RrsTrailingModes> _trailingMode;
	private readonly StrategyParam<int> _trailingStartPoints;
	private readonly StrategyParam<int> _trailingGapPoints;
	private readonly StrategyParam<RrsRiskModes> _riskMode;
	private readonly StrategyParam<decimal> _moneyInRisk;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<string> _tradeComment;
	private readonly StrategyParam<DataType> _candleType;

	private int _tradeCounter;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal _pointSize;
	private decimal _tickValue;
	private decimal _entryPrice;
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
		_tradingMode = Param(nameof(RrsTradingMode), RrsTradingModes.HedgeStyle)
		.SetDisplay("Trading Strategy", "Entry style reproduced from the MT4 extern Trading_Strategy", "General")
		;

		_allowNewTrades = Param(nameof(AllowNewTrades), true)
		.SetDisplay("Enable Trading", "Master switch that mirrors the New_Trade extern", "General")
		;

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Trade Volume", "Base volume used for market orders", "General")
		;

		_stopMode = Param(nameof(StopMode), RrsStopModes.Virtual)
		.SetDisplay("Stop-Loss Type", "Chooses between virtual or classic stop-loss handling", "Risk")
		;

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
		.SetDisplay("Stop-Loss (points)", "MetaTrader points converted with the instrument price step", "Risk")
		;

		_takeMode = Param(nameof(TakeMode), RrsTakeModes.Virtual)
		.SetDisplay("Take-Profit Type", "Chooses between virtual or classic take-profit handling", "Risk")
		;

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
		.SetDisplay("Take-Profit (points)", "MetaTrader points converted with the instrument price step", "Risk")
		;

		_trailingMode = Param(nameof(TrailingMode), RrsTrailingModes.Virtual)
		.SetDisplay("Trailing Type", "Switch between virtual and classic trailing implementation", "Risk")
		;

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 30)
		.SetDisplay("Trailing Start (points)", "Distance in points before the trailing stop activates", "Risk")
		;

		_trailingGapPoints = Param(nameof(TrailingGapPoints), 30)
		.SetDisplay("Trailing Gap (points)", "Distance maintained behind the best price once trailing is active", "Risk")
		;

		_riskMode = Param(nameof(RiskMode), RrsRiskModes.BalancePercentage)
		.SetDisplay("Risk Mode", "Determines how MoneyInRisk is interpreted", "Risk")
		;

		_moneyInRisk = Param(nameof(MoneyInRisk), 5m)
		.SetDisplay("Money In Risk", "Either a percent of balance or an absolute currency amount", "Risk")
		;

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 50)
		.SetDisplay("Max Spread (points)", "Maximum allowed spread expressed in MetaTrader points", "Filters")
		;

		_slippagePoints = Param(nameof(SlippagePoints), 3)
		.SetDisplay("Slippage (points)", "Displayed for completeness, market exits ignore this value", "Filters")
		;

		_tradeComment = Param(nameof(TradeComment), "RRS")
		.SetDisplay("Trade Comment", "Tag attached to every market order", "General")
		;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for price updates", "General")
		;
	}

	/// <summary>
	/// Selected entry mode from the original EA.
	/// </summary>
	public RrsTradingModes RrsTradingMode
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
	public RrsStopModes StopMode
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
	public RrsTakeModes TakeMode
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
	public RrsTrailingModes TrailingMode
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
	public RrsRiskModes RiskMode
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
	/// Candle type for price updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Human readable status updated during processing.
	/// </summary>
	public string StatusMessage => _statusMessage;

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tradeCounter = 0;
		_lastBid = null;
		_lastAsk = null;
		_pointSize = 0m;
		_tickValue = 0m;
		_entryPrice = 0m;
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		Volume = TradeVolume;

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
		_pointSize = 0.0001m;

		_tickValue = GetSecurityValue<decimal?>(Level1Fields.StepPrice) ?? 0m;
		if (_tickValue <= 0m)
		_tickValue = 1m;

		if (RrsTradingMode == RrsTradingModes.AutoSwap)
		InitializeAutoSwap();

		SubscribeCandles(CandleType)
		.Bind(ProcessCandle)
		.Start();
	}

	private void InitializeAutoSwap()
	{
		RrsTradingMode = RrsTradingModes.HedgeStyle;
		LogInfo("AutoSwap mode falls back to HedgeStyle because swap rates are not available through Level1 data.");
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastBid = candle.ClosePrice;
		_lastAsk = candle.ClosePrice;

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
			ClosePositionManual("Stop-loss hit");
			else if (_longTakePrice is not null && bid >= _longTakePrice)
			ClosePositionManual("Take-profit hit");
			else
			UpdateTrailingForLong(bid.Value);
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is not null && ask >= _shortStopPrice)
			ClosePositionManual("Stop-loss hit");
			else if (_shortTakePrice is not null && ask <= _shortTakePrice)
			ClosePositionManual("Take-profit hit");
			else
			UpdateTrailingForShort(ask.Value);
		}
	}

	private void UpdateTrailingForLong(decimal bid)
	{
		if (TrailingMode == RrsTrailingModes.Disabled)
		return;

		var activation = GetPriceDistance(TrailingStartPoints);
		var gap = GetPriceDistance(TrailingGapPoints);
		if (activation <= 0m || gap <= 0m)
		return;

		var entryPrice = _entryPrice;
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
		ClosePositionManual("Trailing stop");
	}

	private void UpdateTrailingForShort(decimal ask)
	{
		if (TrailingMode == RrsTrailingModes.Disabled)
		return;

		var activation = GetPriceDistance(TrailingStartPoints);
		var gap = GetPriceDistance(TrailingGapPoints);
		if (activation <= 0m || gap <= 0m)
		return;

		var entryPrice = _entryPrice;
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
		ClosePositionManual("Trailing stop");
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
			ClosePositionManual("Risk control");
			_statusMessage = "Risk stop activated";
		}
	}

	private decimal CalculateUnrealizedPnL(decimal bid, decimal ask)
	{
		if (Position == 0m)
		return 0m;

		var entryPrice = _entryPrice;
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
		if (RiskMode == RrsRiskModes.BalancePercentage)
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

		switch (RrsTradingMode)
		{
			case RrsTradingModes.HedgeStyle:
			case RrsTradingModes.AutoSwap:
				OpenHedgeReplacement();
				break;
			case RrsTradingModes.BuyOrder:
				OpenLong();
				break;
			case RrsTradingModes.SellOrder:
				OpenShort();
				break;
			case RrsTradingModes.BuySellRandom:
				if (_tradeCounter++ % 2 == 0)
				OpenLong();
				else
				OpenShort();
				break;
			case RrsTradingModes.BuySell:
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
		var order = BuyMarket(TradeVolume);
		if (order != null)
		{
			_statusMessage = "Opened long";
			PrepareProtectionForLong();
		}
	}

	private void OpenShort()
	{
		var order = SellMarket(TradeVolume);
		if (order != null)
		{
			_statusMessage = "Opened short";
			PrepareProtectionForShort();
		}
	}

	private void PrepareProtectionForLong()
	{
		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
		return;

		var stopDistance = GetPriceDistance(StopLossPoints);
		var takeDistance = GetPriceDistance(TakeProfitPoints);

		_longStopPrice = StopMode == RrsStopModes.Disabled || stopDistance <= 0m ? null : entryPrice - stopDistance;
		_longTakePrice = TakeMode == RrsTakeModes.Disabled || takeDistance <= 0m ? null : entryPrice + takeDistance;
		_longTrailingStop = null;
	}

	private void PrepareProtectionForShort()
	{
		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
		return;

		var stopDistance = GetPriceDistance(StopLossPoints);
		var takeDistance = GetPriceDistance(TakeProfitPoints);

		_shortStopPrice = StopMode == RrsStopModes.Disabled || stopDistance <= 0m ? null : entryPrice + stopDistance;
		_shortTakePrice = TakeMode == RrsTakeModes.Disabled || takeDistance <= 0m ? null : entryPrice - takeDistance;
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
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

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

	private void ClosePositionManual(string reason)
	{
		if (Position > 0m)
			SellMarket(Math.Abs(Position));
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		_statusMessage = reason;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;

		if (Position == 0m)
			_entryPrice = 0m;
	}

	private decimal GetPortfolioValue()
	{
		var current = Portfolio?.CurrentValue ?? 0m;
		if (current > 0m)
		return current;

		var begin = Portfolio?.BeginValue ?? 0m;
		return begin > 0m ? begin : current;
	}

	public enum RrsTradingModes
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
	public enum RrsStopModes
	{
		Disabled,
		Virtual,
		Classic,
	}

	/// <summary>
	/// Take-profit handling options.
	/// </summary>
	public enum RrsTakeModes
	{
		Disabled,
		Virtual,
		Classic,
	}

	/// <summary>
	/// Trailing management options.
	/// </summary>
	public enum RrsTrailingModes
	{
		Disabled,
		Virtual,
		Classic,
	}

	/// <summary>
	/// Interpretation modes for the risk threshold.
	/// </summary>
	public enum RrsRiskModes
	{
		BalancePercentage,
		FixedMoney,
	}
}
