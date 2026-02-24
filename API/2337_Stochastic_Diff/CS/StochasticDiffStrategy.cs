using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on the smoothed difference between Stochastic %K and %D.
/// Opens long when the diff turns upward and short when it turns downward.
/// </summary>
public class StochasticDiffStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _smoothingLength;

	private ExponentialMovingAverage _smoothing;
	private decimal? _prevDiff;
	private decimal? _prevPrevDiff;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }

	public StochasticDiffStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis", "General");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K period", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D period", "Stochastic");

		_smoothingLength = Param(nameof(SmoothingLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length for diff smoothing", "Stochastic");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevDiff = null;
		_prevPrevDiff = null;
		_smoothing = new ExponentialMovingAverage { Length = SmoothingLength };

		var stoch = new StochasticOscillator();
		stoch.K.Length = KPeriod;
		stoch.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stochVal is not IStochasticOscillatorValue typed)
			return;

		if (typed.K is not decimal k || typed.D is not decimal d)
			return;

		var diff = k - d;
		var smoothResult = _smoothing.Process(diff, candle.CloseTime, true);

		if (!_smoothing.IsFormed)
			return;

		var current = smoothResult.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrevDiff = _prevDiff;
			_prevDiff = current;
			return;
		}

		if (_prevPrevDiff.HasValue && _prevDiff.HasValue)
		{
			var turningUp = _prevDiff < _prevPrevDiff && current >= _prevDiff;
			var turningDown = _prevDiff > _prevPrevDiff && current <= _prevDiff;

			if (turningUp && Position <= 0)
				BuyMarket();
			else if (turningDown && Position >= 0)
				SellMarket();
		}

		_prevPrevDiff = _prevDiff;
		_prevDiff = current;
	}
}
