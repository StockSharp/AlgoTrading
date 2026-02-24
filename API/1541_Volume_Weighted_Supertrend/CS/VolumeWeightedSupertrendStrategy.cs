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
/// Volume-weighted supertrend strategy combining price and volume trend signals.
/// Uses StdDev-based supertrend on price and a volume supertrend.
/// </summary>
public class VolumeWeightedSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;
	private decimal? _prevSupertrend;
	private int _prevDirection = 1;
	private decimal? _prevClose;

	private readonly List<decimal> _volumes = new();
	private decimal? _prevVolUpperBand;
	private decimal? _prevVolLowerBand;
	private decimal? _prevVolSupertrend;
	private int _prevVolDirection = 1;
	private decimal? _prevVolume;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeWeightedSupertrendStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Supertrend period", "General");

		_factor = Param(nameof(Factor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "Multiplier", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpperBand = null;
		_prevLowerBand = null;
		_prevSupertrend = null;
		_prevDirection = 1;
		_prevClose = null;
		_volumes.Clear();
		_prevVolUpperBand = null;
		_prevVolLowerBand = null;
		_prevVolSupertrend = null;
		_prevVolDirection = 1;
		_prevVolume = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdDev = new StandardDeviation { Length = Period };
		var sma = new SimpleMovingAverage { Length = Period };

		_prevUpperBand = null;
		_prevLowerBand = null;
		_prevSupertrend = null;
		_prevDirection = 1;
		_prevClose = null;
		_volumes.Clear();
		_prevVolUpperBand = null;
		_prevVolLowerBand = null;
		_prevVolSupertrend = null;
		_prevVolDirection = 1;
		_prevVolume = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(stdDev, sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdVal, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;
		var volume = candle.TotalVolume;

		_volumes.Add(volume);
		while (_volumes.Count > Period + 1)
			_volumes.RemoveAt(0);

		if (stdVal <= 0 || _volumes.Count < Period)
		{
			_prevClose = close;
			_prevVolume = volume;
			return;
		}

		// Price supertrend
		var upperBand = hl2 + Factor * stdVal;
		var lowerBand = hl2 - Factor * stdVal;

		if (_prevLowerBand != null)
			lowerBand = lowerBand > _prevLowerBand.Value || (_prevClose.HasValue && _prevClose.Value < _prevLowerBand.Value) ? lowerBand : _prevLowerBand.Value;
		if (_prevUpperBand != null)
			upperBand = upperBand < _prevUpperBand.Value || (_prevClose.HasValue && _prevClose.Value > _prevUpperBand.Value) ? upperBand : _prevUpperBand.Value;

		int direction;
		if (_prevSupertrend == _prevUpperBand)
			direction = close > upperBand ? -1 : 1;
		else
			direction = close < lowerBand ? 1 : -1;

		var supertrend = direction == -1 ? lowerBand : upperBand;
		var supertrendUpStart = direction == -1 && _prevDirection == 1;
		var supertrendDnStart = direction == 1 && _prevDirection == -1;
		var inRisingTrend = supertrend < close;

		// Volume supertrend (manual ATR-like calc on volume)
		var trVolume = _prevVolume.HasValue ? Math.Abs(volume - _prevVolume.Value) : 0m;
		var volAvg = _volumes.Average();
		var volStd = 0m;
		if (_volumes.Count > 1)
		{
			var sumSq = _volumes.Sum(v => (v - volAvg) * (v - volAvg));
			volStd = (decimal)Math.Sqrt((double)(sumSq / _volumes.Count));
		}

		var volUpperBand = volume + Factor * volStd;
		var volLowerBand = volume - Factor * volStd;

		if (_prevVolLowerBand != null)
			volLowerBand = volLowerBand > _prevVolLowerBand.Value || (_prevVolume.HasValue && _prevVolume.Value < _prevVolLowerBand.Value) ? volLowerBand : _prevVolLowerBand.Value;
		if (_prevVolUpperBand != null)
			volUpperBand = volUpperBand < _prevVolUpperBand.Value || (_prevVolume.HasValue && _prevVolume.Value > _prevVolUpperBand.Value) ? volUpperBand : _prevVolUpperBand.Value;

		int volDirection;
		if (_prevVolSupertrend == _prevVolUpperBand)
			volDirection = volume > volUpperBand ? -1 : 1;
		else
			volDirection = volume < volLowerBand ? 1 : -1;

		var volumeSupertrend = volDirection == -1 ? volLowerBand : volUpperBand;
		var volumeChangeUp = _prevVolDirection == 1 && volDirection == -1;
		var volumeChangeDn = _prevVolDirection == -1 && volDirection == 1;
		var inRisingVolume = volumeSupertrend < volume;

		var buy = (inRisingVolume && supertrendUpStart) || (volumeChangeUp && inRisingTrend);
		var sell = (!inRisingVolume && supertrendDnStart) || (volumeChangeDn && !inRisingTrend);

		if (buy && Position <= 0)
			BuyMarket();
		else if (sell && Position >= 0)
			SellMarket();

		_prevUpperBand = upperBand;
		_prevLowerBand = lowerBand;
		_prevSupertrend = supertrend;
		_prevDirection = direction;
		_prevVolUpperBand = volUpperBand;
		_prevVolLowerBand = volLowerBand;
		_prevVolSupertrend = volumeSupertrend;
		_prevVolDirection = volDirection;
		_prevClose = close;
		_prevVolume = volume;
	}
}
