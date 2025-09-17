using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Locker strategy that adds hedging orders whenever floating loss exceeds a predefined threshold.
/// </summary>
public class LockerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _needProfitRatio;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _stepVolume;
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<bool> _enableRescue;

	private decimal _checkpointPrice;
	private decimal _highestBuyPrice;
	private decimal _lowestSellPrice;
	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _pipSize;
	private decimal _lastTradePrice;
	private decimal _startingEquity;
	private bool _initialOrderPlaced;
	private bool _waitingForReset;
	private bool _closingInProgress;
	private bool _startingEquityCaptured;

	/// <summary>
	/// Initializes a new instance of <see cref="LockerStrategy"/>.
	/// </summary>
	public LockerStrategy()
	{
		_needProfitRatio = Param(nameof(NeedProfitRatio), 0.001m)
			.SetDisplay("Profit Ratio", "Fraction of equity required before closing all trades", "Risk")
			.SetCanOptimize(true);

		_initialVolume = Param(nameof(InitialVolume), 0.5m)
			.SetDisplay("Initial Volume", "Volume of the very first market buy", "Trading")
			.SetCanOptimize(true);

		_stepVolume = Param(nameof(StepVolume), 0.2m)
			.SetDisplay("Step Volume", "Volume used for every rescue order", "Trading")
			.SetCanOptimize(true);

		_stepPoints = Param(nameof(StepPoints), 50)
			.SetDisplay("Step Points", "Distance in points that must be covered before adding a new order", "Trading")
			.SetCanOptimize(true);

		_enableRescue = Param(nameof(EnableRescue), true)
			.SetDisplay("Enable Rescue", "Allow the strategy to average when loss exceeds the threshold", "Risk");
	}

	/// <summary>
	/// Fraction of portfolio equity that must be achieved before locking the cycle.
	/// </summary>
	public decimal NeedProfitRatio
	{
		get => _needProfitRatio.Value;
		set => _needProfitRatio.Value = value;
	}

	/// <summary>
	/// Volume of the very first market buy order in every cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Volume used for every rescue order triggered by the drawdown.
	/// </summary>
	public decimal StepVolume
	{
		get => _stepVolume.Value;
		set => _stepVolume.Value = value;
	}

	/// <summary>
	/// Distance in MetaTrader points that must be covered before a new order is added.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Enables the rescue grid when the floating loss breaches the threshold.
	/// </summary>
	public bool EnableRescue
	{
		get => _enableRescue.Value;
		set => _enableRescue.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSize();

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		if (price <= 0m)
			return;

		_lastTradePrice = price;

		if (_pipSize <= 0m)
			UpdatePipSize();

		if (_pipSize <= 0m)
			return;

		if (!_initialOrderPlaced && !_waitingForReset)
			TryOpenInitialOrder(price);

		var portfolioValue = GetPortfolioValue();
		if (!_startingEquityCaptured && portfolioValue > 0m)
		{
			_startingEquity = portfolioValue;
			_startingEquityCaptured = true;
		}
		else if (portfolioValue <= 0m && _startingEquityCaptured)
		{
			portfolioValue = _startingEquity;
		}

		var threshold = NeedProfitRatio * portfolioValue;
		var unrealized = GetUnrealizedProfit(price);

		if (threshold > 0m && unrealized >= threshold)
		{
			if (!_closingInProgress)
			{
				CloseAllPositions();
				_closingInProgress = true;
				_waitingForReset = true;
			}

			return;
		}

		if (!EnableRescue || threshold <= 0m)
			return;

		if (unrealized > -threshold)
			return;

		if (_longVolume <= 0m && _shortVolume <= 0m)
			return;

		var stepDistance = GetStepDistance();
		if (stepDistance <= 0m)
			return;

		var rescueVolume = AlignVolume(StepVolume);
		if (rescueVolume <= 0m)
			return;

		var needBuy = price >= _checkpointPrice + stepDistance && price > _highestBuyPrice;
		if (needBuy)
		{
			BuyMarket(rescueVolume);
			_checkpointPrice = price;
			_highestBuyPrice = Math.Max(_highestBuyPrice, price);
		}

		var needSell = price <= _checkpointPrice - stepDistance && price < _lowestSellPrice;
		if (needSell)
		{
			SellMarket(rescueVolume);
			_checkpointPrice = price;
			_lowestSellPrice = Math.Min(_lowestSellPrice, price);
		}
	}

	private void TryOpenInitialOrder(decimal price)
	{
		var volume = AlignVolume(InitialVolume);
		if (volume <= 0m)
			return;

		_initialOrderPlaced = true;
		BuyMarket(volume);

		_checkpointPrice = price;
		_highestBuyPrice = price;
		_lowestSellPrice = price;
	}

	private decimal GetUnrealizedProfit(decimal price)
	{
		var profit = 0m;

		if (_longVolume > 0m)
			profit += _longVolume * (price - _longAveragePrice);

		if (_shortVolume > 0m)
			profit += _shortVolume * (_shortAveragePrice - price);

		return profit;
	}

	private decimal GetStepDistance()
	{
		var points = StepPoints;
		if (points <= 0)
			return 0m;

		return points * _pipSize;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		var value = portfolio.CurrentValue ?? 0m;
		if (value <= 0m)
			value = portfolio.CurrentBalance ?? 0m;
		if (value <= 0m)
			value = portfolio.BeginValue ?? 0m;
		if (value <= 0m && _startingEquityCaptured)
			value = _startingEquity;

		return value;
	}

	private decimal AlignVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.VolumeMin ?? 0m;
		var max = security.VolumeMax ?? decimal.MaxValue;

		if (min > 0m && volume < min)
			volume = min;
		if (max > 0m && volume > max)
			volume = max;
		if (step > 0m)
			volume = Math.Round(volume / step) * step;

		return volume;
	}

	private void UpdatePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0m;
			return;
		}

		var step = security.PriceStep ?? security.Step ?? 0m;
		if (step <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		var decimals = security.Decimals;
		_pipSize = decimals is 3 or 5 ? step * 10m : step;
	}

	private void CloseAllPositions()
	{
		if (_longVolume > 0m)
			SellMarket(_longVolume);

		if (_shortVolume > 0m)
			BuyMarket(_shortVolume);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(_shortVolume, volume);
				_shortVolume -= closingVolume;
				volume -= closingVolume;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(_longVolume, volume);
				_longVolume -= closingVolume;
				volume -= closingVolume;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
			}
		}

		if (_longVolume <= 0m && _shortVolume <= 0m)
		{
			_longVolume = 0m;
			_shortVolume = 0m;

			if (_waitingForReset)
			{
				_waitingForReset = false;
				_closingInProgress = false;
				_initialOrderPlaced = false;
				_checkpointPrice = _lastTradePrice;
				_highestBuyPrice = _lastTradePrice;
				_lowestSellPrice = _lastTradePrice;
			}
		}
	}
}
