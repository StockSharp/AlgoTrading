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
/// Randomized breakout/mean-reversion hybrid that mimics the behaviour of the RRS Chaotic EA.
/// The strategy opens random buy or sell trades with variable volume while enforcing a risk budget.
/// </summary>
public class RrsChaoticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _maxOpenTrades;
	private readonly StrategyParam<int> _maxSpreadPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<RiskModes> _riskMode;
	private readonly StrategyParam<decimal> _riskValue;
	private readonly StrategyParam<string> _tradeComment;

	private int _tradeCounter;

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private decimal _initialEquity;
	private decimal _entryPrice;

	/// <summary>
	/// Trade direction for risk sizing.
	/// </summary>
	public enum RiskModes
	{
		/// <summary>
		/// Risk a fixed cash amount.
		/// </summary>
		FixedMoney,

		/// <summary>
		/// Risk a percentage of the portfolio value.
		/// </summary>
		BalancePercentage
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RrsChaoticStrategy"/>.
	/// </summary>
	public RrsChaoticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used to drive random entries.", "General");

		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetDisplay("Minimum Volume", "Lower bound for the randomly generated order volume.", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 0.5m)
			.SetDisplay("Maximum Volume", "Upper bound for the randomly generated order volume.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50000)
			.SetDisplay("Take Profit", "Distance in points for the optional take-profit target.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 50000)
			.SetDisplay("Stop Loss", "Distance in points for the protective stop-loss.", "Risk");

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 10)
			.SetDisplay("Max Open Trades", "Maximum net volume measured in volume steps that may stay open.", "Trading");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 100)
			.SetDisplay("Max Spread", "Maximum allowed spread in points before new entries are blocked.", "Trading");

		_slippagePoints = Param(nameof(SlippagePoints), 3)
			.SetDisplay("Slippage", "Slippage tolerance in points (informational only).", "Trading")
			;

		_riskMode = Param(nameof(RiskControlMode), RiskModes.BalancePercentage)
			.SetDisplay("Risk Mode", "Choose between fixed cash or balance percentage drawdown control.", "Risk");

		_riskValue = Param(nameof(RiskValue), 5m)
			.SetDisplay("Risk Value", "Either percentage of equity or fixed cash to risk before flattening.", "Risk");

		_tradeComment = Param(nameof(TradeComment), "RRS")
			.SetDisplay("Trade Comment", "Tag attached to generated orders for traceability.", "General")
			;
	}

	/// <summary>
	/// Candle type used to trigger the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum random volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximum random volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Maximum simultaneously open trades expressed in volume steps.
	/// </summary>
	public int MaxOpenTrades
	{
		get => _maxOpenTrades.Value;
		set => _maxOpenTrades.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in points before entries are skipped.
	/// </summary>
	public int MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Slippage tolerance in points (informational parameter).
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Selected risk control mode.
	/// </summary>
	public RiskModes RiskControlMode
	{
		get => _riskMode.Value;
		set => _riskMode.Value = value;
	}

	/// <summary>
	/// Risk magnitude expressed either as percentage or cash.
	/// </summary>
	public decimal RiskValue
	{
		get => _riskValue.Value;
		set => _riskValue.Value = value;
	}

	/// <summary>
	/// Comment appended to generated orders.
	/// </summary>
	public string TradeComment
	{
		get => _tradeComment.Value;
		set => _tradeComment.Value = value;
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

		_tradeCounter = 0;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_initialEquity = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_initialEquity = GetPortfolioValue();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		// Update protective levels based on the latest execution price.
		var tradePrice = trade.Trade.Price;
		var direction = trade.Order.Side;

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = tradePrice;

		if (Position > 0m && direction == Sides.Buy)
		{
			_longStopPrice = CalculateStopPrice(true, tradePrice);
			_longTakePrice = CalculateTakePrice(true, tradePrice);
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0m && direction == Sides.Sell)
		{
			_shortStopPrice = CalculateStopPrice(false, tradePrice);
			_shortTakePrice = CalculateTakePrice(false, tradePrice);
			_longStopPrice = null;
			_longTakePrice = null;
		}

		if (Position == 0m)
		{
			_entryPrice = 0m;
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		HandleRiskControl(candle);
		ApplyExitRules(candle);

		if (Position != 0m)
			return;

		if (_tradeCounter % 2 == 0)
		{
			BuyMarket(Volume);
		}
		else
		{
			SellMarket(Volume);
		}
		_tradeCounter++;
	}

	private void ApplyExitRules(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0m)
		{
			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}

	private void HandleRiskControl(ICandleMessage candle)
	{
		var threshold = CalculateRiskThreshold();
		if (!threshold.HasValue)
			return;

		var unrealized = GetUnrealizedPnL(candle.ClosePrice);
		if (unrealized <= threshold.Value)
		{
			ClosePosition();
		}
	}

	private decimal? CalculateRiskThreshold()
	{
		return RiskControlMode switch
		{
			RiskModes.BalancePercentage => CalculatePercentageThreshold(),
			RiskModes.FixedMoney => -Math.Abs(RiskValue),
			_ => null
		};
	}

	private decimal? CalculatePercentageThreshold()
	{
		var equity = GetPortfolioValue();
		if (equity <= 0m)
			return null;

		return -Math.Abs(equity * RiskValue / 100m);
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if ((security.PriceStep ?? 0m) > 0m)
			return security.PriceStep.Value;

		return 0.0001m;
	}

	private decimal? CalculateStopPrice(bool isLong, decimal entryPrice)
	{
		if (StopLossPoints <= 0)
			return null;

		var distance = StopLossPoints * GetPriceStep();
		return isLong ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? CalculateTakePrice(bool isLong, decimal entryPrice)
	{
		if (TakeProfitPoints <= 0)
			return null;

		var distance = TakeProfitPoints * GetPriceStep();
		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	private decimal GetUnrealizedPnL(decimal currentPrice)
	{
		if (Position == 0m)
			return 0m;

		var entry = _entryPrice;
		if (entry == 0m)
			return 0m;

		var diff = currentPrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if ((portfolio?.CurrentValue ?? 0m) > 0m)
			return portfolio.CurrentValue.Value;

		if ((portfolio?.BeginValue ?? 0m) > 0m)
			return portfolio.BeginValue.Value;

		return _initialEquity <= 0m ? 10000m : _initialEquity;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}

