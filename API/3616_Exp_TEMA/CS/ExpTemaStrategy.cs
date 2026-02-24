using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades TEMA slope reversals from the original Exp_TEMA expert advisor.
/// Enters long when TEMA slope turns positive, short when negative.
/// </summary>
public class ExpTemaStrategy : Strategy
{
	private readonly StrategyParam<int> _temaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private TripleExponentialMovingAverage _tema;
	private decimal? _prev1;
	private decimal? _prev2;
	private decimal? _prev3;

	public int TemaPeriod
	{
		get => _temaPeriod.Value;
		set => _temaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ExpTemaStrategy()
	{
		_temaPeriod = Param(nameof(TemaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("TEMA Period", "Length of Triple Exponential Moving Average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for TEMA calculation", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tema = new TripleExponentialMovingAverage { Length = TemaPeriod };
		_prev1 = null;
		_prev2 = null;
		_prev3 = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_tema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal temaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_tema.IsFormed)
		{
			_prev1 = temaValue;
			return;
		}

		if (_prev1 is null)
		{
			_prev1 = temaValue;
			return;
		}

		if (_prev2 is null)
		{
			_prev2 = _prev1;
			_prev1 = temaValue;
			return;
		}

		if (_prev3 is null)
		{
			_prev3 = _prev2;
			_prev2 = _prev1;
			_prev1 = temaValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var dtema1 = _prev1.Value - _prev2.Value;
		var dtema2 = _prev2.Value - _prev3.Value;

		// Exit on opposing slope
		if (Position > 0 && dtema1 < 0)
			SellMarket(Position);
		else if (Position < 0 && dtema1 > 0)
			BuyMarket(Math.Abs(Position));

		// Entry on slope reversal
		var turnedUp = dtema2 < 0 && dtema1 > 0;
		var turnedDown = dtema2 > 0 && dtema1 < 0;

		if (turnedUp && Position <= 0)
		{
			var vol = volume + Math.Abs(Position);
			if (vol > 0)
				BuyMarket(vol);
		}
		else if (turnedDown && Position >= 0)
		{
			var vol = volume + Position;
			if (vol > 0)
				SellMarket(vol);
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = temaValue;
	}
}
