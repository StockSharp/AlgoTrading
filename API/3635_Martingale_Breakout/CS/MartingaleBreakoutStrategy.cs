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
/// Port of the "MartinGaleBreakout" MetaTrader expert advisor that looks for large breakout candles and
/// scales the recovery target after a stop-loss.
/// </summary>
public class MartingaleBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _balancePercentAvailable;
	private readonly StrategyParam<decimal> _takeProfitPercentOfBalance;
	private readonly StrategyParam<decimal> _stopLossPercentOfBalance;
	private readonly StrategyParam<decimal> _recoveryStartFraction;
	private readonly StrategyParam<decimal> _recoveryPointsMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLossThreshold;
	private decimal _takeProfitThreshold;
	private bool _isRecovering;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;

	private readonly decimal[] _rangeBuffer = new decimal[10];
	private int _rangeBufferCount;
	private int _rangeBufferIndex;
	private decimal _rangeBufferSum;

	/// <summary>
	/// Distance in price steps between the entry and the take-profit level.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum percentage of account balance that can be used for margin.
	/// </summary>
	public decimal BalancePercentAvailable
	{
		get => _balancePercentAvailable.Value;
		set => _balancePercentAvailable.Value = value;
	}

	/// <summary>
	/// Monetary profit target expressed as a percentage of the account balance.
	/// </summary>
	public decimal TakeProfitPercentOfBalance
	{
		get => _takeProfitPercentOfBalance.Value;
		set => _takeProfitPercentOfBalance.Value = value;
	}

	/// <summary>
	/// Monetary stop-loss expressed as a percentage of the account balance.
	/// </summary>
	public decimal StopLossPercentOfBalance
	{
		get => _stopLossPercentOfBalance.Value;
		set => _stopLossPercentOfBalance.Value = value;
	}

	/// <summary>
	/// Multiplier that scales the stop-loss while the strategy is recovering.
	/// </summary>
	public decimal RecoveryStartFraction
	{
		get => _recoveryStartFraction.Value;
		set => _recoveryStartFraction.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the take-profit distance while recovering.
	/// </summary>
	public decimal RecoveryPointsMultiplier
	{
		get => _recoveryPointsMultiplier.Value;
		set => _recoveryPointsMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MartingaleBreakoutStrategy"/> class.
	/// </summary>
	public MartingaleBreakoutStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
		.SetRange(5, 500)
		.SetDisplay("Take-profit points", "Distance between entry and take-profit in price steps.", "Trading")
		.SetCanOptimize(true);

		_balancePercentAvailable = Param(nameof(BalancePercentAvailable), 50m)
		.SetRange(1m, 100m)
		.SetDisplay("Balance usage", "Maximum balance percentage allowed for margin.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPercentOfBalance = Param(nameof(TakeProfitPercentOfBalance), 0.1m)
		.SetRange(0.01m, 5m)
		.SetDisplay("Balance take-profit", "Target profit as a percentage of balance.", "Risk")
		.SetCanOptimize(true);

		_stopLossPercentOfBalance = Param(nameof(StopLossPercentOfBalance), 10m)
		.SetRange(0.5m, 50m)
		.SetDisplay("Balance stop-loss", "Maximum loss as a percentage of balance.", "Risk")
		.SetCanOptimize(true);

		_recoveryStartFraction = Param(nameof(RecoveryStartFraction), 0.1m)
		.SetRange(0.01m, 1m)
		.SetDisplay("Recovery fraction", "Fraction of the stop-loss used before activating recovery mode.", "Risk")
		.SetCanOptimize(true);

		_recoveryPointsMultiplier = Param(nameof(RecoveryPointsMultiplier), 1m)
		.SetRange(0.5m, 5m)
		.SetDisplay("Recovery distance", "Multiplier applied to the take-profit distance while recovering.", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
		.SetDisplay("Candle type", "Data source used for breakout detection.", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ResetState()
	{
		_stopLossThreshold = 0m;
		_takeProfitThreshold = 0m;
		_isRecovering = false;

		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;

		Array.Clear(_rangeBuffer, 0, _rangeBuffer.Length);
		_rangeBufferCount = 0;
		_rangeBufferIndex = 0;
		_rangeBufferSum = 0m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var portfolio = Portfolio;
		var security = Security;
		if (portfolio == null || security == null)
		return;

		var balance = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
		return;

		EnsureThresholds(balance);

		var closePrice = candle.ClosePrice;
		if (closePrice <= 0m)
		return;

		var floatingPnL = GetFloatingPnL(closePrice);

		var baseStopLoss = StopLossPercentOfBalance / 100m * balance;
		var recoveryStopLoss = baseStopLoss * RecoveryStartFraction;

		if (floatingPnL <= -_stopLossThreshold || floatingPnL >= _takeProfitThreshold)
		{
			if (floatingPnL <= -_stopLossThreshold && _stopLossThreshold < baseStopLoss)
			{
				FlattenPositions();
				_takeProfitThreshold -= floatingPnL;
				_stopLossThreshold = baseStopLoss;
				_isRecovering = true;
				return;
			}

			FlattenPositions();
			_takeProfitThreshold = TakeProfitPercentOfBalance / 100m * balance;
			_stopLossThreshold = recoveryStopLoss;
			_isRecovering = false;
			return;
		}

		if (Position != 0m)
		return;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var ask = security.BestAsk?.Price ?? closePrice;
		var bid = security.BestBid?.Price ?? closePrice;

		var isBullBreakout = IsBullBreakout(candle);
		var isBearBreakout = IsBearBreakout(candle);

		UpdateRangeStatistics(candle);

		var availableBalance = balance * BalancePercentAvailable / 100m;
		var freeFunds = (portfolio.CurrentValue ?? 0m) - (portfolio.BlockedValue ?? 0m);

		if (isBullBreakout)
		{
			var profitDistance = priceStep * TakeProfitPoints * (_isRecovering ? RecoveryPointsMultiplier : 1m);
			var targetPrice = ask + profitDistance;
			var volume = CalculateVolume(_takeProfitThreshold, ask, targetPrice);

			if (IsVolumeAllowed(volume, availableBalance, freeFunds, ask, Sides.Buy))
			{
				BuyMarket(volume);
				return;
			}
		}

		if (isBearBreakout)
		{
			var profitDistance = priceStep * TakeProfitPoints * (_isRecovering ? RecoveryPointsMultiplier : 1m);
			var targetPrice = bid - profitDistance;
			var volume = CalculateVolume(_takeProfitThreshold, bid, targetPrice);

			if (IsVolumeAllowed(volume, availableBalance, freeFunds, bid, Sides.Sell))
			{
				SellMarket(volume);
			}
		}
	}

	private void EnsureThresholds(decimal balance)
	{
		if (_stopLossThreshold > 0m && _takeProfitThreshold > 0m)
		return;

		var baseStopLoss = StopLossPercentOfBalance / 100m * balance;
		var recoveryStopLoss = baseStopLoss * RecoveryStartFraction;
		var takeProfit = TakeProfitPercentOfBalance / 100m * balance;

		_stopLossThreshold = recoveryStopLoss;
		_takeProfitThreshold = takeProfit;
		_isRecovering = false;
	}

	private bool IsBullBreakout(ICandleMessage candle)
	{
		if (!IsAbnormalRange(candle))
		return false;

		var body = candle.ClosePrice - candle.OpenPrice;
		var range = candle.HighPrice - candle.LowPrice;

		return body > 0m && body > 0.5m * range;
	}

	private bool IsBearBreakout(ICandleMessage candle)
	{
		if (!IsAbnormalRange(candle))
		return false;

		var body = candle.OpenPrice - candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;

		return body > 0m && body > 0.5m * range;
	}

	private bool IsAbnormalRange(ICandleMessage candle)
	{
		if (_rangeBufferCount < _rangeBuffer.Length)
		return false;

		var range = candle.HighPrice - candle.LowPrice;
		var average = _rangeBufferSum / _rangeBuffer.Length;

		return range > average * 3m;
	}

	private void UpdateRangeStatistics(ICandleMessage candle)
	{
		var range = candle.HighPrice - candle.LowPrice;

		if (_rangeBufferCount < _rangeBuffer.Length)
		{
			_rangeBuffer[_rangeBufferIndex] = range;
			_rangeBufferSum += range;
			_rangeBufferCount++;
			_rangeBufferIndex = (_rangeBufferIndex + 1) % _rangeBuffer.Length;
			return;
		}

		_rangeBufferSum -= _rangeBuffer[_rangeBufferIndex];
		_rangeBuffer[_rangeBufferIndex] = range;
		_rangeBufferSum += range;
		_rangeBufferIndex = (_rangeBufferIndex + 1) % _rangeBuffer.Length;
	}

	private decimal CalculateVolume(decimal takeProfitTarget, decimal startPrice, decimal endPrice)
	{
		var security = Security;
		if (security == null)
		return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		var volumeStep = security.VolumeStep ?? 1m;

		var priceDiff = Math.Abs(endPrice - startPrice);
		if (priceDiff <= 0m || priceStep <= 0m || stepPrice <= 0m || volumeStep <= 0m)
		return 0m;

		var steps = priceDiff / priceStep;
		if (steps <= 0m)
		return 0m;

		var volume = takeProfitTarget * volumeStep / (steps * stepPrice);

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var volumeStep = security.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Floor(volume / volumeStep);
			volume = steps > 0m ? steps * volumeStep : 0m;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private bool IsVolumeAllowed(decimal volume, decimal allowedBalance, decimal freeFunds, decimal price, Sides side)
	{
		if (volume <= 0m)
		return false;

		var security = Security;
		if (security == null)
		return false;

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		return false;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		return false;

		var volumeStep = security.VolumeStep ?? 0m;
		if (volumeStep > 0m)
		{
			var steps = Math.Round(volume / volumeStep);
			if (Math.Abs(steps * volumeStep - volume) > 0.0000001m)
			return false;
		}

		var marginPerStep = GetMarginPerStepVolume(price, side);
		if (marginPerStep <= 0m)
		return true;

		var stepVolume = security.VolumeStep ?? 1m;
		var multiplier = volumeStep > 0m ? volume / volumeStep : volume / stepVolume;
		var requiredMargin = marginPerStep * multiplier;

		if (allowedBalance > 0m && requiredMargin > allowedBalance)
		return false;

		if (freeFunds > 0m && requiredMargin > freeFunds)
		return false;

		return true;
	}

	private decimal GetMarginPerStepVolume(decimal price, Sides side)
	{
		var security = Security;
		if (security == null)
		return 0m;

		var margin = side == Sides.Buy ? security.MarginBuy : security.MarginSell;
		if (margin is decimal directMargin && directMargin > 0m)
		return directMargin;

		if (price <= 0m)
		price = security.LastPrice ?? 0m;

		if (price <= 0m)
		return 0m;

		var volumeStep = security.VolumeStep ?? 1m;
		return price * volumeStep;
	}

	private decimal GetFloatingPnL(decimal currentPrice)
	{
		var security = Security;
		if (security == null)
		return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		var volumeStep = security.VolumeStep ?? 1m;

		if (priceStep <= 0m || stepPrice <= 0m || volumeStep <= 0m)
		{
			var longPnL = _longVolume > 0m ? (currentPrice - _longAveragePrice) * _longVolume : 0m;
			var shortPnL = _shortVolume > 0m ? (_shortAveragePrice - currentPrice) * _shortVolume : 0m;
			return longPnL + shortPnL;
		}

		decimal ConvertToMoney(decimal priceDifference, decimal volume)
		{
			if (volume <= 0m)
			return 0m;

			var steps = priceDifference / priceStep;
			return steps * stepPrice * (volume / volumeStep);
		}

		var longProfit = ConvertToMoney(currentPrice - _longAveragePrice, _longVolume);
		var shortProfit = ConvertToMoney(_shortAveragePrice - currentPrice, _shortVolume);

		return longProfit + shortProfit;
	}

	private void FlattenPositions()
	{
		CancelActiveOrders();

		if (_longVolume > 0m)
		SellMarket(_longVolume);

		if (_shortVolume > 0m)
		BuyMarket(_shortVolume);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null || trade.Trade == null)
		return;

		if (trade.Trade.Security != Security)
		return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(_shortVolume, volume);
				ReduceShort(closingVolume, price);
				volume -= closingVolume;
			}

			if (volume > 0m)
			IncreaseLong(volume, price);
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(_longVolume, volume);
				ReduceLong(closingVolume, price);
				volume -= closingVolume;
			}

			if (volume > 0m)
			IncreaseShort(volume, price);
		}
	}

	private void IncreaseLong(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var totalVolume = _longVolume + volume;
		_longAveragePrice = totalVolume > 0m
		? (_longAveragePrice * _longVolume + price * volume) / totalVolume
		: price;
		_longVolume = totalVolume;
	}

	private void ReduceLong(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		_longVolume -= volume;
		if (_longVolume <= 0m)
		{
			_longVolume = 0m;
			_longAveragePrice = 0m;
		}
	}

	private void IncreaseShort(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var totalVolume = _shortVolume + volume;
		_shortAveragePrice = totalVolume > 0m
		? (_shortAveragePrice * _shortVolume + price * volume) / totalVolume
		: price;
		_shortVolume = totalVolume;
	}

	private void ReduceShort(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		_shortVolume -= volume;
		if (_shortVolume <= 0m)
		{
			_shortVolume = 0m;
			_shortAveragePrice = 0m;
		}
	}
}

