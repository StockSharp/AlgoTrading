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
/// Hedging strategy converted from the "Hedge any positions" MQL5 expert.
/// It opens an opposite trade once the current position loses a configurable number of pips.
/// </summary>
public class HedgeAnyPositionsStrategy : Strategy
{
	private sealed class HedgeLeg
	{
		public bool IsLong;
		public decimal EntryPrice;
		public decimal Volume;
		public bool IsHedged;
	}

	private readonly List<HedgeLeg> _legs = new();

	private readonly StrategyParam<int> _losingPips;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<bool> _autoPlaceInitialTrade;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<Sides> _initialDirection;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private bool _initialTradePlaced;

	/// <summary>
	/// Losing distance in pips required before a hedge order is triggered.
	/// </summary>
	public int LosingPips
	{
		get => _losingPips.Value;
		set => _losingPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the original lot size when creating the hedge.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Enables automatic placement of the very first trade when the strategy starts.
	/// </summary>
	public bool AutoPlaceInitialTrade
	{
		get => _autoPlaceInitialTrade.Value;
		set => _autoPlaceInitialTrade.Value = value;
	}

	/// <summary>
	/// Volume for the optional initial trade placed at start-up.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Direction for the optional initial trade.
	/// </summary>
	public Sides InitialDirection
	{
		get => _initialDirection.Value;
		set => _initialDirection.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate price movements.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HedgeAnyPositionsStrategy"/> class.
	/// </summary>
	public HedgeAnyPositionsStrategy()
	{
		_losingPips = Param(nameof(LosingPips), 5)
			.SetGreaterThanZero()
			.SetDisplay("Losing Distance (pips)", "Adverse move in pips required before hedging", "Risk")
			.SetCanOptimize(true);

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier applied to the hedging order volume", "Risk")
			.SetCanOptimize(true);

		_autoPlaceInitialTrade = Param(nameof(AutoPlaceInitialTrade), false)
			.SetDisplay("Auto Place Initial Trade", "Automatically send the first order on start", "General");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Volume for the optional first order", "Trading")
			.SetCanOptimize(true);

		_initialDirection = Param(nameof(InitialDirection), Sides.Buy)
			.SetDisplay("Initial Direction", "Direction used by the optional first order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series driving the hedging checks", "Data");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		TryPlaceInitialTrade(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var lossDistance = LosingPips * _pipSize;

		if (lossDistance <= 0m)
			return;

		EvaluateHedges(candle.ClosePrice, lossDistance);
	}

	private void TryPlaceInitialTrade(ICandleMessage candle)
	{
		if (!AutoPlaceInitialTrade || _initialTradePlaced)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = AdjustVolume(InitialVolume);

		if (volume <= 0m)
			return;

		var order = InitialDirection == Sides.Buy
			? BuyMarket(volume)
			: SellMarket(volume);

		if (order is null)
			return;

		_initialTradePlaced = true;

		_legs.Add(new HedgeLeg
		{
			IsLong = InitialDirection == Sides.Buy,
			EntryPrice = candle.ClosePrice,
			Volume = volume,
			IsHedged = false
		});
	}

	private void EvaluateHedges(decimal currentPrice, decimal lossDistance)
	{
		var initialCount = _legs.Count;

		for (var i = 0; i < initialCount; i++)
		{
			var leg = _legs[i];

			if (leg.IsHedged)
				continue;

			if (leg.IsLong)
			{
				if (leg.EntryPrice - currentPrice < lossDistance)
					continue;

				var hedgeVolume = AdjustVolume(leg.Volume * LotCoefficient);

				if (hedgeVolume <= 0m)
					continue;

				if (SellMarket(hedgeVolume) is null)
					continue;

				leg.IsHedged = true;

				_legs.Add(new HedgeLeg
				{
					IsLong = false,
					EntryPrice = currentPrice,
					Volume = hedgeVolume,
					IsHedged = false
				});
			}
			else
			{
				if (currentPrice - leg.EntryPrice < lossDistance)
					continue;

				var hedgeVolume = AdjustVolume(leg.Volume * LotCoefficient);

				if (hedgeVolume <= 0m)
					continue;

				if (BuyMarket(hedgeVolume) is null)
					continue;

				leg.IsHedged = true;

				_legs.Add(new HedgeLeg
				{
					IsLong = true,
					EntryPrice = currentPrice,
					Volume = hedgeVolume,
					IsHedged = false
				});
			}
		}
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;

		if (step > 0m)
			volume = step * Math.Floor(volume / step);

		var minVolume = Security.MinVolume ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = Security.MaxVolume;

		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals ?? GetDecimalsFromStep(step);
		var factor = decimals == 3 || decimals == 5 ? 10m : 1m;

		return step * factor;
	}

	private static int GetDecimalsFromStep(decimal step)
	{
		if (step <= 0m)
			return 0;

		var value = Math.Abs(Math.Log10((double)step));
		return (int)Math.Round(value);
	}

	private void ResetState()
	{
		_legs.Clear();
		_initialTradePlaced = false;
	}
}

