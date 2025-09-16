using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that reacts to fast bid/ask shifts and closes trades on profit or adverse move limits.
/// </summary>
public class LuckyStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftPoints;
	private readonly StrategyParam<int> _limitPoints;
	private readonly StrategyParam<bool> _reverse;

	private decimal? _previousAsk;
	private decimal? _previousBid;
	private decimal? _currentAsk;
	private decimal? _currentBid;

	private decimal _shiftThreshold;
	private decimal _limitThreshold;

	/// <summary>
	/// Minimum number of points (pips) required for price acceleration.
	/// </summary>
	public int ShiftPoints
	{
		get => _shiftPoints.Value;
		set => _shiftPoints.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion in points before closing the position.
	/// </summary>
	public int LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Switch to invert the trading direction.
	/// </summary>
	public bool Reverse
	{
		get => _reverse.Value;
		set => _reverse.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public LuckyStrategy()
	{
		_shiftPoints = Param(nameof(ShiftPoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift points", "Minimum pip movement required to trigger a trade", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_limitPoints = Param(nameof(LimitPoints), 18)
			.SetGreaterThanZero()
			.SetDisplay("Limit points", "Maximum adverse pip movement before closing", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse mode", "Invert the direction of new trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAsk = null;
		_previousBid = null;
		_currentAsk = null;
		_currentBid = null;
		_shiftThreshold = 0m;
		_limitThreshold = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shiftThreshold = CalculatePriceOffset(ShiftPoints);
		_limitThreshold = CalculatePriceOffset(LimitPoints);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private decimal CalculatePriceOffset(int points)
	{
		if (points <= 0)
			return 0m;

		var step = Security?.PriceStep ?? Security?.Step ?? 0m;

		if (step <= 0m)
			return 0m;

		var multiplier = GetPipMultiplier(step);

		return points * step * multiplier;
	}

	private static decimal GetPipMultiplier(decimal step)
	{
		var digits = 0;
		var temp = step;

		while (temp > 0m && temp < 1m && digits < 10)
		{
			temp *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? 10m : 1m;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;

			if (_previousAsk.HasValue && _shiftThreshold > 0m && ask - _previousAsk.Value >= _shiftThreshold)
			{
				if (Reverse)
					OpenShort(ask, "Reverse sell triggered by ask breakout");
				else
					OpenLong(ask, "Buy triggered by ask breakout");
			}

			_previousAsk = ask;
			_currentAsk = ask;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;

			if (_previousBid.HasValue && _shiftThreshold > 0m && _previousBid.Value - bid >= _shiftThreshold)
			{
				if (Reverse)
					OpenLong(bid, "Reverse buy triggered by bid breakdown");
				else
					OpenShort(bid, "Sell triggered by bid breakdown");
			}

			_previousBid = bid;
			_currentBid = bid;
		}

		TryClosePosition();
	}

	private void OpenLong(decimal price, string reason)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume(price);

		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"{reason}. Price={price:0.#####}, Volume={volume:0.###}");
	}

	private void OpenShort(decimal price, string reason)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume(price);

		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"{reason}. Price={price:0.#####}, Volume={volume:0.###}");
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var baseVolume = Volume;

		if (Portfolio?.CurrentValue is decimal equity && equity > 0m && price > 0m)
		{
			var lots = Math.Round(equity / 10000m, 1, MidpointRounding.AwayFromZero);

			if (lots > 0m)
				baseVolume = Math.Max(baseVolume, lots);
		}

		return baseVolume;
	}

	private void TryClosePosition()
	{
		if (Position == 0)
			return;

		var avgPrice = Position.AveragePrice;

		if (avgPrice <= 0m)
			return;

		if (Position > 0)
		{
			if (_currentBid is decimal bid && bid > avgPrice)
			{
				SellMarket(Position);
				LogInfo($"Closed long on profit. Price={bid:0.#####}");
			}
			else if (_limitThreshold > 0m && _currentAsk is decimal ask && avgPrice - ask >= _limitThreshold)
			{
				SellMarket(Position);
				LogInfo($"Closed long on drawdown limit. Price={ask:0.#####}");
			}
		}
		else if (Position < 0)
		{
			if (_currentAsk is decimal ask && ask < avgPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Closed short on profit. Price={ask:0.#####}");
			}
			else if (_limitThreshold > 0m && _currentBid is decimal bid && bid - avgPrice >= _limitThreshold)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Closed short on drawdown limit. Price={bid:0.#####}");
			}
		}
	}
}
