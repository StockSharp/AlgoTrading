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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Counter-trend breakout strategy converted from the MT4 expert advisor "Casino111".
/// The system monitors the previous daily range and opens market orders when the new candle open gaps beyond those extremes.
/// </summary>
public class Casino111Strategy : Strategy
{
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<decimal> _betPoints;
	private readonly StrategyParam<decimal> _upperOffsetPoints;
	private readonly StrategyParam<decimal> _lowerOffsetPoints;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _dailyCandleType;

	private decimal? _previousOpen;
	private decimal? _dailyUpperThreshold;
	private decimal? _dailyLowerThreshold;

	private decimal _priceStep;
	private decimal _bestBid;
	private decimal _bestAsk;

	private decimal _currentVolume;
	private decimal _previousPosition;
	private decimal _lastRealizedPnL;

	/// <summary>
	/// Initializes a new instance of the <see cref="Casino111Strategy"/> class.
	/// </summary>
	public Casino111Strategy()
	{
		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long entries when the lower trigger is broken.", "General");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow short entries when the upper trigger is broken.", "General");

		_betPoints = Param(nameof(BetPoints), 400m)
			.SetDisplay("Stop/Take Distance", "Protective distance in MetaTrader points applied to both stop-loss and take-profit.", "Risk")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(100m, 800m, 10m);

