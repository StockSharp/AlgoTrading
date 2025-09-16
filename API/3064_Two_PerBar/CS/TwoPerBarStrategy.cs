namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that opens a hedged pair of market orders on every new bar.
/// </summary>
public class TwoPerBarStrategy : Strategy
{
	private sealed class HedgeLeg
	{
		public bool IsLong;
		public decimal Volume;
		public decimal EntryPrice;
		public decimal? TakeProfitPrice;
	}

	private readonly List<HedgeLeg> _legs = new();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _takeProfitPoints;

	private decimal _pointSize;
	private decimal _lastCycleVolume;

	/// <summary>
	/// Initializes a new instance of <see cref="TwoPerBarStrategy"/>.
	/// </summary>
	public TwoPerBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used to detect new bars.", "General");

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Lot size used when no previous positions exist.", "Trading")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Factor applied to the heaviest remaining leg after closing a cycle.", "Trading")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Volume", "Upper limit for the calculated lot size before resetting to the initial value.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Distance to the take profit expressed in instrument points.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type that drives the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base lot size for a fresh cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous maximum lot size.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Hard limit for the calculated lot size.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
		_lastCycleVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointSize = CalculatePointSize();
		_lastCycleVolume = PrepareVolume(InitialVolume);

		if (_lastCycleVolume > 0m)
		Volume = _lastCycleVolume;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		CheckTakeProfitHits(candle);

		var hadLegs = _legs.Count > 0;
		var maxVolume = 0m;

		for (var i = 0; i < _legs.Count; i++)
		{
		var leg = _legs[i];

		if (leg.Volume > maxVolume)
		maxVolume = leg.Volume;
		}

		if (_legs.Count > 0)
		CloseAllLegs();

		var nextVolume = hadLegs ? maxVolume * VolumeMultiplier : InitialVolume;
		nextVolume = PrepareVolume(nextVolume);

		if (nextVolume <= 0m)
		return;

		_lastCycleVolume = nextVolume;
		Volume = nextVolume;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var offset = TakeProfitPoints > 0 && _pointSize > 0m ? TakeProfitPoints * _pointSize : 0m;
		OpenHedgePair(candle.ClosePrice, offset);
	}

	private void CheckTakeProfitHits(ICandleMessage candle)
	{
		if (TakeProfitPoints <= 0)
		return;

		for (var i = _legs.Count - 1; i >= 0; i--)
		{
		var leg = _legs[i];
		var target = leg.TakeProfitPrice;

		if (target is null)
		continue;

		if (leg.IsLong)
		{
		if (candle.HighPrice >= target.Value)
		{
		SellMarket(leg.Volume);
		_legs.RemoveAt(i);
		}
		}
		else
		{
		if (candle.LowPrice <= target.Value)
		{
		BuyMarket(leg.Volume);
		_legs.RemoveAt(i);
		}
		}
		}
	}

	private void CloseAllLegs()
	{
		for (var i = _legs.Count - 1; i >= 0; i--)
		{
		var leg = _legs[i];

		if (leg.IsLong)
		SellMarket(leg.Volume);
		else
		BuyMarket(leg.Volume);
		}

		_legs.Clear();
	}

	private void OpenHedgePair(decimal entryPrice, decimal takeProfitOffset)
	{
		var volume = _lastCycleVolume;
		if (volume <= 0m)
		return;

		var longOrder = BuyMarket(volume);
		if (longOrder is not null)
		{
		_legs.Add(new HedgeLeg
		{
		IsLong = true,
		Volume = volume,
		EntryPrice = entryPrice,
		TakeProfitPrice = takeProfitOffset > 0m ? entryPrice + takeProfitOffset : null
		});
		}

		var shortOrder = SellMarket(volume);
		if (shortOrder is not null)
		{
		_legs.Add(new HedgeLeg
		{
		IsLong = false,
		Volume = volume,
		EntryPrice = entryPrice,
		TakeProfitPrice = takeProfitOffset > 0m ? entryPrice - takeProfitOffset : null
		});
		}
	}

	private decimal PrepareVolume(decimal candidate)
	{
		if (candidate <= 0m)
		return 0m;

		if (ShouldResetVolume(candidate))
		candidate = InitialVolume;

		var normalized = NormalizeVolume(candidate);

		if (normalized <= 0m)
		return 0m;

		if (ShouldResetVolume(normalized))
		normalized = NormalizeVolume(InitialVolume);

		return normalized;
	}

	private bool ShouldResetVolume(decimal volume)
	{
		if (volume <= 0m)
		return false;

		if (MaxVolume > 0m && volume > MaxVolume)
		return true;

		var security = Security;
		var maxFromSecurity = security?.MaxVolume;

		return maxFromSecurity != null && volume > maxFromSecurity.Value;
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

		var max = security.MaxVolume;
		if (max != null && normalized > max.Value)
		normalized = max.Value;
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
