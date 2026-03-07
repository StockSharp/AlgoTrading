using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades an ATR-adaptive RSI confirmed by relative volume.
/// </summary>
public class AdaptiveRsiVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _minRsiPeriod;
	private readonly StrategyParam<int> _maxRsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _volumeLookback;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _fastRsi = null!;
	private RelativeStrengthIndex _slowRsi = null!;
	private AverageTrueRange _atr = null!;
	private SimpleMovingAverage _volumeSma = null!;
	private decimal _adaptiveRsiValue;
	private decimal _avgVolume;
	private decimal _atrValue;
	private int _cooldownRemaining;

	/// <summary>
	/// Strategy parameter: Minimum RSI period.
	/// </summary>
	public int MinRsiPeriod
	{
		get => _minRsiPeriod.Value;
		set => _minRsiPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Maximum RSI period.
	/// </summary>
	public int MaxRsiPeriod
	{
		get => _maxRsiPeriod.Value;
		set => _maxRsiPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR period for volatility normalization.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Volume lookback period.
	/// </summary>
	public int VolumeLookback
	{
		get => _volumeLookback.Value;
		set => _volumeLookback.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Closed candles to wait between position changes.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveRsiVolumeStrategy()
	{
		_minRsiPeriod = Param(nameof(MinRsiPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Min RSI Period", "Fast RSI period used in high volatility", "Indicator Settings");

		_maxRsiPeriod = Param(nameof(MaxRsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Max RSI Period", "Slow RSI period used in low volatility", "Indicator Settings");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings");

		_volumeLookback = Param(nameof(VolumeLookback), 12)
			.SetGreaterThanZero()
			.SetDisplay("Volume Lookback", "Periods used for average volume", "Volume Settings");

		_cooldownBars = Param(nameof(CooldownBars), 8)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another signal", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_adaptiveRsiValue = 50m;
		_avgVolume = 0m;
		_atrValue = 0m;
		_cooldownRemaining = 0;

		_fastRsi?.Reset();
		_slowRsi?.Reset();
		_atr?.Reset();
		_volumeSma?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastRsi = new RelativeStrengthIndex
		{
			Length = MinRsiPeriod
		};

		_slowRsi = new RelativeStrengthIndex
		{
			Length = MaxRsiPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		_volumeSma = new SimpleMovingAverage
		{
			Length = VolumeLookback
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastRsi);
			DrawIndicator(area, _slowRsi);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var fastRsiValue = _fastRsi.Process(new DecimalIndicatorValue(_fastRsi, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var slowRsiValue = _slowRsi.Process(new DecimalIndicatorValue(_slowRsi, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var atrValue = _atr.Process(new CandleIndicatorValue(_atr, candle) { IsFinal = true });
		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		if (!_fastRsi.IsFormed || !_slowRsi.IsFormed || !_atr.IsFormed || !_volumeSma.IsFormed ||
			fastRsiValue.IsEmpty || slowRsiValue.IsEmpty || atrValue.IsEmpty || volumeValue.IsEmpty)
			return;

		_avgVolume = volumeValue.ToDecimal();
		_atrValue = atrValue.ToDecimal();

		var fastRsi = fastRsiValue.ToDecimal();
		var slowRsi = slowRsiValue.ToDecimal();
		var normalizedAtr = Math.Min(Math.Max(_atrValue / Math.Max(candle.ClosePrice * 0.02m, 1m), 0m), 1m);

		// High volatility shifts weight to the faster RSI.
		_adaptiveRsiValue = slowRsi + ((fastRsi - slowRsi) * normalizedAtr);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isHighVolume = candle.TotalVolume >= (_avgVolume * 0.9m);
		var oversoldLevel = 45m - (normalizedAtr * 5m);
		var overboughtLevel = 55m + (normalizedAtr * 5m);

		if (_cooldownRemaining == 0 && isHighVolume && _adaptiveRsiValue <= oversoldLevel && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && isHighVolume && _adaptiveRsiValue >= overboughtLevel && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && _adaptiveRsiValue >= 52m)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && _adaptiveRsiValue <= 48m)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
