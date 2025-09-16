using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Randomized entry strategy that mimics the MetaTrader "New Random" expert.
/// </summary>
public class NewRandomStrategy : Strategy
{
	/// <summary>
	/// Available direction selection modes.
	/// </summary>
	public enum RandomMode
	{
		/// <summary>
		/// Use a pseudo random generator for every entry decision.
		/// </summary>
		Generator,

		/// <summary>
		/// Alternate strictly between buy and sell starting from a buy.
		/// </summary>
		BuySellBuy,

		/// <summary>
		/// Alternate strictly between sell and buy starting from a sell.
		/// </summary>
		SellBuySell
	}

	private readonly StrategyParam<RandomMode> _mode;
	private readonly StrategyParam<int> _minimalLotCount;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private Random _random;
	private decimal _lastBid;
	private decimal _lastAsk;
	private decimal _lastTradePrice;
	private bool _hasLastTradePrice;
	private decimal _pipValue;
	private bool _pendingEntry;
	private bool _pendingExit;
	private Sides? _sequenceLastSide;
	private Sides? _positionSide;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Gets or sets the direction selection mode.
	/// </summary>
	public RandomMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Gets or sets the multiplier for the minimum tradable volume.
	/// </summary>
	public int MinimalLotCount
	{
		get => _minimalLotCount.Value;
		set => _minimalLotCount.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss distance measured in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit distance measured in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NewRandomStrategy"/> class.
	/// </summary>
	public NewRandomStrategy()
	{
		_mode = Param(nameof(Mode), RandomMode.Generator)
		.SetDisplay("Random Mode", "Direction selection mode", "General");
		_minimalLotCount = Param(nameof(MinimalLotCount), 1)
		.SetGreaterThanZero()
		.SetDisplay("Minimal Lot Count", "Multiplier for the minimum tradable volume", "Trading");
		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance measured in pips", "Risk Management");
		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
		.SetDisplay("Take Profit (pips)", "Take-profit distance measured in pips", "Risk Management");
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

		_random = null;
		_lastBid = 0m;
		_lastAsk = 0m;
		_lastTradePrice = 0m;
		_hasLastTradePrice = false;
		_pipValue = 0m;
		_pendingEntry = false;
		_pendingExit = false;
		_sequenceLastSide = null;
		_positionSide = null;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize random generator only when needed.
		_random = Mode == RandomMode.Generator ? new Random(Environment.TickCount) : null;

		// Prepare alternation state for sequence modes.
		_sequenceLastSide = Mode switch
		{
			RandomMode.BuySellBuy => Sides.Sell,
			RandomMode.SellBuySell => Sides.Buy,
			_ => null
		};

		var step = Security.Step;
		if (step <= 0m)
			step = 1m;

		var pipFactor = (Security.Decimals == 3 || Security.Decimals == 5) ? 10m : 1m;
		_pipValue = step * pipFactor;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		// Store the latest best bid/ask and last trade price snapshots.
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;

		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
		{
			_lastTradePrice = (decimal)last;
			_hasLastTradePrice = _lastTradePrice > 0m;
		}

		// Detect fills of pending entry orders.
		if (_pendingEntry && Position != 0)
		{
			_positionSide = Position > 0 ? Sides.Buy : Sides.Sell;
			_pendingEntry = false;
		}

		// Detect completion of exit orders.
		if (_pendingExit && Position == 0)
		{
			_pendingExit = false;
			_positionSide = null;
			_entryPrice = 0m;
			_stopPrice = null;
			_takePrice = null;
		}

		// When flat try to initiate a new trade.
		if (Position == 0 && !_pendingEntry && !_pendingExit)
		{
			_positionSide = null;
			_entryPrice = 0m;
			_stopPrice = null;
			_takePrice = null;

			if (IsFormedAndOnlineAndAllowTrading())
				TryEnterPosition();

			return;
		}

		if (_positionSide == null || !IsFormedAndOnlineAndAllowTrading())
		return;

		ManagePosition();
	}

	private void TryEnterPosition()
	{
		var side = DetermineNextSide();
		if (side is null)
		return;

		var entryPrice = side == Sides.Buy ? _lastAsk : _lastBid;

		if (entryPrice <= 0m)
		{
			if (_hasLastTradePrice)
			{
				entryPrice = _lastTradePrice;
			}
			else
			{
				return;
			}
		}

		var volume = CalculateVolume();
		if (volume <= 0m)
		return;

		_entryPrice = entryPrice;
		_stopPrice = StopLossPips > 0
			? side == Sides.Buy
				? entryPrice - _pipValue * StopLossPips
				: entryPrice + _pipValue * StopLossPips
			: null;
		_takePrice = TakeProfitPips > 0
			? side == Sides.Buy
				? entryPrice + _pipValue * TakeProfitPips
				: entryPrice - _pipValue * TakeProfitPips
			: null;

		// Submit market order in the selected direction.
		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_pendingEntry = true;

		if (Mode != RandomMode.Generator)
			_sequenceLastSide = side;
	}

	private decimal CalculateVolume()
	{
		decimal? minVolume = Security.VolumeMin;

		if (minVolume is null or <= 0m)
			minVolume = Security.VolumeStep;

		if (minVolume is null or <= 0m)
			minVolume = Volume;

		if (minVolume is null or <= 0m)
			minVolume = 1m;

		return MinimalLotCount * minVolume.Value;
	}

	private void ManagePosition()
	{
		var price = GetCurrentPrice();
		if (price <= 0m)
		return;

		if (_positionSide == Sides.Buy && Position > 0 && !_pendingExit)
		{
			if (_stopPrice.HasValue && price <= _stopPrice.Value)
			{
				_pendingExit = true;
				SellMarket(Position);
				return;
			}

			if (_takePrice.HasValue && price >= _takePrice.Value)
			{
				_pendingExit = true;
				SellMarket(Position);
				return;
			}
		}
		else if (_positionSide == Sides.Sell && Position < 0 && !_pendingExit)
		{
			if (_stopPrice.HasValue && price >= _stopPrice.Value)
			{
				_pendingExit = true;
				BuyMarket(-Position);
				return;
			}

			if (_takePrice.HasValue && price <= _takePrice.Value)
			{
				_pendingExit = true;
				BuyMarket(-Position);
			}
		}
	}

	private decimal GetCurrentPrice()
	{
		if (_hasLastTradePrice && _lastTradePrice > 0m)
			return _lastTradePrice;

		if (_positionSide == Sides.Buy)
			return _lastBid > 0m ? _lastBid : _lastAsk;

		if (_positionSide == Sides.Sell)
			return _lastAsk > 0m ? _lastAsk : _lastBid;

		return 0m;
	}

	private Sides? DetermineNextSide()
	{
		return Mode switch
		{
			RandomMode.Generator => (_random?.Next(2) ?? 0) == 0 ? Sides.Buy : Sides.Sell,
			RandomMode.BuySellBuy => _sequenceLastSide == Sides.Buy ? Sides.Sell : Sides.Buy,
			RandomMode.SellBuySell => _sequenceLastSide == Sides.Sell ? Sides.Buy : Sides.Sell,
			_ => Sides.Buy
		};
	}
}
