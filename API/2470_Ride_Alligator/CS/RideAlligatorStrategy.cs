using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bill Williams Alligator.
/// Buys when the lips cross above the jaws while the teeth remain below.
/// Sells when the lips cross below the jaws while the teeth remain above.
/// A trailing stop is placed at the jaws line.
/// </summary>
public class RideAlligatorStrategy : Strategy
{
	private readonly StrategyParam<int> _basePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private bool _prevLipsAboveJaw;
	private decimal? _stopPrice;

	/// <summary>
	/// Base period used to calculate Alligator lengths.
	/// </summary>
	public int BasePeriod
	{
		get => _basePeriod.Value;
		set => _basePeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RideAlligatorStrategy"/>.
	/// </summary>
	public RideAlligatorStrategy()
	{
		_basePeriod = Param(nameof(BasePeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Base Period", "Root period for Alligator lines", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

		_prevLipsAboveJaw = false;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var a1 = (int)Math.Round(BasePeriod * 1.61803398874989m);
		var a2 = (int)Math.Round(a1 * 1.61803398874989m);
		var a3 = (int)Math.Round(a2 * 1.61803398874989m);

		_jaw = new SmoothedMovingAverage { Length = a3 };
		_teeth = new SmoothedMovingAverage { Length = a2 };
		_lips = new SmoothedMovingAverage { Length = a1 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		var jawVal = _jaw.Process(median);
		var teethVal = _teeth.Process(median);
		var lipsVal = _lips.Process(median);

		if (!jawVal.IsFinal || !teethVal.IsFinal || !lipsVal.IsFinal)
			return;

		var jaw = jawVal.GetValue<decimal>();
		var teeth = teethVal.GetValue<decimal>();
		var lips = lipsVal.GetValue<decimal>();

		var lipsAboveJaw = lips > jaw;
		var lipsBelowJaw = lips < jaw;
		var teethAboveJaw = teeth > jaw;
		var teethBelowJaw = teeth < jaw;

		if (Position <= 0 && !_prevLipsAboveJaw && lipsAboveJaw && teethBelowJaw)
		{
			BuyMarket();
			_stopPrice = null;
		}
		else if (Position >= 0 && _prevLipsAboveJaw && lipsBelowJaw && teethAboveJaw)
		{
			SellMarket();
			_stopPrice = null;
		}

		if (Position > 0)
		{
			if (jaw < candle.ClosePrice)
			{
				var step = Security.PriceStep ?? 0m;
				if (_stopPrice is null || jaw > _stopPrice + step)
					_stopPrice = jaw;
			}

			if (_stopPrice != null && candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (jaw > candle.ClosePrice)
			{
				var step = Security.PriceStep ?? 0m;
				if (_stopPrice is null || jaw < _stopPrice - step)
					_stopPrice = jaw;
			}

			if (_stopPrice != null && candle.ClosePrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
			}
		}

		_prevLipsAboveJaw = lipsAboveJaw;
	}
}