		_upperOffsetPoints = Param(nameof(UpperOffsetPoints), 97m)
			.SetDisplay("Upper Offset", "Points added to the previous daily high to define the sell trigger.", "Logic")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 5m);

		_lowerOffsetPoints = Param(nameof(LowerOffsetPoints), 77m)
			.SetDisplay("Lower Offset", "Points subtracted from the previous daily low to define the buy trigger.", "Logic")
			.SetNotNegative()
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 5m);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Reproduce the original martingale-style volume progression.", "Risk");

		_maxVolume = Param(nameof(MaxVolume), 4m)
			.SetDisplay("Max Volume", "Ceiling applied to the calculated volume when money management is active.", "Risk")
			.SetGreaterThanZero();

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Initial order volume used after a win or when money management is disabled.", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Trading Candle", "Primary timeframe evaluated for gap-break reversals.", "General");

		_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Daily Candle", "Higher timeframe supplying the previous day's high and low levels.", "General");
	}

	/// <summary>
	/// Enables or disables long entries.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Enables or disables short entries.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Protective stop-loss and take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal BetPoints
	{
		get => _betPoints.Value;
		set => _betPoints.Value = value;
	}

	/// <summary>
	/// Offset added to the previous daily high to build the sell trigger.
	/// </summary>
	public decimal UpperOffsetPoints
	{
		get => _upperOffsetPoints.Value;
		set => _upperOffsetPoints.Value = value;
	}

	/// <summary>
	/// Offset subtracted from the previous daily low to build the buy trigger.
	/// </summary>
	public decimal LowerOffsetPoints
	{
		get => _lowerOffsetPoints.Value;
		set => _lowerOffsetPoints.Value = value;
	}

	/// <summary>
	/// Enables the martingale-style volume progression.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Maximum order volume allowed when the progression escalates.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Base order volume used after a profitable trade and when money management is disabled.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Trading timeframe evaluated for the gap-break logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Daily timeframe providing the reference high and low levels.
	/// </summary>
	public DataType DailyCandleType
	{
		get => _dailyCandleType.Value;
		set => _dailyCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CandleType);

		if (DailyCandleType != CandleType)
			yield return (Security, DailyCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousOpen = null;
		_dailyUpperThreshold = null;
		_dailyLowerThreshold = null;

		_priceStep = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;

		_currentVolume = 0m;
		_previousPosition = 0m;
		_lastRealizedPnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_currentVolume = AlignVolume(BaseVolume);

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
			.Bind(ProcessTradingCandle)
			.Start();

		var dailySubscription = SubscribeCandles(DailyCandleType);
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(ProcessDepth)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawOwnTrades(area);
		}

		var bet = BetPoints;
		if (bet > 0m)
		{
			var unit = new Unit(bet, UnitTypes.Step);
			StartProtection(unit, unit);
		}
		else
		{
			StartProtection();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (_previousPosition == 0m && Position != 0m)
		{
			_lastRealizedPnL = PnL;
		}
		else if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;

			UpdateMoneyManagement(tradePnL);
		}

		_previousPosition = Position;
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var currentOpen = candle.OpenPrice;

		if (_previousOpen is not decimal previousOpen)
		{
			_previousOpen = currentOpen;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousOpen = currentOpen;
			return;
		}

		if (_dailyUpperThreshold is not decimal upperThreshold || _dailyLowerThreshold is not decimal lowerThreshold)
		{
			_previousOpen = currentOpen;
			return;
		}

		var signal = 0;

		if (previousOpen < upperThreshold && currentOpen > upperThreshold)
			signal = -1;
		else if (previousOpen > lowerThreshold && currentOpen < lowerThreshold)
			signal = 1;

		if (Position != 0m || HasActiveOrders())
		{
			_previousOpen = currentOpen;
			return;
		}

		if (signal > 0)
		{
			TryOpenLong(currentOpen, lowerThreshold);
		}
		else if (signal < 0)
		{
			TryOpenShort(currentOpen, upperThreshold);
		}

		_previousOpen = currentOpen;
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dailyUpperThreshold = candle.HighPrice + _priceStep * UpperOffsetPoints;
		_dailyLowerThreshold = candle.LowPrice - _priceStep * LowerOffsetPoints;
	}

	private void ProcessDepth(IOrderBookMessage depth)
	{
		var bestBid = depth.GetBestBid();
		if (bestBid != null)
			_bestBid = bestBid.Price;

		var bestAsk = depth.GetBestAsk();
		if (bestAsk != null)
			_bestAsk = bestAsk.Price;
	}

	private void TryOpenLong(decimal openPrice, decimal triggerPrice)
	{
		if (!EnableBuy)
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"Long entry: open {openPrice:F5} crossed below trigger {triggerPrice:F5}.");
	}

	private void TryOpenShort(decimal openPrice, decimal triggerPrice)
	{
		if (!EnableSell)
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"Short entry: open {openPrice:F5} crossed above trigger {triggerPrice:F5}.");
	}

	private decimal GetTradeVolume()
	{
		var baseVolume = AlignVolume(BaseVolume);

		if (!UseMoneyManagement)
		{
			_currentVolume = baseVolume;
			return baseVolume;
		}

		if (_currentVolume <= 0m)
			_currentVolume = baseVolume;

		var volume = AlignVolume(_currentVolume);

		var limit = MaxVolume;
		if (limit > 0m && volume > limit)
		{
			volume = AlignVolume(limit);
			_currentVolume = volume;
		}

		return volume;
	}

	private void UpdateMoneyManagement(decimal tradePnL)
	{
		var baseVolume = AlignVolume(BaseVolume);

		if (!UseMoneyManagement)
		{
			_currentVolume = baseVolume;
			return;
		}

		if (tradePnL > 0m)
		{
			_currentVolume = baseVolume;
			return;
		}

		var multiplier = CalculateProgressionFactor();
		if (multiplier <= 0m)
		{
			_currentVolume = baseVolume;
			return;
		}

		var nextVolume = _currentVolume > 0m ? _currentVolume * multiplier : baseVolume * multiplier;

		var limit = MaxVolume;
		if (limit > 0m && nextVolume > limit)
			nextVolume = limit;

		_currentVolume = AlignVolume(nextVolume);
	}

	private decimal CalculateProgressionFactor()
	{
		var bet = BetPoints;
		if (bet <= 0m)
			return 1m;

		var spreadPoints = GetSpreadPoints();

		var denominator = bet - spreadPoints;
		if (denominator <= 0m)
			return 1m;

		return (bet * 2m) / denominator;
	}

	private decimal GetSpreadPoints()
	{
		if (_priceStep <= 0m)
			return 0m;

		if (_bestBid > 0m && _bestAsk > 0m && _bestAsk > _bestBid)
			return (_bestAsk - _bestBid) / _priceStep;

		return 0m;
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}
}
