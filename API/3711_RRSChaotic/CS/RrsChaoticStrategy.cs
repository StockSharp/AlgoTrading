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

	private readonly Random _random = new(Environment.TickCount);

	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private decimal _initialEquity;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used to drive random entries.", "General");

		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetDisplay("Minimum Volume", "Lower bound for the randomly generated order volume.", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 0.5m)
			.SetDisplay("Maximum Volume", "Upper bound for the randomly generated order volume.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetDisplay("Take Profit", "Distance in points for the optional take-profit target.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
			.SetDisplay("Stop Loss", "Distance in points for the protective stop-loss.", "Risk");

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 10)
			.SetDisplay("Max Open Trades", "Maximum net volume measured in volume steps that may stay open.", "Trading");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 100)
			.SetDisplay("Max Spread", "Maximum allowed spread in points before new entries are blocked.", "Trading");

		_slippagePoints = Param(nameof(SlippagePoints), 3)
			.SetDisplay("Slippage", "Slippage tolerance in points (informational only).", "Trading")
			.SetCanOptimize(false);

		_riskMode = Param(nameof(RiskModes), RiskModes.BalancePercentage)
			.SetDisplay("Risk Mode", "Choose between fixed cash or balance percentage drawdown control.", "Risk");

		_riskValue = Param(nameof(RiskValue), 5m)
			.SetDisplay("Risk Value", "Either percentage of equity or fixed cash to risk before flattening.", "Risk");

		_tradeComment = Param(nameof(TradeComment), "RRS")
			.SetDisplay("Trade Comment", "Tag attached to generated orders for traceability.", "General")
			.SetCanOptimize(false);
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

		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_initialEquity = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
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
		if (Position > 0m && trade.OrderDirection == Sides.Buy)
		{
			_longStopPrice = CalculateStopPrice(true, trade.Price);
			_longTakePrice = CalculateTakePrice(true, trade.Price);
			_shortStopPrice = null;
			_shortTakePrice = null;
		}
		else if (Position < 0m && trade.OrderDirection == Sides.Sell)
		{
			_shortStopPrice = CalculateStopPrice(false, trade.Price);
			_shortTakePrice = CalculateTakePrice(false, trade.Price);
			_longStopPrice = null;
			_longTakePrice = null;
		}

		if (Position == 0m)
		{
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ApplyExitRules(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsSpreadAcceptable())
			return;

		var volume = GenerateRandomVolume();
		if (volume <= 0m)
			return;

		var randomValue = _random.Next(0, 11);

		if ((randomValue == 6 || randomValue == 9) && Position >= 0m)
		{
			TryOpenLong(volume);
		}
		else if ((randomValue == 3 || randomValue == 8) && Position <= 0m)
		{
			TryOpenShort(volume);
		}
	}

	private void TryOpenLong(decimal volume)
	{
		if (!CanIncreaseExposure(volume))
			return;

		BuyMarket(volume, comment: TradeComment);
	}

	private void TryOpenShort(decimal volume)
	{
		if (!CanIncreaseExposure(volume))
			return;

		SellMarket(volume, comment: TradeComment);
	}

	private bool CanIncreaseExposure(decimal volume)
	{
		var step = GetVolumeStep();
		if (step <= 0m)
			step = 1m;

		var currentUnits = Math.Abs(Position) / step;
		var additionalUnits = volume / step;

		return currentUnits + additionalUnits <= MaxOpenTrades;
	}

	private void ApplyExitRules(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longTakePrice.HasValue && candle.HighPrice >= _longTakePrice.Value)
			{
				SellMarket(Position, comment: TradeComment + " TP");
				return;
			}

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position, comment: TradeComment + " SL");
			}
		}
		else if (Position < 0m)
		{
			if (_shortTakePrice.HasValue && candle.LowPrice <= _shortTakePrice.Value)
			{
				BuyMarket(-Position, comment: TradeComment + " TP");
				return;
			}

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(-Position, comment: TradeComment + " SL");
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
			CancelActiveOrders();
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

	private decimal GenerateRandomVolume()
	{
		var min = Math.Max(0m, MinVolume);
		var max = Math.Max(min, MaxVolume);

		var sample = (decimal)_random.NextDouble();
		var volume = min + (max - min) * sample;

		var step = GetVolumeStep();
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			if (steps <= 0m)
				steps = 1m;
			volume = steps * step;
		}

		if (volume < min)
			volume = min;
		else if (volume > max)
			volume = max;

		return volume;
	}

	private decimal GetVolumeStep()
	{
		var security = Security;
		if (security == null)
			return 1m;

		if (security.VolumeStep > 0m)
			return security.VolumeStep;

		if (security.MinVolume > 0m)
			return security.MinVolume;

		return 1m;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		if (security.PriceStep > 0m)
			return security.PriceStep;

		if (security.MinStep > 0m)
			return security.MinStep;

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

	private bool IsSpreadAcceptable()
	{
		if (MaxSpreadPoints <= 0)
			return true;

		var security = Security;
		var bid = security?.BestBid?.Price ?? 0m;
		var ask = security?.BestAsk?.Price ?? 0m;

		if (bid <= 0m || ask <= 0m)
			return true;

		var step = GetPriceStep();
		if (step <= 0m)
			return true;

		var spread = (ask - bid) / step;
		return spread <= MaxSpreadPoints;
	}

	private decimal GetUnrealizedPnL(decimal currentPrice)
	{
		if (Position == 0m)
			return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
			return 0m;

		var diff = currentPrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
			return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
			return portfolio.BeginValue;

		return _initialEquity <= 0m ? 10000m : _initialEquity;
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position, comment: TradeComment + " Risk");
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position, comment: TradeComment + " Risk");
		}
	}
}

