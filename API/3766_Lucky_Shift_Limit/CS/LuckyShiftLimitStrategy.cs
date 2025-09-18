using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quote-reversion strategy that reacts to sudden bid/ask jumps and enforces a MetaTrader-style loss cap.
/// </summary>
public class LuckyShiftLimitStrategy : Strategy
{
	private readonly StrategyParam<int> _shiftPoints;
	private readonly StrategyParam<int> _limitPoints;

	private decimal? _previousAsk;
	private decimal? _previousBid;
	private decimal? _currentAsk;
	private decimal? _currentBid;

	private decimal _shiftOffset;
	private decimal _limitOffset;

	/// <summary>
	/// Minimum number of MetaTrader points separating consecutive asks before a fade-in sell order is sent.
	/// </summary>
	public int ShiftPoints
	{
		get => _shiftPoints.Value;
		set => _shiftPoints.Value = value;
	}

	/// <summary>
	/// Maximum adverse excursion (in MetaTrader points) tolerated before force-closing losing trades.
	/// </summary>
	public int LimitPoints
	{
		get => _limitPoints.Value;
		set => _limitPoints.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters taken from the original MQ4 expert.
	/// </summary>
	public LuckyShiftLimitStrategy()
	{
		_shiftPoints = Param(nameof(ShiftPoints), 3)
			.SetGreaterThanZero()
			.SetDisplay("Shift points", "Minimum pip delta between consecutive quotes", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_limitPoints = Param(nameof(LimitPoints), 18)
			.SetGreaterThanZero()
			.SetDisplay("Limit points", "Maximum allowed drawdown in pips", "Risk management")
			.SetCanOptimize(true)
			.SetOptimize(5, 80, 5);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAsk = null;
		_previousBid = null;
		_currentAsk = null;
		_currentBid = null;
		_shiftOffset = 0m;
		_limitOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shiftOffset = CalculatePriceOffset(ShiftPoints);
		_limitOffset = CalculatePriceOffset(LimitPoints);

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

		return points * step * GetPipMultiplier(step);
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
		UpdateOffsetsIfChanged();

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
		{
			if (_previousAsk is decimal prevAsk && _shiftOffset > 0m && ask - prevAsk >= _shiftOffset)
			{
				TryOpenShort(ask, "Ask accelerated above threshold");
			}

			_previousAsk = ask;
			_currentAsk = ask;
		}

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
		{
			if (_previousBid is decimal prevBid && _shiftOffset > 0m && prevBid - bid >= _shiftOffset)
			{
				TryOpenLong(bid, "Bid dropped below threshold");
			}

			_previousBid = bid;
			_currentBid = bid;
		}

		TryClosePosition();
	}

	private void UpdateOffsetsIfChanged()
	{
		var shift = CalculatePriceOffset(ShiftPoints);

		if (shift != _shiftOffset)
			_shiftOffset = shift;

		var limit = CalculatePriceOffset(LimitPoints);

		if (limit != _limitOffset)
			_limitOffset = limit;
	}

	private void TryOpenLong(decimal price, string reason)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume();

		if (volume <= 0m)
			return;

		BuyMarket(volume);
		LogInfo($"{reason}. Price={price:0.#####}, Volume={volume:0.###}");
	}

	private void TryOpenShort(decimal price, string reason)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = CalculateOrderVolume();

		if (volume <= 0m)
			return;

		SellMarket(volume);
		LogInfo($"{reason}. Price={price:0.#####}, Volume={volume:0.###}");
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = Volume;

		if (Portfolio?.CurrentValue is decimal equity && equity > 0m)
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
				LogInfo($"Closed long in profit. Bid={bid:0.#####}");
				return;
			}

			if (_limitOffset > 0m && _currentAsk is decimal ask && avgPrice - ask >= _limitOffset)
			{
				SellMarket(Position);
				LogInfo($"Closed long on loss cap. Ask={ask:0.#####}");
			}
		}
		else
		{
			var volume = Math.Abs(Position);

			if (_currentAsk is decimal ask && ask < avgPrice)
			{
				BuyMarket(volume);
				LogInfo($"Closed short in profit. Ask={ask:0.#####}");
				return;
			}

			if (_limitOffset > 0m && _currentBid is decimal bid && bid - avgPrice >= _limitOffset)
			{
				BuyMarket(volume);
				LogInfo($"Closed short on loss cap. Bid={bid:0.#####}");
			}
		}
	}
}
