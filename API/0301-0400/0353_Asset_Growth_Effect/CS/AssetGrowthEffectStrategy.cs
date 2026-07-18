using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Relative asset-growth strategy that fades excessive synthetic balance-sheet expansion in the primary instrument versus the secondary benchmark.
/// </summary>
public class AssetGrowthEffectStrategy : Strategy
{
	private readonly StrategyParam<string> _security2Id;
	private readonly StrategyParam<int> _assetLength;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private Security _security2 = null!;
	private ExponentialMovingAverage _primaryAssetBase = null!;
	private ExponentialMovingAverage _secondaryAssetBase = null!;
	private SimpleMovingAverage _growthSpreadAverage = null!;
	private StandardDeviation _growthSpreadDeviation = null!;
	private decimal _previousPrimaryAssetBase;
	private decimal _previousSecondaryAssetBase;
	private decimal _latestPrimaryGrowth;
	private decimal _latestSecondaryGrowth;
	private bool _primaryUpdated;
	private bool _secondaryUpdated;
	private decimal? _previousZScore;
	private int _cooldownRemaining;

	/// <summary>
	/// Secondary security identifier.
	/// </summary>
	public string Security2Id
	{
		get => _security2Id.Value;
		set => _security2Id.Value = value;
	}

	/// <summary>
	/// Smoothing length for the synthetic asset base.
	/// </summary>
	public int AssetLength
	{
		get => _assetLength.Value;
		set => _assetLength.Value = value;
	}

	/// <summary>
	/// Lookback period used to normalize growth spread.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to open a position.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Z-score threshold required to close a position.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type used for both instruments.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AssetGrowthEffectStrategy()
	{
		_security2Id = Param(nameof(Security2Id), Paths.HistoryDefaultSecurity2)
			.SetDisplay("Second Security Id", "Identifier of the secondary benchmark security", "General");

		_assetLength = Param(nameof(AssetLength), 8)
			.SetRange(2, 40)
			.SetDisplay("Asset Length", "Smoothing length for the synthetic asset base", "Indicators");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 24)
			.SetRange(10, 150)
			.SetDisplay("Lookback Period", "Lookback period used to normalize growth spread", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 1.35m)
			.SetRange(0.5m, 4m)
			.SetDisplay("Entry Threshold", "Z-score threshold required to open a position", "Signals");

		_exitThreshold = Param(nameof(ExitThreshold), 0.3m)
			.SetRange(0m, 2m)
			.SetDisplay("Exit Threshold", "Z-score threshold required to close a position", "Signals");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetRange(0, 100)
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Risk");

		_stopLoss = Param(nameof(StopLoss), 2.5m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for both instruments", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		if (!Security2Id.IsEmpty())
			yield return (new Security { Id = Security2Id }, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_security2 = null!;
		_primaryAssetBase = null!;
		_secondaryAssetBase = null!;
		_growthSpreadAverage = null!;
		_growthSpreadDeviation = null!;
		_previousPrimaryAssetBase = 0m;
		_previousSecondaryAssetBase = 0m;
		_latestPrimaryGrowth = 0m;
		_latestSecondaryGrowth = 0m;
		_primaryUpdated = false;
		_secondaryUpdated = false;
		_previousZScore = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Primary security is not specified.");

		if (Security2Id.IsEmpty())
			throw new InvalidOperationException("Secondary security identifier is not specified.");

		_security2 = this.LookupById(Security2Id) ?? new Security { Id = Security2Id };
		_primaryAssetBase = new ExponentialMovingAverage { Length = AssetLength };
		_secondaryAssetBase = new ExponentialMovingAverage { Length = AssetLength };
		_growthSpreadAverage = new SimpleMovingAverage { Length = LookbackPeriod };
		_growthSpreadDeviation = new StandardDeviation { Length = LookbackPeriod };
		_cooldownRemaining = 0;

		var primarySubscription = SubscribeCandles(CandleType, security: Security);
		var secondarySubscription = SubscribeCandles(CandleType, security: _security2);

		primarySubscription
			.Bind(ProcessPrimaryCandle)
			.Start();

		secondarySubscription
			.Bind(ProcessSecondaryCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawCandles(area, secondarySubscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent));
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestPrimaryGrowth = UpdateGrowth(_primaryAssetBase, candle, ref _previousPrimaryAssetBase);
		_primaryUpdated = true;
		TryProcessGrowthSpread(candle.OpenTime);
	}

	private void ProcessSecondaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_latestSecondaryGrowth = UpdateGrowth(_secondaryAssetBase, candle, ref _previousSecondaryAssetBase);
		_secondaryUpdated = true;
		TryProcessGrowthSpread(candle.OpenTime);
	}

	private decimal UpdateGrowth(ExponentialMovingAverage average, ICandleMessage candle, ref decimal previousValue)
	{
		var syntheticAssets = CalculateSyntheticAssets(candle);
		var assetBase = average.Process(syntheticAssets, candle.OpenTime, true).ToDecimal();

		if (previousValue == 0m)
		{
			previousValue = assetBase;
			return 0m;
		}

		var growth = (assetBase - previousValue) / Math.Max(Math.Abs(previousValue), 1m);
		previousValue = assetBase;

		return growth;
	}

	private decimal CalculateSyntheticAssets(ICandleMessage candle)
	{
		var priceBase = Math.Max(candle.OpenPrice, 1m);
		var range = Math.Max(candle.HighPrice - candle.LowPrice, Security?.PriceStep ?? 1m);
		var turnoverProxy = candle.ClosePrice * (1m + ((range / priceBase) * 5m));
		var balanceSheetProxy = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		return turnoverProxy + balanceSheetProxy;
	}

	private void TryProcessGrowthSpread(DateTime time)
	{
		if (!_primaryUpdated || !_secondaryUpdated)
			return;

		_primaryUpdated = false;
		_secondaryUpdated = false;

		if (!_primaryAssetBase.IsFormed || !_secondaryAssetBase.IsFormed)
			return;

		var growthSpread = _latestPrimaryGrowth - _latestSecondaryGrowth;
		var mean = _growthSpreadAverage.Process(growthSpread, time, true).ToDecimal();
		var deviation = _growthSpreadDeviation.Process(growthSpread, time, true).ToDecimal();

		if (!_growthSpreadAverage.IsFormed || !_growthSpreadDeviation.IsFormed || deviation <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var zScore = (growthSpread - mean) / deviation;
		var bullishEntry = _previousZScore is decimal previousBullish && previousBullish > -EntryThreshold && zScore <= -EntryThreshold;
		var bearishEntry = _previousZScore is decimal previousBearish && previousBearish < EntryThreshold && zScore >= EntryThreshold;

		if (_cooldownRemaining == 0 && Position == 0)
		{
			if (bullishEntry)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (bearishEntry)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && zScore >= -ExitThreshold)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && zScore <= ExitThreshold)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_previousZScore = zScore;
	}
}
