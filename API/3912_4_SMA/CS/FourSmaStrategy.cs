namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class FourSmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _verySlowLength;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _mediumSma;
	private SimpleMovingAverage _slowSma;
	private SimpleMovingAverage _verySlowSma;

	private decimal? _previousSlow;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;

	public FourSmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_trailingStop = Param(nameof(TrailingStop), 11m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5m, 100m, 5m);

		_fastLength = Param(nameof(FastLength), 5)
			.SetDisplay("Fast SMA Length", "Length of the fast moving average", "Trend Filters")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_mediumLength = Param(nameof(MediumLength), 20)
			.SetDisplay("Medium SMA Length", "Length of the medium moving average", "Trend Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_slowLength = Param(nameof(SlowLength), 40)
			.SetDisplay("Slow SMA Length", "Length of the slow moving average", "Trend Filters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 1);

		_verySlowLength = Param(nameof(VerySlowLength), 60)
			.SetDisplay("Very Slow SMA Length", "Length of the very slow moving average", "Trend Filters")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 1);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int MediumLength
	{
		get => _mediumLength.Value;
		set => _mediumLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int VerySlowLength
	{
		get => _verySlowLength.Value;
		set => _verySlowLength.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_previousSlow = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_mediumSma = new SimpleMovingAverage { Length = MediumLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };
		_verySlowSma = new SimpleMovingAverage { Length = VerySlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Use the median price to match the PRICE_MEDIAN source from MQL.
		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		// Update every moving average with the latest price sample.
		var fastValue = _fastSma.Process(median, candle.OpenTime, true);
		var mediumValue = _mediumSma.Process(median, candle.OpenTime, true);
		var slowValue = _slowSma.Process(median, candle.OpenTime, true);
		var verySlowValue = _verySlowSma.Process(median, candle.OpenTime, true);

		if (!fastValue.IsFinal || !mediumValue.IsFinal || !slowValue.IsFinal || !verySlowValue.IsFinal)
		{
			if (slowValue.IsFinal)
				_previousSlow = slowValue.ToDecimal();

			return;
		}

		var fast = fastValue.ToDecimal();
		var medium = mediumValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var verySlow = verySlowValue.ToDecimal();

		var previousSlow = _previousSlow;
		_previousSlow = slow;

		ManageLongPosition(candle, slow, verySlow);
		ManageShortPosition(candle, slow, verySlow);

		if (previousSlow is null)
			return;

		var slopeThreshold = GetSlopeThreshold();

		var longCondition = Position <= 0
			&& fast > medium
			&& medium > slow
			&& slow - verySlow >= slopeThreshold
			&& previousSlow <= verySlow;

		if (longCondition)
		{
			EnterLong(candle);
			return;
		}

		var shortCondition = Position >= 0
			&& fast < medium
			&& medium < slow
			&& verySlow - slow >= slopeThreshold
			&& previousSlow >= verySlow;

		if (shortCondition)
			EnterShort(candle);
	}

	private void ManageLongPosition(ICandleMessage candle, decimal slow, decimal verySlow)
	{
		// Manage exits for long positions including crossover, stop loss, and trailing logic.
		if (Position <= 0)
			return;

		if (slow <= verySlow)
		{
			SellMarket(Position);
			ResetLongProtection();
			return;
		}

		var step = GetPriceStep();
		var trailDistance = TrailingStop > 0m ? TrailingStop * step : 0m;

		if (_longEntryPrice is decimal entry && trailDistance > 0m)
		{
			var advance = candle.ClosePrice - entry;
			if (advance > trailDistance)
			{
				var newStop = candle.ClosePrice - trailDistance;
				if (_longStop is not decimal current || newStop > current)
					_longStop = newStop;
			}
		}

		if (_longStop is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetLongProtection();
			return;
		}

		if (_longTake is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Position);
			ResetLongProtection();
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal slow, decimal verySlow)
	{
		// Handle short position lifecycle and protective rules.
		if (Position >= 0)
			return;

		if (slow >= verySlow)
		{
			BuyMarket(-Position);
			ResetShortProtection();
			return;
		}

		var step = GetPriceStep();
		var trailDistance = TrailingStop > 0m ? TrailingStop * step : 0m;

		if (_shortEntryPrice is decimal entry && trailDistance > 0m)
		{
			var advance = entry - candle.ClosePrice;
			if (advance > trailDistance)
			{
				var newStop = candle.ClosePrice + trailDistance;
				if (_shortStop is not decimal current || newStop < current)
					_shortStop = newStop;
			}
		}

		if (_shortStop is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(-Position);
			ResetShortProtection();
			return;
		}

		if (_shortTake is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(-Position);
			ResetShortProtection();
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		// Enter the market with a long position and pre-calculate protective levels.
		BuyMarket();

		var step = GetPriceStep();
		var entry = candle.ClosePrice;

		_longEntryPrice = entry;
		_longStop = StopLoss > 0m ? entry - StopLoss * step : null;
		_longTake = TakeProfit > 0m ? entry + TakeProfit * step : null;
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		// Enter the market with a short position and define protection.
		SellMarket();

		var step = GetPriceStep();
		var entry = candle.ClosePrice;

		_shortEntryPrice = entry;
		_shortStop = StopLoss > 0m ? entry + StopLoss * step : null;
		_shortTake = TakeProfit > 0m ? entry - TakeProfit * step : null;
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private decimal GetSlopeThreshold()
	{
		var step = GetPriceStep();
		return step > 0m ? step : 0.0001m;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0.0001m;
	}
}
