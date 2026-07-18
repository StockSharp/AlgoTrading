using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Alligator strategy using three smoothed moving averages (jaw, teeth, lips).
/// Buys when lips cross above jaw; sells when lips cross below jaw.
/// Uses a trailing SMA for exit.
/// </summary>
public class LiveAlligatorStrategy : Strategy
{
	private readonly StrategyParam<int> _jawLength;
	private readonly StrategyParam<int> _teethLength;
	private readonly StrategyParam<int> _lipsLength;
	private readonly StrategyParam<int> _trailLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLips;
	private decimal _prevJaw;
	private decimal _prevTrail;
	private bool _hasPrev;

	public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }
	public int TeethLength { get => _teethLength.Value; set => _teethLength.Value = value; }
	public int LipsLength { get => _lipsLength.Value; set => _lipsLength.Value = value; }
	public int TrailLength { get => _trailLength.Value; set => _trailLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LiveAlligatorStrategy()
	{
		_jawLength = Param(nameof(JawLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Jaw", "Alligator Jaw length", "Indicators");

		_teethLength = Param(nameof(TeethLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Teeth", "Alligator Teeth length", "Indicators");

		_lipsLength = Param(nameof(LipsLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Lips", "Alligator Lips length", "Indicators");

		_trailLength = Param(nameof(TrailLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trail", "Trailing SMA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevLips = 0;
		_prevJaw = 0;
		_prevTrail = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var jaw = new SmoothedMovingAverage { Length = JawLength };
		var teeth = new SmoothedMovingAverage { Length = TeethLength };
		var lips = new SmoothedMovingAverage { Length = LipsLength };
		var trail = new SimpleMovingAverage { Length = TrailLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jaw, teeth, lips, trail, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawVal, decimal teethVal, decimal lipsVal, decimal trailVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevLips = lipsVal;
			_prevJaw = jawVal;
			_prevTrail = trailVal;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Lips cross above jaw -> uptrend start
		if (_prevLips <= _prevJaw && lipsVal > jawVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Lips cross below jaw -> downtrend start
		else if (_prevLips >= _prevJaw && lipsVal < jawVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		// Trail exit: close below trail for longs
		if (Position > 0 && close < _prevTrail)
		{
			SellMarket();
		}
		// Trail exit: close above trail for shorts
		else if (Position < 0 && close > _prevTrail)
		{
			BuyMarket();
		}

		_prevLips = lipsVal;
		_prevJaw = jawVal;
		_prevTrail = trailVal;
	}
}
