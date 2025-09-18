using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum strategy that opens trades when bid/ask jumps reach a configurable distance and manages exits with profit and drawdown filters.
/// </summary>
public class LuckyCodeStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftPoints;
	private readonly StrategyParam<int> _limitPoints;

	private decimal? _previousAsk;
	private decimal? _previousBid;
	private decimal? _currentAsk;
	private decimal? _currentBid;

	private decimal _shiftThreshold;
	private decimal _limitThreshold;

	/// <summary>
	/// Minimum bid/ask movement in points required before opening a new trade.
	/// </summary>
	public int ShiftPoints
	{
		get => _shiftPoints.Value;
		set => _shiftPoints.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion in points tolerated before forcing an exit.
	/// </summary>
	public int LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public LuckyCodeStrategy()
	{
		_shiftPoints = Param(nameof(ShiftPoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift points", "Minimum bid/ask jump required to trigger entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_limitPoints = Param(nameof(LimitPoints), 18)
			.SetGreaterThanZero()
			.SetDisplay("Limit points", "Maximum number of points allowed against the position", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);
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

		// Subscribe to Level 1 quotes once the price thresholds are prepared.
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

		return points * step;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// React to the latest best ask update.
		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = (decimal)askObj;

			if (_previousAsk is decimal previousAsk && _shiftThreshold > 0m && ask - previousAsk >= _shiftThreshold)
			{
				OpenShort(ask, "Sell triggered by fast ask growth");
			}

			_previousAsk = ask;
			_currentAsk = ask;
		}

		// React to the latest best bid update.
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = (decimal)bidObj;

			if (_previousBid is decimal previousBid && _shiftThreshold > 0m && previousBid - bid >= _shiftThreshold)
			{
				OpenLong(bid, "Buy triggered by fast bid drop");
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
			// Manage long exposure: grab profits quickly or cap the drawdown.
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
			// Manage short exposure with symmetric exit conditions.
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
