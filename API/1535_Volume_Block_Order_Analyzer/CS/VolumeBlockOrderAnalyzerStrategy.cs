namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _cumulativeImpact;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public decimal ImpactDecay { get => _impactDecay.Value; set => _impactDecay.Value = value; }
	public decimal ImpactNormalization { get => _impactNormalization.Value; set => _impactNormalization.Value = value; }
	public decimal SignalThreshold { get => _signalThreshold.Value; set => _signalThreshold.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
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

		_stopPercent = Param(nameof(StopPercent), 2m)
			.SetDisplay("Trailing Stop %", "Maximum adverse move after entry", "Risk")
			.SetGreaterThanZero();

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 72)
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
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var avgVolume = new SMA { Length = LookbackPeriod };
		var subscription = SubscribeCandles(CandleType);

		_cumulativeImpact = 0m;
		_entryPrice = 0m;
		_cooldownRemaining = 0;

		subscription
			.Bind(avgVolume, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal averageVolume)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var relativeVolume = averageVolume <= 0m ? 0m : candle.TotalVolume / averageVolume;
		var directionalMove = candle.ClosePrice > candle.OpenPrice ? 1m : candle.ClosePrice < candle.OpenPrice ? -1m : 0m;
		var impact = relativeVolume >= VolumeThreshold ? directionalMove * relativeVolume / ImpactNormalization : 0m;

		_cumulativeImpact = _cumulativeImpact * ImpactDecay + impact;

		if (Position > 0)
		{
			var stop = _entryPrice * (1m - StopPercent / 100m);
			if (candle.LowPrice <= stop || _cumulativeImpact <= 0m)
			{
				SellMarket(Position);
				_cooldownRemaining = SignalCooldownBars;
			}
			return;
		}

		if (Position < 0)
		{
			var stop = _entryPrice * (1m + StopPercent / 100m);
			if (candle.HighPrice >= stop || _cumulativeImpact >= 0m)
			{
				BuyMarket(Math.Abs(Position));
				_cooldownRemaining = SignalCooldownBars;
			}
			return;
		}

		if (_cooldownRemaining > 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cumulativeImpact >= SignalThreshold)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cumulativeImpact <= -SignalThreshold)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
