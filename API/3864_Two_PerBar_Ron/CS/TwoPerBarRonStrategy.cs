namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Recreates the MetaTrader "TwoPerBar" expert by opening both a long and a short order every new bar.
/// Uses martingale-style position sizing and closes individual legs once the target profit (in points) is reached.
/// </summary>
public class TwoPerBarRonStrategy : Strategy
{
	private sealed class HedgeLeg
	{
		public bool IsLong;
		public decimal TargetVolume;
		public decimal ActiveVolume;
		public decimal AveragePrice;
		public Order EntryOrder;
		public Order ExitOrder;
	}

	private readonly List<HedgeLeg> _legs = new();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _profitTargetPoints;

	private decimal _currentVolume;
	private decimal _pointSize;
	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _hasBid;
	private bool _hasAsk;

	/// <summary>
	/// Initializes a new instance of <see cref="TwoPerBarRonStrategy"/>.
	/// </summary>
	public TwoPerBarRonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe that detects new bars.", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial lot size used when no previous trades exist.", "Trading")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Factor applied after an unfinished cycle closes.", "Trading")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 12.8m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Volume", "Hard upper limit for the martingale lot size.", "Risk")
			.SetCanOptimize(true);

		_profitTargetPoints = Param(nameof(ProfitTargetPoints), 19m)
			.SetNotNegative()
			.SetDisplay("Profit Target (points)", "Monetary target converted using instrument point size.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type that drives the once-per-bar logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base lot size for fresh cycles.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Martingale factor applied after a losing cycle.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Hard cap for the computed volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Target profit expressed in points (money units in MetaTrader terminology).
	/// </summary>
	public decimal ProfitTargetPoints
	{
		get => _profitTargetPoints.Value;
		set => _profitTargetPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_legs.Clear();
		_currentVolume = 0m;
		_pointSize = 0m;
		_bestBid = 0m;
		_bestAsk = 0m;
		_hasBid = false;
		_hasAsk = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		_currentVolume = NormalizeVolume(BaseVolume);

		if (_currentVolume > 0m)
			Volume = _currentVolume;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message is null)
			return;

		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue != null)
		{
			_bestBid = (decimal)bidValue;
			_hasBid = true;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue != null)
		{
			_bestAsk = (decimal)askValue;
			_hasAsk = true;
		}

		CheckProfitTargets();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle is null || candle.State != CandleStates.Finished)
			return;

		CheckProfitTargets();

		var hadLegs = _legs.Count > 0;
		if (hadLegs)
			CloseAllLegs();

		var nextVolume = hadLegs ? _currentVolume * VolumeMultiplier : BaseVolume;
		nextVolume = ApplyVolumeLimits(nextVolume);
		nextVolume = NormalizeVolume(nextVolume);

		if (nextVolume <= 0m)
		{
			LogWarning("Calculated volume is not tradable. Skipping this bar.");
			return;
		}

		_currentVolume = nextVolume;
		Volume = nextVolume;
		_pointSize = CalculatePointSize();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		OpenHedgePair(candle.ClosePrice);
	}

	private void CheckProfitTargets()
	{
		if (_legs.Count == 0)
			return;

		if (ProfitTargetPoints <= 0m || _pointSize <= 0m)
			return;

		var targetOffset = ProfitTargetPoints * _pointSize;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (leg.ActiveVolume <= 0m || leg.AveragePrice <= 0m)
				continue;

			if (leg.IsLong)
			{
				if (!_hasBid)
					continue;

				if (_bestBid - leg.AveragePrice >= targetOffset)
					CloseLeg(i);
			}
			else
			{
				if (!_hasAsk)
					continue;

				if (leg.AveragePrice - _bestAsk >= targetOffset)
					CloseLeg(i);
			}
		}
	}

	private void CloseAllLegs()
	{
		for (var i = _legs.Count - 1; i >= 0; i--)
			CloseLeg(i);
	}

	private void CloseLeg(int index)
	{
		if (index < 0 || index >= _legs.Count)
			return;

		var leg = _legs[index];
		var volume = leg.ActiveVolume;

		if (volume <= 0m)
		{
			_legs.RemoveAt(index);
			return;
		}

		if (leg.IsLong)
		{
			leg.ExitOrder = SellMarket(volume);
		}
		else
		{
			leg.ExitOrder = BuyMarket(volume);
		}

		leg.ActiveVolume = 0m;
	}

	private void OpenHedgePair(decimal referencePrice)
	{
		var volume = _currentVolume;
		if (volume <= 0m)
			return;

		var longLeg = new HedgeLeg
		{
			IsLong = true,
			TargetVolume = volume,
			ActiveVolume = 0m,
			AveragePrice = referencePrice,
			EntryOrder = BuyMarket(volume)
		};

		var shortLeg = new HedgeLeg
		{
			IsLong = false,
			TargetVolume = volume,
			ActiveVolume = 0m,
			AveragePrice = referencePrice,
			EntryOrder = SellMarket(volume)
		};

		_legs.Clear();
		_legs.Add(longLeg);
		_legs.Add(shortLeg);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order is null || trade.Trade is null)
			return;

		var order = trade.Order;
		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
			var leg = _legs[i];

			if (ReferenceEquals(order, leg.EntryOrder))
			{
				UpdateEntry(leg, price, volume);
				return;
			}

			if (ReferenceEquals(order, leg.ExitOrder))
			{
				_legs.RemoveAt(i);
				return;
			}
		}
	}

	private static void UpdateEntry(HedgeLeg leg, decimal fillPrice, decimal fillVolume)
	{
		if (leg is null || fillVolume <= 0m)
			return;

		var remaining = leg.TargetVolume - leg.ActiveVolume;
		if (remaining <= 0m)
			return;

		var appliedVolume = Math.Min(fillVolume, remaining);
		if (appliedVolume <= 0m)
			return;

		var weightedPrice = leg.AveragePrice * leg.ActiveVolume + fillPrice * appliedVolume;
		var newActive = leg.ActiveVolume + appliedVolume;

		if (newActive > 0m)
			leg.AveragePrice = weightedPrice / newActive;

		leg.ActiveVolume = newActive;

		if (leg.ActiveVolume >= leg.TargetVolume)
			leg.EntryOrder = null;
	}

	private decimal ApplyVolumeLimits(decimal candidate)
	{
		if (candidate <= 0m)
			return 0m;

		var limited = candidate;

		if (MaxVolume > 0m && limited > MaxVolume)
			limited = MaxVolume;

		var security = Security;
		if (security?.MaxVolume is decimal maxVolume && maxVolume > 0m && limited > maxVolume)
			limited = maxVolume;

		return limited;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var normalized = decimal.Round(volume, 2, MidpointRounding.ToZero);

		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 0m;
			if (step > 0m)
				normalized = step * Math.Floor(normalized / step);

			var min = security.MinVolume ?? 0m;
			if (min > 0m && normalized < min)
				return 0m;
		}

		return normalized > 0m ? normalized : 0m;
	}

	private decimal CalculatePointSize()
	{
		var security = Security;

		if (security?.PriceStep is decimal step && step > 0m)
			return step;

		if (security?.MinPriceStep is decimal minStep && minStep > 0m)
			return minStep;

		return 0m;
	}
}

