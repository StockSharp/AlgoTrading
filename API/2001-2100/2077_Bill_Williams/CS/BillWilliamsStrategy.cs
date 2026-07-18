using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams strategy combining Alligator divergence with fractal breakouts.
/// </summary>
public class BillWilliamsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _filterPct;
	private readonly StrategyParam<decimal> _gatorDivSlowPct;
	private readonly StrategyParam<decimal> _gatorDivFastPct;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _fractalUp;
	private decimal? _fractalDown;

	public decimal FilterPct { get => _filterPct.Value; set => _filterPct.Value = value; }
	public decimal GatorDivSlowPct { get => _gatorDivSlowPct.Value; set => _gatorDivSlowPct.Value = value; }
	public decimal GatorDivFastPct { get => _gatorDivFastPct.Value; set => _gatorDivFastPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BillWilliamsStrategy()
	{
		_filterPct = Param(nameof(FilterPct), 0.05m)
			.SetDisplay("Filter %", "Minimal price offset as percentage", "General");

		_gatorDivSlowPct = Param(nameof(GatorDivSlowPct), 0.3m)
			.SetDisplay("Jaw-Teeth %", "Required jaw-teeth distance as % of price", "Alligator");

		_gatorDivFastPct = Param(nameof(GatorDivFastPct), 0.15m)
			.SetDisplay("Lips-Teeth %", "Required lips-teeth distance as % of price", "Alligator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_highs.Clear();
		_lows.Clear();
		_fractalUp = default;
		_fractalDown = default;
		_jaw = default;
		_teeth = default;
		_lips = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		Indicators.Add(_jaw);
		Indicators.Add(_teeth);
		Indicators.Add(_lips);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > 5) _highs.Dequeue();
		if (_lows.Count > 5) _lows.Dequeue();

		if (_highs.Count == 5)
		{
			var hs = _highs.ToArray();
			if (hs[2] > hs[0] && hs[2] > hs[1] && hs[2] > hs[3] && hs[2] > hs[4])
				_fractalUp = hs[2];
		}

		if (_lows.Count == 5)
		{
			var ls = _lows.ToArray();
			if (ls[2] < ls[0] && ls[2] < ls[1] && ls[2] < ls[3] && ls[2] < ls[4])
				_fractalDown = ls[2];
		}

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw.Process(median, candle.OpenTime, true);
		var teethVal = _teeth.Process(median, candle.OpenTime, true);
		var lipsVal = _lips.Process(median, candle.OpenTime, true);

		if (!jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var jaw = jawVal.ToDecimal();
		var teeth = teethVal.ToDecimal();
		var lips = lipsVal.ToDecimal();

		var price = candle.ClosePrice;
		var filter = FilterPct / 100m * price;
		var slowThreshold = GatorDivSlowPct / 100m * price;
		var fastThreshold = GatorDivFastPct / 100m * price;

		var slowDiff = Math.Abs(jaw - teeth);
		var fastDiff = Math.Abs(lips - teeth);
		var alligatorOpen = slowDiff >= slowThreshold && fastDiff >= fastThreshold;

		if (Position <= 0 && alligatorOpen && _fractalUp is decimal up &&
			candle.HighPrice >= up + filter && candle.ClosePrice > candle.OpenPrice && up >= teeth)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (Position >= 0 && alligatorOpen && _fractalDown is decimal down &&
			candle.LowPrice <= down - filter && candle.ClosePrice < candle.OpenPrice && down <= teeth)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		// Fractal stop
		if (Position > 0 && _fractalDown is decimal longStop && candle.ClosePrice <= longStop - filter)
		{
			SellMarket();
		}
		else if (Position < 0 && _fractalUp is decimal shortStop && candle.ClosePrice >= shortStop + filter)
		{
			BuyMarket();
		}
	}
}
