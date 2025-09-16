using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedging grid strategy converted from the Frank Ud MetaTrader expert.
/// The strategy opens both long and short positions and then adds martingale orders in the
/// direction that remains after a stop or take profit is triggered.
/// </summary>
public class FrankUdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<bool> _autoLot;
	private readonly StrategyParam<decimal> _lotSize;

	private readonly List<PositionEntry> _longPositions = new();
	private readonly List<PositionEntry> _shortPositions = new();

	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;
	private decimal _stepDistance;
	private decimal _baseVolume;
	private decimal _multiplier = 1m;
	private decimal _lastBid;
	private decimal _lastAsk;
	private decimal? _longTakeProfit;
	private decimal? _longStopLoss;
	private decimal? _shortTakeProfit;
	private decimal? _shortStopLoss;

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Price step that triggers new martingale entries.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Enables usage of the LotSize parameter instead of the minimum contract volume.
	/// </summary>
	public bool AutoLot
	{
		get => _autoLot.Value;
		set => _autoLot.Value = value;
	}

	/// <summary>
	/// Base lot size when AutoLot is enabled.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public FrankUdStrategy()
	{
		_takeProfitPips = Param(nameof(TakeProfitPips), 12m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 12m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetGreaterThanZero();

		_stepPips = Param(nameof(StepPips), 16m)
			.SetDisplay("Step (pips)", "Distance in pips between martingale entries", "Grid")
			.SetGreaterThanZero();

		_autoLot = Param(nameof(AutoLot), true)
			.SetDisplay("Use custom lot", "Use the lot size parameter instead of minimum volume", "Risk");

		_lotSize = Param(nameof(LotSize), 0.5m)
			.SetDisplay("Lot Size", "Base lot size when AutoLot is enabled", "Risk")
			.SetGreaterThanZero();
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

		_longPositions.Clear();
		_shortPositions.Clear();
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;
		_stepDistance = 0m;
		_baseVolume = 0m;
		_multiplier = 1m;
		_lastBid = 0m;
		_lastAsk = 0m;
		_longTakeProfit = null;
		_longStopLoss = null;
		_shortTakeProfit = null;
		_shortStopLoss = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_multiplier = 1m;

		var priceStep = Security?.PriceStep ?? 1m;
		var pipValue = priceStep * 10m;

		_takeProfitDistance = TakeProfitPips * pipValue;
		_stopLossDistance = StopLossPips * pipValue;
		_stepDistance = StepPips * pipValue;

		_baseVolume = CalculateBaseVolume();

		// Subscribe to Level1 data to drive the strategy from best bid/ask updates.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private decimal CalculateBaseVolume()
	{
		var security = Security;

		decimal baseVolume;

		if (AutoLot)
		{
			baseVolume = LotSize;
		}
		else if (security?.MinVolume != null && security.MinVolume > 0m)
		{
			baseVolume = security.MinVolume.Value;
		}
		else if (security?.VolumeStep != null && security.VolumeStep > 0m)
		{
			baseVolume = security.VolumeStep.Value;
		}
		else
		{
			baseVolume = Volume;
		}

		return AdjustVolume(baseVolume);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var bid = level1.TryGetDecimal(Level1Fields.BestBidPrice);
		if (bid != null)
			_lastBid = bid.Value;

		var ask = level1.TryGetDecimal(Level1Fields.BestAskPrice);
		if (ask != null)
			_lastAsk = ask.Value;

		if (_lastBid <= 0m || _lastAsk <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Check protective levels before making new decisions.
		if (_longPositions.Count > 0 && _longStopLoss is decimal longStop && _lastBid <= longStop)
			CloseLongPositions();

		if (_shortPositions.Count > 0 && _shortStopLoss is decimal shortStop && _lastAsk >= shortStop)
			CloseShortPositions();

		if (_longPositions.Count > 0 && _longTakeProfit is decimal longTp && _lastBid >= longTp)
			CloseLongPositions();

		if (_shortPositions.Count > 0 && _shortTakeProfit is decimal shortTp && _lastAsk <= shortTp)
			CloseShortPositions();

		if (_longPositions.Count == 0 && _shortPositions.Count == 0)
		{
			TryOpenInitialPositions();
			return;
		}

		if (_longPositions.Count == 0 && _shortPositions.Count > 0)
		{
			ManageShortPositions();
		}
		else if (_shortPositions.Count == 0 && _longPositions.Count > 0)
		{
			ManageLongPositions();
		}
	}

	private void TryOpenInitialPositions()
	{
		if (_baseVolume <= 0m)
			return;

		var volume = AdjustVolume(_baseVolume * _multiplier);
		if (volume <= 0m)
			return;

		// Open hedged positions in both directions with identical volume.
		BuyMarket(volume);
		SellMarket(volume);

		_longPositions.Add(new PositionEntry(volume, _lastAsk));
		_shortPositions.Add(new PositionEntry(volume, _lastBid));

		_longTakeProfit = _lastAsk + _takeProfitDistance;
		_longStopLoss = _stopLossDistance > 0m ? _lastAsk - _stopLossDistance : null;

		_shortTakeProfit = _lastBid - _takeProfitDistance;
		_shortStopLoss = _stopLossDistance > 0m ? _lastBid + _stopLossDistance : null;
	}

	private void ManageShortPositions()
	{
		var netPrice = GetAveragePrice(_shortPositions);
		if (netPrice == 0m)
			return;

		// Update common take-profit for the sell basket.
		_shortTakeProfit = netPrice - _takeProfitDistance;
		_shortStopLoss = null;

		if (_lastAsk <= _shortTakeProfit)
		{
			CloseShortPositions();
			return;
		}

		var lastPrice = GetExtremePrice(_shortPositions, false);
		if (lastPrice == 0m)
			return;

		// Add a new sell order if price moved by the configured step.
		if (_lastAsk > lastPrice + _stepDistance)
			AddShortPosition();
	}

	private void ManageLongPositions()
	{
		var netPrice = GetAveragePrice(_longPositions);
		if (netPrice == 0m)
			return;

		// Update common take-profit for the buy basket.
		_longTakeProfit = netPrice + _takeProfitDistance;
		_longStopLoss = null;

		if (_lastBid >= _longTakeProfit)
		{
			CloseLongPositions();
			return;
		}

		var lastPrice = GetExtremePrice(_longPositions, true);
		if (lastPrice == 0m)
			return;

		// Add a new buy order if price moved by the configured step.
		if (_lastBid < lastPrice - _stepDistance)
			AddLongPosition();
	}

	private void AddShortPosition()
	{
		if (_baseVolume <= 0m)
			return;

		var nextMultiplier = _multiplier * 2m;
		var volume = AdjustVolume(_baseVolume * nextMultiplier);
		if (volume <= 0m)
			return;

		_multiplier = volume / _baseVolume;

		// Register the additional sell order and store its entry price.
		SellMarket(volume);
		_shortPositions.Add(new PositionEntry(volume, _lastBid));

		var netPrice = GetAveragePrice(_shortPositions);
		_shortTakeProfit = netPrice == 0m ? null : netPrice - _takeProfitDistance;
	}

	private void AddLongPosition()
	{
		if (_baseVolume <= 0m)
			return;

		var nextMultiplier = _multiplier * 2m;
		var volume = AdjustVolume(_baseVolume * nextMultiplier);
		if (volume <= 0m)
			return;

		_multiplier = volume / _baseVolume;

		// Register the additional buy order and store its entry price.
		BuyMarket(volume);
		_longPositions.Add(new PositionEntry(volume, _lastAsk));

		var netPrice = GetAveragePrice(_longPositions);
		_longTakeProfit = netPrice == 0m ? null : netPrice + _takeProfitDistance;
	}

	private void CloseLongPositions()
	{
		var volume = GetTotalVolume(_longPositions);
		if (volume > 0m)
			SellMarket(volume);

		_longPositions.Clear();
		_longTakeProfit = null;
		_longStopLoss = null;

		if (_shortPositions.Count == 0)
			_multiplier = 1m;
	}

	private void CloseShortPositions()
	{
		var volume = GetTotalVolume(_shortPositions);
		if (volume > 0m)
			BuyMarket(volume);

		_shortPositions.Clear();
		_shortTakeProfit = null;
		_shortStopLoss = null;

		if (_longPositions.Count == 0)
			_multiplier = 1m;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security?.VolumeStep != null && security.VolumeStep > 0m)
		{
			var step = security.VolumeStep.Value;
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		if (security?.MinVolume != null && security.MinVolume > 0m && volume < security.MinVolume.Value)
			volume = security.MinVolume.Value;

		if (security?.MaxVolume != null && security.MaxVolume > 0m && volume > security.MaxVolume.Value)
			volume = security.MaxVolume.Value;

		return volume;
	}

	private static decimal GetAveragePrice(List<PositionEntry> positions)
	{
		decimal totalPriceVolume = 0m;
		decimal totalVolume = 0m;

		foreach (var position in positions)
		{
			totalPriceVolume += position.Price * position.Volume;
			totalVolume += position.Volume;
		}

		return totalVolume == 0m ? 0m : totalPriceVolume / totalVolume;
	}

	private static decimal GetExtremePrice(List<PositionEntry> positions, bool isLong)
	{
		var hasValue = false;
		decimal result = 0m;

		foreach (var position in positions)
		{
			var price = position.Price;

			if (!hasValue)
			{
				result = price;
				hasValue = true;
				continue;
			}

			if (isLong)
			{
				if (price < result)
					result = price;
			}
			else
			{
				if (price > result)
					result = price;
			}
		}

		return hasValue ? result : 0m;
	}

	private static decimal GetTotalVolume(List<PositionEntry> positions)
	{
		decimal total = 0m;

		foreach (var position in positions)
			total += position.Volume;

		return total;
	}

	private readonly struct PositionEntry
	{
		public PositionEntry(decimal volume, decimal price)
		{
			Volume = volume;
			Price = price;
		}

		public decimal Volume { get; }
		public decimal Price { get; }
	}
}
