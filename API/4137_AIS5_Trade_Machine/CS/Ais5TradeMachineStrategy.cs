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
/// AIS5 Trade Machine inspired strategy.
/// Builds higher timeframe volume zones and trades lower timeframe breakouts with ATR-based risk control.
/// </summary>
public class Ais5TradeMachineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _profileCandleType;
	private readonly StrategyParam<DataType> _tradingCandleType;
	private readonly StrategyParam<int> _volumeLookback;
	private readonly StrategyParam<decimal> _strongVolumeMultiplier;
	private readonly StrategyParam<decimal> _weakVolumeDivider;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _zoneBasePoints;
	private readonly StrategyParam<int> _zoneStepPoints;

	private SMA _profileVolumeAverage = null!;
	private SMA _tradingVolumeAverage = null!;
	private AverageTrueRange _atr = null!;

	private decimal _latestStrongLevel;
	private decimal _latestWeakLevel;
	private decimal _strongLevelVolume;
	private decimal _weakLevelVolume;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	private decimal _priceStep;

	/// <summary>
	/// Higher timeframe candle type used to build the volume profile.
	/// </summary>
	public DataType ProfileCandleType
	{
		get => _profileCandleType.Value;
		set => _profileCandleType.Value = value;
	}

	/// <summary>
	/// Lower timeframe candle type used for entries and exits.
	/// </summary>
	public DataType TradingCandleType
	{
		get => _tradingCandleType.Value;
		set => _tradingCandleType.Value = value;
	}

	/// <summary>
	/// Number of candles used to average volume on both timeframes.
	/// </summary>
	public int VolumeLookback
	{
		get => _volumeLookback.Value;
		set => _volumeLookback.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average volume to tag strong zones.
	/// </summary>
	public decimal StrongVolumeMultiplier
	{
		get => _strongVolumeMultiplier.Value;
		set => _strongVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Divider applied to the average volume to tag weak zones and exit on contractions.
	/// </summary>
	public decimal WeakVolumeDivider
	{
		get => _weakVolumeDivider.Value;
		set => _weakVolumeDivider.Value = value;
	}

	/// <summary>
	/// ATR multiplier that defines the adaptive stop buffer.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Base offset from a zone level expressed in points.
	/// </summary>
	public int ZoneBasePoints
	{
		get => _zoneBasePoints.Value;
		set => _zoneBasePoints.Value = value;
	}

	/// <summary>
	/// Additional breakout buffer expressed in points.
	/// </summary>
	public int ZoneStepPoints
	{
		get => _zoneStepPoints.Value;
		set => _zoneStepPoints.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Ais5TradeMachineStrategy"/>.
	/// </summary>
	public Ais5TradeMachineStrategy()
	{
		_profileCandleType = Param(nameof(ProfileCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Profile Candle", "Higher timeframe used to aggregate volume", "General");

		_tradingCandleType = Param(nameof(TradingCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Trading Candle", "Lower timeframe that drives entries", "General");

		_volumeLookback = Param(nameof(VolumeLookback), 20)
		.SetGreaterThanZero()
		.SetDisplay("Volume Lookback", "Number of candles used for average volume", "Volume")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 10);

		_strongVolumeMultiplier = Param(nameof(StrongVolumeMultiplier), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Strong Volume Mult", "Multiplier above average volume that marks strong zones", "Volume")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 3.0m, 0.5m);

		_weakVolumeDivider = Param(nameof(WeakVolumeDivider), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("Weak Volume Divider", "Divider below average volume that marks weak zones", "Volume")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 4.0m, 0.5m);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3.0m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for stop distance", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 5.0m, 0.5m);

		_zoneBasePoints = Param(nameof(ZoneBasePoints), 50)
		.SetGreaterThanZero()
		.SetDisplay("Zone Base Points", "Initial offset from the zone level in points", "Zones")
		.SetCanOptimize(true)
		.SetOptimize(10, 150, 10);

		_zoneStepPoints = Param(nameof(ZoneStepPoints), 100)
		.SetGreaterThanZero()
		.SetDisplay("Zone Step Points", "Additional breakout buffer in points", "Zones")
		.SetCanOptimize(true)
		.SetOptimize(20, 300, 20);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TradingCandleType);

		if (ProfileCandleType != TradingCandleType)
		yield return (Security, ProfileCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_profileVolumeAverage = null!;
		_tradingVolumeAverage = null!;
		_atr = null!;

		_latestStrongLevel = 0m;
		_latestWeakLevel = 0m;
		_strongLevelVolume = 0m;
		_weakLevelVolume = 0m;

		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;

		_priceStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();

		_profileVolumeAverage = new SMA { Length = Math.Max(1, VolumeLookback) };
		_tradingVolumeAverage = new SMA { Length = Math.Max(1, VolumeLookback) };
		_atr = new AverageTrueRange { Length = Math.Max(1, VolumeLookback) };

		var profileSubscription = SubscribeCandles(ProfileCandleType);
		profileSubscription
		.Bind(ProcessProfileCandle)
		.Start();

		var tradingSubscription = SubscribeCandles(TradingCandleType);
		tradingSubscription
		.Bind(_atr, ProcessTradingCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
		}
		else if (Position < 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ProcessProfileCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var volume = candle.TotalVolume ?? 0m;
		var averageValue = _profileVolumeAverage
		.Process(volume, candle.OpenTime, true)
		.ToDecimal();

		if (!_profileVolumeAverage.IsFormed || averageValue <= 0m)
		return;

		var strongThreshold = averageValue * StrongVolumeMultiplier;
		var weakThreshold = averageValue / Math.Max(WeakVolumeDivider, 1m);

		if (volume >= strongThreshold)
		{
			_latestStrongLevel = candle.ClosePrice;
			_strongLevelVolume = volume;
			LogInfo($"Strong zone updated at {_latestStrongLevel:F5} with volume {volume:F0} (avg {averageValue:F0}).");
		}
		else if (volume <= weakThreshold)
		{
			_latestWeakLevel = candle.ClosePrice;
			_weakLevelVolume = volume;
			LogInfo($"Weak zone updated at {_latestWeakLevel:F5} with volume {volume:F0} (avg {averageValue:F0}).");
		}
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var volume = candle.TotalVolume ?? 0m;
		var volumeAverageValue = _tradingVolumeAverage
		.Process(volume, candle.OpenTime, true)
		.ToDecimal();

		var hasVolumeContext = _tradingVolumeAverage.IsFormed;
		var hasAtrContext = _atr.IsFormed;

		if (!hasVolumeContext || !hasAtrContext)
		return;

		var baseBuffer = ZoneBasePoints * _priceStep;
		var breakoutBuffer = ZoneStepPoints * _priceStep;
		var atrBuffer = atrValue * AtrMultiplier;
		var dynamicBuffer = Math.Max(baseBuffer, atrBuffer);
		var breakoutOffset = dynamicBuffer + breakoutBuffer;

		var highVolumeConfirmation = volumeAverageValue <= 0m || volume >= volumeAverageValue * StrongVolumeMultiplier;
		var lowVolumeExit = volumeAverageValue > 0m && volume <= volumeAverageValue / Math.Max(WeakVolumeDivider, 1m);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position == 0m && _latestStrongLevel > 0m)
		{
			var breakoutPrice = _latestStrongLevel + breakoutOffset;
			if (candle.ClosePrice >= breakoutPrice && highVolumeConfirmation)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - dynamicBuffer;
				_targetPrice = _entryPrice + dynamicBuffer * Math.Max(1m, WeakVolumeDivider);
				LogInfo($"Entered long at {_entryPrice:F5} after breaking {_latestStrongLevel:F5} with ATR {atrValue:F5} and volume {volume:F0} (avg {volumeAverageValue:F0}).");
			}
		}

		if (Position == 0m && _latestWeakLevel > 0m)
		{
			var breakdownPrice = _latestWeakLevel - breakoutOffset;
			if (candle.ClosePrice <= breakdownPrice && highVolumeConfirmation)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + dynamicBuffer;
				_targetPrice = _entryPrice - dynamicBuffer * Math.Max(1m, WeakVolumeDivider);
				LogInfo($"Entered short at {_entryPrice:F5} after breaking {_latestWeakLevel:F5} with ATR {atrValue:F5} and volume {volume:F0} (avg {volumeAverageValue:F0}).");
			}
		}

		if (Position > 0m)
		{
			var hitStop = candle.LowPrice <= _stopPrice;
			var hitTarget = candle.HighPrice >= _targetPrice;

			if (hitStop || hitTarget || lowVolumeExit)
			{
				SellMarket(Math.Abs(Position));
				var reason = hitStop ? "stop" : hitTarget ? "target" : "volume contraction";
				LogInfo($"Exited long at {candle.ClosePrice:F5} due to {reason}.");
			}
		}
		else if (Position < 0m)
		{
			var hitStop = candle.HighPrice >= _stopPrice;
			var hitTarget = candle.LowPrice <= _targetPrice;

			if (hitStop || hitTarget || lowVolumeExit)
			{
				BuyMarket(Math.Abs(Position));
				var reason = hitStop ? "stop" : hitTarget ? "target" : "volume contraction";
				LogInfo($"Exited short at {candle.ClosePrice:F5} due to {reason}.");
			}
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
		return step;

		return 0.0001m;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}
}
