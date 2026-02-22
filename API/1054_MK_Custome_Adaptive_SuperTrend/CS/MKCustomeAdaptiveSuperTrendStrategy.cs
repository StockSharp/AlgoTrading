using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive SuperTrend strategy using volatility clustering.
/// </summary>
public class MKCustomeAdaptiveSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<int> _trainingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private readonly List<decimal> _atrHistory = new();

	private decimal _prevLowerBand;
	private decimal _prevUpperBand;
	private decimal _prevSuperTrend;
	private int _prevDirection;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public int TrainingPeriod { get => _trainingPeriod.Value; set => _trainingPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MKCustomeAdaptiveSuperTrendStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 10);
		_factor = Param(nameof(Factor), 3m);
		_trainingPeriod = Param(nameof(TrainingPeriod), 20);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLowerBand = 0;
		_prevUpperBand = 0;
		_prevSuperTrend = 0;
		_prevDirection = 0;
		_atrHistory.Clear();

		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;

		_atrHistory.Add(atr);
		if (_atrHistory.Count > TrainingPeriod)
			_atrHistory.RemoveAt(0);

		if (_atrHistory.Count < TrainingPeriod)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		var atrHigh = _atrHistory.Max();
		var atrLow = _atrHistory.Min();

		var range = atrHigh - atrLow;
		if (range <= 0) range = atr * 0.01m;

		var highVol = atrLow + range * 0.75m;
		var midVol = atrLow + range * 0.5m;
		var lowVol = atrLow + range * 0.25m;

		var distHigh = Math.Abs(atr - highVol);
		var distMid = Math.Abs(atr - midVol);
		var distLow = Math.Abs(atr - lowVol);

		var assigned = distHigh < distMid
			? (distHigh < distLow ? highVol : lowVol)
			: (distMid < distLow ? midVol : lowVol);

		var (st, dir) = CalcSuperTrend(candle, assigned);

		if (_prevDirection <= 0 && dir > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket();
		}
		else if (_prevDirection >= 0 && dir < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket();
		}

		_prevDirection = dir;
	}

	private (decimal st, int dir) CalcSuperTrend(ICandleMessage candle, decimal atrVal)
	{
		var src = (candle.HighPrice + candle.LowPrice) / 2m;
		var upperBand = src + Factor * atrVal;
		var lowerBand = src - Factor * atrVal;

		if (_prevLowerBand != default && lowerBand < _prevLowerBand && candle.ClosePrice > _prevLowerBand)
			lowerBand = _prevLowerBand;

		if (_prevUpperBand != default && upperBand > _prevUpperBand && candle.ClosePrice < _prevUpperBand)
			upperBand = _prevUpperBand;

		int dir;
		if (_prevSuperTrend == 0)
			dir = candle.ClosePrice > src ? 1 : -1;
		else if (_prevSuperTrend == _prevUpperBand)
			dir = candle.ClosePrice > upperBand ? 1 : -1;
		else
			dir = candle.ClosePrice < lowerBand ? -1 : 1;

		var st = dir == 1 ? lowerBand : upperBand;

		_prevLowerBand = lowerBand;
		_prevUpperBand = upperBand;
		_prevSuperTrend = st;

		return (st, dir);
	}
}
