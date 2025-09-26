using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "2-Pair Correlation EA" that trades the BTCUSD/ETHUSD spread.
/// The strategy opens a market-neutral pair when the bid prices diverge beyond a threshold
/// and closes both legs once the combined profit target is achieved.
/// Risk control is handled through drawdown monitoring, volatility filtering, and dynamic position sizing.
/// </summary>
public class TwoPairCorrelationStrategy : Strategy
{
	private readonly StrategyParam<Security> _secondSecurityParam;
	private readonly StrategyParam<decimal> _maxDrawdownPercentParam;
	private readonly StrategyParam<decimal> _riskPercentParam;
	private readonly StrategyParam<decimal> _priceDifferenceThresholdParam;
	private readonly StrategyParam<decimal> _minimumTotalProfitParam;
	private readonly StrategyParam<int> _atrPeriodParam;
	private readonly StrategyParam<decimal> _recoveryPercentParam;
	private readonly StrategyParam<int> _stopLossPipsParam;
	private readonly StrategyParam<DataType> _atrCandleTypeParam;

	private AverageTrueRange _primaryAtr;
	private AverageTrueRange _secondaryAtr;

	private decimal _primaryAtrValue;
	private decimal _secondaryAtrValue;

	private decimal? _primaryBid;
	private decimal? _secondaryBid;
	private decimal? _primaryAsk;
	private decimal? _secondaryAsk;

	private decimal _peakEquity;
	private bool _tradingPaused;

	private decimal _primaryPosition;
	private decimal _secondaryPosition;
	private decimal _primaryEntryPrice;
	private decimal _secondaryEntryPrice;

