namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that accumulates directional volume impact and trades on persistent imbalances.
/// </summary>
public class VolumeBlockOrderAnalyzerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _impactDecay;
	private readonly StrategyParam<decimal> _impactNormalization;
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _cumulativeImpact;
	private int _cooldownRemaining;
	private readonly List<decimal> _volumeBuffer = new();

	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal ImpactDecay { get => _impactDecay.Value; set => _impactDecay.Value = value; }
	public decimal ImpactNormalization { get => _impactNormalization.Value; set => _impactNormalization.Value = value; }
	public decimal SignalThreshold { get => _signalThreshold.Value; set => _signalThreshold.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeBlockOrderAnalyzerStrategy()
	{
		_volumeThreshold = Param(nameof(VolumeThreshold), 1.05m)
			.SetDisplay("Volume Threshold", "Relative volume required for an impact update", "Volume")
			.SetGreaterThanZero();

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetDisplay("Lookback Period", "Lookback used for average volume", "Volume")
			.SetGreaterThanZero();

		_impactDecay = Param(nameof(ImpactDecay), 0.9m)
			.SetDisplay("Impact Decay", "Decay applied to accumulated impact", "Impact");

		_impactNormalization = Param(nameof(ImpactNormalization), 2m)
			.SetDisplay("Impact Normalization", "Normalization applied to directional volume", "Impact")
			.SetGreaterThanZero();

		_signalThreshold = Param(nameof(SignalThreshold), 0.3m)
			.SetDisplay("Signal Threshold", "Absolute impact required for a new trade", "Strategy");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetDisplay("Signal Cooldown", "Bars to wait after entries and exits", "Strategy")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_cumulativeImpact = 0m;
		_cooldownRemaining = 0;
		_volumeBuffer.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cumulativeImpact = 0m;
		_cooldownRemaining = 0;
		_volumeBuffer.Clear();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

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

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// Track volume for averaging
		_volumeBuffer.Add(candle.TotalVolume);
		if (_volumeBuffer.Count > LookbackPeriod)
			_volumeBuffer.RemoveAt(0);

		if (_volumeBuffer.Count < LookbackPeriod)
			return;

		// Calculate average volume
		var sumVol = 0m;
		for (var i = 0; i < _volumeBuffer.Count; i++)
			sumVol += _volumeBuffer[i];
		var averageVolume = sumVol / _volumeBuffer.Count;

		var relativeVolume = averageVolume <= 0m ? 0m : candle.TotalVolume / averageVolume;
		var directionalMove = candle.ClosePrice > candle.OpenPrice ? 1m : candle.ClosePrice < candle.OpenPrice ? -1m : 0m;
		var impact = relativeVolume >= VolumeThreshold ? directionalMove * relativeVolume / ImpactNormalization : 0m;

		_cumulativeImpact = _cumulativeImpact * ImpactDecay + impact;

		if (Position != 0 || _cooldownRemaining > 0)
			return;

		if (_cumulativeImpact >= SignalThreshold)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cumulativeImpact <= -SignalThreshold)
		{
			SellMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
