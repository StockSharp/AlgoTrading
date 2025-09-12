using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume-weighted supertrend strategy combining price and volume trend signals.
/// </summary>
public class VolumeWeightedSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private VolumeWeightedMovingAverage _vwap = null!;
	private ExponentialMovingAverage _volumeAtr = null!;

	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;
	private decimal? _prevSupertrend;
	private int _prevDirection = 1;
	private decimal? _prevClose;

	private decimal? _prevVolUpperBand;
	private decimal? _prevVolLowerBand;
	private decimal? _prevVolSupertrend;
	private int _prevVolDirection = 1;
	private decimal? _prevVolume;

	/// <summary>
	/// ATR period for price supertrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period for VWAP and volume supertrend.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VolumeWeightedSupertrendStrategy"/>.
	/// </summary>
	public VolumeWeightedSupertrendStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for price supertrend", "General")
			.SetCanOptimize(true);

		_volumePeriod = Param(nameof(VolumePeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Period for VWAP and volume trend", "General")
			.SetCanOptimize(true);

		_factor = Param(nameof(Factor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "ATR multiplier", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_vwap = new VolumeWeightedMovingAverage { Length = VolumePeriod };
		_volumeAtr = new ExponentialMovingAverage { Length = VolumePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenNew(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr.Process(candle);
		var vwapValue = _vwap.Process(candle);

		if (!atrValue.IsFinal || !vwapValue.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			_prevVolume = candle.TotalVolume;
			return;
		}

		var atr = atrValue.ToDecimal();
		var vwap = vwapValue.ToDecimal();

		var upperBand = vwap + Factor * atr;
		var lowerBand = vwap - Factor * atr;

		if (_prevLowerBand != null)
			lowerBand = lowerBand > _prevLowerBand || _prevClose < _prevLowerBand ? lowerBand : _prevLowerBand.Value;
		if (_prevUpperBand != null)
			upperBand = upperBand < _prevUpperBand || _prevClose > _prevUpperBand ? upperBand : _prevUpperBand.Value;

		int direction;
		if (_prevSupertrend == _prevUpperBand)
			direction = candle.ClosePrice > upperBand ? -1 : 1;
		else
			direction = candle.ClosePrice < lowerBand ? 1 : -1;

		var supertrend = direction == -1 ? lowerBand : upperBand;
		bool supertrendUpStart = direction == -1 && _prevDirection == 1;
		bool supertrendDnStart = direction == 1 && _prevDirection == -1;
		bool inRisingTrend = supertrend < candle.ClosePrice;

		// Volume supertrend
		var volume = candle.TotalVolume;
		var trVolume = _prevVolume == null ? 0m : Math.Abs(volume - _prevVolume.Value);
		var volAtrValue = _volumeAtr.Process(trVolume);
		if (!volAtrValue.IsFinal)
		{
			_prevClose = candle.ClosePrice;
			_prevVolume = volume;
			_prevUpperBand = upperBand;
			_prevLowerBand = lowerBand;
			_prevSupertrend = supertrend;
			_prevDirection = direction;
			return;
		}

		var atrVol = volAtrValue.ToDecimal();
		var volUpperBand = volume + Factor * atrVol;
		var volLowerBand = volume - Factor * atrVol;

		if (_prevVolLowerBand != null)
			volLowerBand = volLowerBand > _prevVolLowerBand || _prevVolume < _prevVolLowerBand ? volLowerBand : _prevVolLowerBand.Value;
		if (_prevVolUpperBand != null)
			volUpperBand = volUpperBand < _prevVolUpperBand || _prevVolume > _prevVolUpperBand ? volUpperBand : _prevVolUpperBand.Value;

		int volDirection;
		if (_prevVolSupertrend == _prevVolUpperBand)
			volDirection = volume > volUpperBand ? -1 : 1;
		else
			volDirection = volume < volLowerBand ? 1 : -1;

		var volumeSupertrend = volDirection == -1 ? volLowerBand : volUpperBand;
		bool volumeChangeUp = _prevVolDirection == 1 && volDirection == -1;
		bool volumeChangeDn = _prevVolDirection == -1 && volDirection == 1;
		bool inRisingVolume = volumeSupertrend < volume;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevUpperBand = upperBand;
			_prevLowerBand = lowerBand;
			_prevSupertrend = supertrend;
			_prevDirection = direction;

			_prevVolUpperBand = volUpperBand;
			_prevVolLowerBand = volLowerBand;
			_prevVolSupertrend = volumeSupertrend;
			_prevVolDirection = volDirection;
			_prevClose = candle.ClosePrice;
			_prevVolume = volume;
			return;
		}

		var buy = (inRisingVolume && supertrendUpStart) || (volumeChangeUp && inRisingTrend);
		var sell = (!inRisingVolume && supertrendDnStart) || (volumeChangeDn && !inRisingTrend);

		if (buy && Position <= 0)
		{
			var qty = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(qty);
		}
		else if (sell && Position > 0)
		{
			SellMarket(Position);
		}

		_prevUpperBand = upperBand;
		_prevLowerBand = lowerBand;
		_prevSupertrend = supertrend;
		_prevDirection = direction;

		_prevVolUpperBand = volUpperBand;
		_prevVolLowerBand = volLowerBand;
		_prevVolSupertrend = volumeSupertrend;
		_prevVolDirection = volDirection;
		_prevClose = candle.ClosePrice;
		_prevVolume = volume;
	}
}
