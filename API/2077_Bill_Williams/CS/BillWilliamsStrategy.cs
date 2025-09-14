using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<decimal> _filterPoints;
	private readonly StrategyParam<decimal> _gatorDivSlowPoints;
	private readonly StrategyParam<decimal> _gatorDivFastPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _fractalUp;
	private decimal? _fractalDown;

	/// <summary>
	/// Price filter in points.
	/// </summary>
	public decimal FilterPoints
	{
		get => _filterPoints.Value;
		set => _filterPoints.Value = value;
	}

	/// <summary>
	/// Minimal jaw-teeth distance in points.
	/// </summary>
	public decimal GatorDivSlowPoints
	{
		get => _gatorDivSlowPoints.Value;
		set => _gatorDivSlowPoints.Value = value;
	}

	/// <summary>
	/// Minimal lips-teeth distance in points.
	/// </summary>
	public decimal GatorDivFastPoints
	{
		get => _gatorDivFastPoints.Value;
		set => _gatorDivFastPoints.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BillWilliamsStrategy"/>.
	/// </summary>
	public BillWilliamsStrategy()
	{
		_filterPoints = Param(nameof(FilterPoints), 30m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Filter", "Minimal price offset in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);

		_gatorDivSlowPoints = Param(nameof(GatorDivSlowPoints), 250m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Jaw-Teeth Points", "Required jaw-teeth distance", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(0m, 500m, 25m);

		_gatorDivFastPoints = Param(nameof(GatorDivFastPoints), 150m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Lips-Teeth Points", "Required lips-teeth distance", "Alligator")
			.SetCanOptimize(true)
			.SetOptimize(0m, 300m, 25m);

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
		_fractalUp = null;
		_fractalDown = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > 5)
			_highs.Dequeue();
		if (_lows.Count > 5)
			_lows.Dequeue();

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
		var time = candle.ServerTime;
		var jawVal = _jaw.Process(new DecimalIndicatorValue(_jaw, median, time));
		var teethVal = _teeth.Process(new DecimalIndicatorValue(_teeth, median, time));
		var lipsVal = _lips.Process(new DecimalIndicatorValue(_lips, median, time));

		if (!jawVal.IsFormed || !teethVal.IsFormed || !lipsVal.IsFormed)
			return;

		var jaw = jawVal.ToDecimal();
		var teeth = teethVal.ToDecimal();
		var lips = lipsVal.ToDecimal();

		var step = Security?.PriceStep ?? 1m;
		var filter = FilterPoints * step;
		var slowThreshold = GatorDivSlowPoints * step;
		var fastThreshold = GatorDivFastPoints * step;

		var slowDiff = Math.Abs(jaw - teeth);
		var fastDiff = Math.Abs(lips - teeth);
		var alligatorOpen = slowDiff >= slowThreshold && fastDiff >= fastThreshold;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && alligatorOpen && _fractalUp is decimal up && candle.HighPrice >= up + filter && candle.ClosePrice > candle.OpenPrice && up >= teeth)
		{
			BuyMarket();
		}
		else if (Position >= 0 && alligatorOpen && _fractalDown is decimal down && candle.LowPrice <= down - filter && candle.ClosePrice < candle.OpenPrice && down <= teeth)
		{
			SellMarket();
		}

		if (Position > 0 && _fractalDown is decimal longStop && candle.ClosePrice <= longStop - filter)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && _fractalUp is decimal shortStop && candle.ClosePrice >= shortStop + filter)
		{
			BuyMarket(-Position);
		}
	}
}