	/// <summary>
	/// Secondary security that forms the hedge leg (ETHUSD in the original expert).
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurityParam.Value;
		set => _secondSecurityParam.Value = value;
	}

	/// <summary>
	/// Maximum drawdown percentage that pauses new entries.
	/// </summary>
	public decimal MaxDrawdownPercent
	{
		get => _maxDrawdownPercentParam.Value;
		set => _maxDrawdownPercentParam.Value = value;
	}

	/// <summary>
	/// Percentage of portfolio equity risked per trade to derive the dynamic volume.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercentParam.Value;
		set => _riskPercentParam.Value = value;
	}

	/// <summary>
	/// Absolute bid price divergence that triggers a new pair entry.
	/// </summary>
	public decimal PriceDifferenceThreshold
	{
		get => _priceDifferenceThresholdParam.Value;
		set => _priceDifferenceThresholdParam.Value = value;
	}

	/// <summary>
	/// Total floating profit in account currency required to close both legs.
	/// </summary>
	public decimal MinimumTotalProfit
	{
		get => _minimumTotalProfitParam.Value;
		set => _minimumTotalProfitParam.Value = value;
	}

	/// <summary>
	/// Number of candles used by the ATR volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriodParam.Value;
		set => _atrPeriodParam.Value = value;
	}

	/// <summary>
	/// Equity recovery percentage required to resume trading after a drawdown pause.
	/// </summary>
	public decimal RecoveryPercent
	{
		get => _recoveryPercentParam.Value;
		set => _recoveryPercentParam.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips used to convert the risk percentage into a position volume.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPipsParam.Value;
		set => _stopLossPipsParam.Value = value;
	}

	/// <summary>
	/// Candle type used to compute the ATR volatility gauge.
	/// </summary>
	public DataType AtrCandleType
	{
		get => _atrCandleTypeParam.Value;
		set => _atrCandleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TwoPairCorrelationStrategy"/> class.
	/// </summary>
	public TwoPairCorrelationStrategy()
	{
		_secondSecurityParam = Param<Security>(nameof(SecondSecurity))
		.SetDisplay("Second Symbol", "Secondary instrument used for the hedge leg", "General")
		.SetRequired();

		_maxDrawdownPercentParam = Param(nameof(MaxDrawdownPercent), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_riskPercentParam = Param(nameof(RiskPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Percentage of equity risked per trade", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_priceDifferenceThresholdParam = Param(nameof(PriceDifferenceThreshold), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Price Difference", "Bid divergence required to open the pair", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(20m, 300m, 20m);

		_minimumTotalProfitParam = Param(nameof(MinimumTotalProfit), 0.30m)
		.SetGreaterThanZero()
		.SetDisplay("Total Profit", "Profit target for closing both legs", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 2m, 0.05m);

		_atrPeriodParam = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Number of candles used by the volatility filter", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_recoveryPercentParam = Param(nameof(RecoveryPercent), 95m)
		.SetDisplay("Recovery %", "Equity recovery required to resume trading", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(80m, 100m, 5m);

		_stopLossPipsParam = Param(nameof(StopLossPips), 50)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss Pips", "Synthetic stop distance used for lot sizing", "Risk");

		_atrCandleTypeParam = Param(nameof(AtrCandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("ATR Candle", "Candle series used for ATR", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, DataType.Level1);

		if (SecondSecurity != null)
			yield return (SecondSecurity, DataType.Level1);

		if (Security != null)
			yield return (Security, AtrCandleType);

		if (SecondSecurity != null)
			yield return (SecondSecurity, AtrCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryAtr = null;
		_secondaryAtr = null;

		_primaryAtrValue = 0m;
		_secondaryAtrValue = 0m;

		_primaryBid = null;
		_secondaryBid = null;
		_primaryAsk = null;
		_secondaryAsk = null;

		_peakEquity = 0m;
		_tradingPaused = false;

		_primaryPosition = 0m;
		_secondaryPosition = 0m;
		_primaryEntryPrice = 0m;
		_secondaryEntryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (SecondSecurity == null)
			throw new InvalidOperationException("Secondary security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		_peakEquity = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;

		_primaryAtr = new AverageTrueRange { Length = AtrPeriod };
		_secondaryAtr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeLevel1()
		.Bind(ProcessPrimaryLevel1)
		.Start();

		SubscribeLevel1(security: SecondSecurity)
		.Bind(ProcessSecondaryLevel1)
		.Start();

		var primaryCandles = SubscribeCandles(AtrCandleType);
		primaryCandles
		.Bind(_primaryAtr, ProcessPrimaryAtr)
		.Start();

		var secondaryCandles = SubscribeCandles(AtrCandleType, security: SecondSecurity);
		secondaryCandles
		.Bind(_secondaryAtr, ProcessSecondaryAtr)
		.Start();
	}

	private void ProcessPrimaryLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			_primaryBid = (decimal)bidObj;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			_primaryAsk = (decimal)askObj;

		TryEvaluatePair();
	}

	private void ProcessSecondaryLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			_secondaryBid = (decimal)bidObj;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			_secondaryAsk = (decimal)askObj;

		TryEvaluatePair();
	}

	private void ProcessPrimaryAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_primaryAtrValue = atrValue;
	}

	private void ProcessSecondaryAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondaryAtrValue = atrValue;
	}

	private void TryEvaluatePair()
	{
		UpdateDrawdownState();
		ClosePairOnProfit();

		if (_tradingPaused)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_primaryBid is not decimal primaryBid || _secondaryBid is not decimal secondaryBid)
			return;

		if (HasOpenPair())
			return;

		if (IsVolatilityHigh())
			return;

		var difference = primaryBid - secondaryBid;

		if (difference > PriceDifferenceThreshold)
		{
			OpenPair(longPrimary: true, primaryBid, secondaryBid);
		}
		else if (difference < -PriceDifferenceThreshold)
		{
			OpenPair(longPrimary: false, primaryBid, secondaryBid);
		}
	}

	private void OpenPair(bool longPrimary, decimal primaryPrice, decimal secondaryPrice)
	{
		var primaryVolume = CalculateDynamicVolume(Security);
		var secondaryVolume = CalculateDynamicVolume(SecondSecurity);

		if (primaryVolume <= 0m || secondaryVolume <= 0m)
			return;

		secondaryVolume = NormalizeVolume(SecondSecurity, primaryVolume);

		if (secondaryVolume <= 0m)
			return;

		var entryDirection = longPrimary ? Sides.Buy : Sides.Sell;
		var hedgeDirection = longPrimary ? Sides.Sell : Sides.Buy;

		LogInfo($"Opening pair: {entryDirection} {primaryVolume:0.###} {Security?.Code} @ {primaryPrice:0.#####} | " +
		$"{hedgeDirection} {secondaryVolume:0.###} {SecondSecurity?.Code} @ {secondaryPrice:0.#####}.");

		if (longPrimary)
		{
			BuyMarket(primaryVolume);
			SellMarket(secondaryVolume, SecondSecurity);
		}
		else
		{
			SellMarket(primaryVolume);
			BuyMarket(secondaryVolume, SecondSecurity);
		}
	}

	private decimal CalculateDynamicVolume(Security security)
	{
		if (security == null)
			return 0m;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m || RiskPercent <= 0m)
			return NormalizeVolume(security, Volume);

		var riskAmount = portfolioValue * RiskPercent / 100m;

		var point = security.PriceStep ?? security.Step ?? 0m;
		if (point <= 0m)
			point = 1m;

		var stopDistance = StopLossPips * point;
		if (stopDistance <= 0m)
			return NormalizeVolume(security, Volume);

		var lotSize = riskAmount / stopDistance;
		if (lotSize <= 0m)
			return NormalizeVolume(security, Volume);

		var normalized = NormalizeVolume(security, lotSize);
		return normalized > 0m ? normalized : NormalizeVolume(security, Volume);
	}

	private void ClosePairOnProfit()
	{
		if (MinimumTotalProfit <= 0m)
			return;

		if (!HasOpenPair())
			return;

		var totalProfit = CalculateTotalProfit();
		if (totalProfit < MinimumTotalProfit)
			return;

		LogInfo($"Closing pair after reaching profit target: {totalProfit:0.##}.");
		ClosePairPositions();
	}

	private decimal CalculateTotalProfit()
	{
		var total = 0m;

		total += CalculateProfit(Security, _primaryPosition, _primaryEntryPrice, _primaryBid, _primaryAsk);
		total += CalculateProfit(SecondSecurity, _secondaryPosition, _secondaryEntryPrice, _secondaryBid, _secondaryAsk);

		return total;
	}

	private static decimal CalculateProfit(Security security, decimal positionVolume, decimal entryPrice, decimal? bid, decimal? ask)
	{
		if (security == null)
			return 0m;

		if (positionVolume == 0m || entryPrice <= 0m)
			return 0m;

		var isLong = positionVolume > 0m;
		var exitPrice = isLong ? bid ?? ask : ask ?? bid;

		if (exitPrice is not decimal price || price <= 0m)
			return 0m;

		var diff = isLong ? price - entryPrice : entryPrice - price;
		if (diff == 0m)
			return 0m;

		return ConvertPriceToMoney(security, diff, Math.Abs(positionVolume));
	}

	private static decimal ConvertPriceToMoney(Security security, decimal priceDifference, decimal volume)
	{
		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep > 0m && stepPrice > 0m)
		{
			var steps = priceDifference / priceStep;
			return steps * stepPrice * volume;
		}

		return priceDifference * volume;
	}

	private void ClosePairPositions()
	{
		var primaryVolume = Math.Abs(_primaryPosition);
		if (primaryVolume > 0m)
		{
			if (_primaryPosition > 0m)
				SellMarket(primaryVolume);
			else
				BuyMarket(primaryVolume);
		}

		var secondaryVolume = Math.Abs(_secondaryPosition);
		if (SecondSecurity != null && secondaryVolume > 0m)
		{
			if (_secondaryPosition > 0m)
				SellMarket(secondaryVolume, SecondSecurity);
			else
				BuyMarket(secondaryVolume, SecondSecurity);
		}
	}

	private bool HasOpenPair() => _primaryPosition != 0m || _secondaryPosition != 0m;

	private bool IsVolatilityHigh()
	{
		if (_primaryAtr is not { IsFormed: true } || _secondaryAtr is not { IsFormed: true })
			return false;

		var threshold = PriceDifferenceThreshold * 0.01m;
		return _primaryAtrValue > threshold || _secondaryAtrValue > threshold;
	}

	private void UpdateDrawdownState()
	{
		if (Portfolio == null)
			return;

		var equity = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return;

		if (equity > _peakEquity)
			_peakEquity = equity;

		if (MaxDrawdownPercent <= 0m || _peakEquity <= 0m)
		{
			_tradingPaused = false;
			return;
		}

		var drawdown = (_peakEquity - equity) / _peakEquity * 100m;

		if (!_tradingPaused && drawdown >= MaxDrawdownPercent)
		{
			_tradingPaused = true;
			LogWarning($"Trading paused due to drawdown {drawdown:0.##}%.");
		}
		else if (_tradingPaused)
		{
			var recovery = equity / _peakEquity * 100m;
			if (recovery >= RecoveryPercent)
			{
				_tradingPaused = false;
				LogInfo($"Equity recovered to {recovery:0.##}%, trading resumed.");
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		var execution = trade.Trade;

		if (order?.Security == null || execution == null)
			return;

		var price = execution.Price;
		var volume = trade.Volume;

		if (price <= 0m || volume <= 0m)
			return;

		var signedVolume = order.Side == Sides.Buy ? volume : -volume;

		if (order.Security == Security)
		{
			UpdatePosition(ref _primaryPosition, ref _primaryEntryPrice, signedVolume, price);
		}
		else if (order.Security == SecondSecurity)
		{
			UpdatePosition(ref _secondaryPosition, ref _secondaryEntryPrice, signedVolume, price);
		}
	}

	private static void UpdatePosition(ref decimal position, ref decimal entryPrice, decimal delta, decimal price)
	{
		var newVolume = position + delta;

		if (position == 0m)
		{
			entryPrice = price;
		}
		else if (Math.Sign(position) == Math.Sign(newVolume) && newVolume != 0m)
		{
			var prevAbs = Math.Abs(position);
			var deltaAbs = Math.Abs(delta);
			entryPrice = (entryPrice * prevAbs + price * deltaAbs) / (prevAbs + deltaAbs);
		}
		else if (newVolume == 0m)
		{
			entryPrice = 0m;
		}
		else
		{
			entryPrice = price;
		}

		position = newVolume;

		if (position == 0m)
			entryPrice = 0m;
	}

	private static decimal NormalizeVolume(Security security, decimal volume)
	{
		if (security == null)
			return volume;

		if (security.VolumeStep is decimal step && step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		if (security.VolumeMin is decimal min && min > 0m && volume < min)
			volume = min;

		if (security.VolumeMax is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}
}