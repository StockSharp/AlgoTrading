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
/// Triangular arbitrage strategy that trades three currency pairs.
/// Detects price discrepancies between the implied cross rate and the direct cross pair.
/// Closes the basket once the floating profit reaches the specified target.
/// </summary>
public class TriangularArbitrageStrategy : Strategy
{
	private readonly StrategyParam<Security> _firstPairParam;
	private readonly StrategyParam<Security> _secondPairParam;
	private readonly StrategyParam<Security> _crossPairParam;
	private readonly StrategyParam<decimal> _lotSizeParam;
	private readonly StrategyParam<decimal> _profitTargetParam;
	private readonly StrategyParam<decimal> _thresholdParam;
	private readonly StrategyParam<decimal> _minimumBalanceParam;

	private decimal? _firstAsk;
	private decimal? _firstBid;
	private decimal? _secondAsk;
	private decimal? _secondBid;
	private decimal? _crossAsk;
	private decimal? _crossBid;

	private decimal _firstPosition;
	private decimal _secondPosition;
	private decimal _crossPosition;

	private decimal _firstAveragePrice;
	private decimal _secondAveragePrice;
	private decimal _crossAveragePrice;

	private bool _hasOpenCycle;
	private bool _closeRequested;
	private decimal _realizedSnapshot;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TriangularArbitrageStrategy()
	{
		_firstPairParam = Param<Security>(nameof(FirstPair))
		.SetDisplay("First Pair", "Primary leg, e.g. EURUSD", "Instruments");

		_secondPairParam = Param<Security>(nameof(SecondPair))
		.SetDisplay("Second Pair", "Secondary leg, e.g. USDJPY", "Instruments");

		_crossPairParam = Param<Security>(nameof(CrossPair))
		.SetDisplay("Cross Pair", "Direct cross, e.g. EURJPY", "Instruments");

		_lotSizeParam = Param(nameof(LotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Volume submitted for every order", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_profitTargetParam = Param(nameof(ProfitTarget), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Target", "Profit in account currency that closes the basket", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 50m, 5m);

		_thresholdParam = Param(nameof(Threshold), 0.0001m)
		.SetGreaterThanZero()
		.SetDisplay("Threshold", "Relative difference between implied and direct price", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(0.00005m, 0.001m, 0.00005m);

		_minimumBalanceParam = Param(nameof(MinimumBalance), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Minimum Balance", "Required portfolio equity before trading", "Risk");
	}

	/// <summary>
	/// First currency pair (for example EURUSD).
	/// </summary>
	public Security FirstPair
	{
		get => _firstPairParam.Value;
		set => _firstPairParam.Value = value;
	}

	/// <summary>
	/// Second currency pair (for example USDJPY).
	/// </summary>
	public Security SecondPair
	{
		get => _secondPairParam.Value;
		set => _secondPairParam.Value = value;
	}

	/// <summary>
	/// Cross currency pair (for example EURJPY).
	/// </summary>
	public Security CrossPair
	{
		get => _crossPairParam.Value;
		set => _crossPairParam.Value = value;
	}

	/// <summary>
	/// Order volume used for every trade.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSizeParam.Value;
		set => _lotSizeParam.Value = value;
	}

	/// <summary>
	/// Profit target measured in the account currency.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTargetParam.Value;
		set => _profitTargetParam.Value = value;
	}

	/// <summary>
	/// Relative spread between implied and direct price required to open trades.
	/// </summary>
	public decimal Threshold
	{
		get => _thresholdParam.Value;
		set => _thresholdParam.Value = value;
	}

	/// <summary>
	/// Minimum equity that must be available before the strategy can open a cycle.
	/// </summary>
	public decimal MinimumBalance
	{
		get => _minimumBalanceParam.Value;
		set => _minimumBalanceParam.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (FirstPair != null)
		yield return (FirstPair, DataType.Level1);

		if (SecondPair != null)
		yield return (SecondPair, DataType.Level1);

		if (CrossPair != null)
		yield return (CrossPair, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstAsk = null;
		_firstBid = null;
		_secondAsk = null;
		_secondBid = null;
		_crossAsk = null;
		_crossBid = null;

		_firstPosition = 0m;
		_secondPosition = 0m;
		_crossPosition = 0m;

		_firstAveragePrice = 0m;
		_secondAveragePrice = 0m;
		_crossAveragePrice = 0m;

		_hasOpenCycle = false;
		_closeRequested = false;
		_realizedSnapshot = PnLManager?.RealizedPnL ?? 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_realizedSnapshot = PnLManager?.RealizedPnL ?? _realizedSnapshot;

		if (FirstPair == null || SecondPair == null || CrossPair == null)
		throw new InvalidOperationException("All three securities must be configured before starting the strategy.");

		SubscribeLevel1(FirstPair)
		.Bind(ProcessFirstPair)
		.Start();

		SubscribeLevel1(SecondPair)
		.Bind(ProcessSecondPair)
		.Start();

		SubscribeLevel1(CrossPair)
		.Bind(ProcessCrossPair)
		.Start();
	}

	private void ProcessFirstPair(Level1ChangeMessage level1)
	{
		UpdateQuotes(level1, ref _firstBid, ref _firstAsk);
		EvaluateArbitrage();
	}

	private void ProcessSecondPair(Level1ChangeMessage level1)
	{
		UpdateQuotes(level1, ref _secondBid, ref _secondAsk);
		EvaluateArbitrage();
	}

	private void ProcessCrossPair(Level1ChangeMessage level1)
	{
		UpdateQuotes(level1, ref _crossBid, ref _crossAsk);
		EvaluateArbitrage();
	}

	private static void UpdateQuotes(Level1ChangeMessage level1, ref decimal? bid, ref decimal? ask)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bidPrice)
		bid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal askPrice)
		ask = askPrice;
	}

	private void EvaluateArbitrage()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_firstAsk is not decimal firstAsk ||
		_secondAsk is not decimal secondAsk ||
		_crossAsk is not decimal crossAsk)
		return;

		if (firstAsk <= 0m || secondAsk <= 0m || crossAsk <= 0m)
		return;

		var impliedPrice = firstAsk * secondAsk;
		var diff = (impliedPrice - crossAsk) / crossAsk;

		if (!_hasOpenCycle && !_closeRequested)
		{
			var portfolio = Portfolio;
			var balance = portfolio?.CurrentValue ?? portfolio?.CurrentBalance ?? 0m;

			if (balance < MinimumBalance)
			return;

			if (diff > Threshold)
			{
				OpenCycle(1);
			}
			else if (diff < -Threshold)
			{
				OpenCycle(-1);
			}
		}

		if ((_hasOpenCycle || HasOpenPositions()) && !_closeRequested)
		{
			var profit = CalculateTotalProfit();
			if (ProfitTarget > 0m && profit >= ProfitTarget)
			CloseCycle();
		}
	}

	private void OpenCycle(int direction)
	{
		var firstVolume = AdjustVolume(FirstPair, LotSize);
		var secondVolume = AdjustVolume(SecondPair, LotSize);
		var crossVolume = AdjustVolume(CrossPair, LotSize);

		if (firstVolume <= 0m || secondVolume <= 0m || crossVolume <= 0m)
		{
			LogWarning("Volume cannot be aligned to instrument constraints.");
			return;
		}

		_hasOpenCycle = true;
		_closeRequested = false;
		_realizedSnapshot = PnLManager?.RealizedPnL ?? 0m;

		if (direction > 0)
		{
			BuyMarket(crossVolume, CrossPair);
			SellMarket(firstVolume, FirstPair);
			SellMarket(secondVolume, SecondPair);
		}
		else
		{
			SellMarket(crossVolume, CrossPair);
			BuyMarket(firstVolume, FirstPair);
			BuyMarket(secondVolume, SecondPair);
		}
	}

	private void CloseCycle()
	{
		_closeRequested = true;

		if (_crossPosition > 0m)
		SellMarket(_crossPosition, CrossPair);
		else if (_crossPosition < 0m)
		BuyMarket(-_crossPosition, CrossPair);

		if (_firstPosition > 0m)
		SellMarket(_firstPosition, FirstPair);
		else if (_firstPosition < 0m)
		BuyMarket(-_firstPosition, FirstPair);

		if (_secondPosition > 0m)
		SellMarket(_secondPosition, SecondPair);
		else if (_secondPosition < 0m)
		BuyMarket(-_secondPosition, SecondPair);
	}

	private bool HasOpenPositions()
	{
		return _firstPosition != 0m || _secondPosition != 0m || _crossPosition != 0m;
	}

	private decimal CalculateTotalProfit()
	{
		var profit = (PnLManager?.RealizedPnL ?? 0m) - _realizedSnapshot;

		profit += CalculateOpenPnL(FirstPair, _firstPosition, _firstAveragePrice, _firstBid, _firstAsk);
		profit += CalculateOpenPnL(SecondPair, _secondPosition, _secondAveragePrice, _secondBid, _secondAsk);
		profit += CalculateOpenPnL(CrossPair, _crossPosition, _crossAveragePrice, _crossBid, _crossAsk);

		return profit;
	}

	private static decimal CalculateOpenPnL(Security security, decimal position, decimal averagePrice, decimal? bid, decimal? ask)
	{
		if (security == null || position == 0m || averagePrice <= 0m)
		return 0m;

		decimal? price = null;

		if (position > 0m)
		price = bid ?? ask;
		else if (position < 0m)
		price = ask ?? bid;

		if (price is not decimal currentPrice || currentPrice <= 0m)
		return 0m;

		var priceDiff = position > 0m ? currentPrice - averagePrice : averagePrice - currentPrice;
		var volume = Math.Abs(position);

		var step = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (step > 0m && stepPrice > 0m)
		return priceDiff / step * stepPrice * volume;

		return priceDiff * volume;
	}

	private static decimal AdjustVolume(Security security, decimal desiredVolume)
	{
		if (security == null || desiredVolume <= 0m)
		return 0m;

		var volume = desiredVolume;

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (minVolume > 0m && volume < minVolume)
		return 0m;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var security = trade?.Trade?.Security;
		var order = trade?.Order;
		if (security == null || order == null)
		return;

		var direction = order.Direction;
		if (direction == null)
		return;

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
		return;

		var signedVolume = direction == Sides.Buy ? volume : -volume;
		var price = trade.Trade.Price;

		if (security == FirstPair)
		{
			UpdatePosition(ref _firstPosition, ref _firstAveragePrice, signedVolume, price);
		}
		else if (security == SecondPair)
		{
			UpdatePosition(ref _secondPosition, ref _secondAveragePrice, signedVolume, price);
		}
		else if (security == CrossPair)
		{
			UpdatePosition(ref _crossPosition, ref _crossAveragePrice, signedVolume, price);
		}

		if (!HasOpenPositions())
		{
			_hasOpenCycle = false;
			_closeRequested = false;
			_realizedSnapshot = PnLManager?.RealizedPnL ?? _realizedSnapshot;
		}
	}

	private static void UpdatePosition(ref decimal position, ref decimal averagePrice, decimal signedVolume, decimal tradePrice)
	{
		if (signedVolume == 0m)
		return;

		var newPosition = position + signedVolume;

		if (position == 0m)
		{
			averagePrice = tradePrice;
		}
		else if (Math.Sign(position) == Math.Sign(signedVolume) && Math.Sign(newPosition) == Math.Sign(position))
		{
			var previousAbs = Math.Abs(position);
			var tradeAbs = Math.Abs(signedVolume);
			var newAbs = Math.Abs(newPosition);

			if (newAbs > 0m)
			averagePrice = (averagePrice * previousAbs + tradePrice * tradeAbs) / newAbs;
		}
		else if (newPosition == 0m)
		{
			averagePrice = 0m;
		}
		else if (Math.Sign(position) != Math.Sign(newPosition))
		{
			averagePrice = tradePrice;
		}

		position = newPosition;

		if (position == 0m)
		averagePrice = 0m;
	}
}

