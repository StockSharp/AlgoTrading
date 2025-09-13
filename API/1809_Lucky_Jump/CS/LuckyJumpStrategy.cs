using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal strategy that reacts to sudden price jumps.
/// Sells after an upward spike and buys after a downward spike.
/// Exits on any profit or when loss exceeds a fixed limit.
/// </summary>
public class LuckyJumpStrategy : Strategy
{
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<int> _limit;
	private readonly StrategyParam<decimal> _volume;

	private decimal _prevAsk;
	private decimal _prevBid;
	private decimal _entryPrice;
	private bool _isFirstTick;
	private decimal _priceStep;

	/// <summary>
	/// Price jump in points required to open a position.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Maximum loss in points before closing the position.
	/// </summary>
	public int Limit
	{
		get => _limit.Value;
		set => _limit.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="LuckyJumpStrategy"/>.
	/// </summary>
	public LuckyJumpStrategy()
	{
		_shift = Param(nameof(Shift), 30)
			.SetGreaterThanZero()
			.SetDisplay("Shift", "Price jump in points to trigger entry", "Trading");

		_limit = Param(nameof(Limit), 180)
			.SetGreaterThanZero()
			.SetDisplay("Loss Limit", "Maximum loss in points before exit", "Risk");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAsk = 0m;
		_prevBid = 0m;
		_entryPrice = 0m;
		_isFirstTick = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Determine minimal price increment.
		_priceStep = Security.PriceStep ?? 1m;

		// Subscribe to level1 quotes for bid/ask updates.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		// Extract current best ask and bid prices.
		if (!level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) ||
			!level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			return;

		var ask = (decimal)askObj;
		var bid = (decimal)bidObj;

		// Initialize previous values on the first tick.
		if (_isFirstTick)
		{
			_prevAsk = ask;
			_prevBid = bid;
			_isFirstTick = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			// For long positions close on profit or if loss exceeds limit.
			if (bid > _entryPrice || (_entryPrice - ask) >= Limit * _priceStep)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			// For short positions close on profit or if loss exceeds limit.
			if (ask < _entryPrice || (bid - _entryPrice) >= Limit * _priceStep)
				BuyMarket(-Position);
		}
		else
		{
			// Open short if price jumped up.
			if (ask - _prevAsk >= Shift * _priceStep)
			{
				SellMarket(Volume);
				_entryPrice = ask;
			}
			// Open long if price dropped down.
			else if (_prevBid - bid >= Shift * _priceStep)
			{
				BuyMarket(Volume);
				_entryPrice = bid;
			}
		}

		// Remember current prices for next tick comparison.
		_prevAsk = ask;
		_prevBid = bid;
	}
}
