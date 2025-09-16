using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Live Alligator strategy.
/// Opens a new position when Alligator lines switch direction and a
/// set of EMAs confirms the trend.
/// Exits on trailing smoothed moving average.
/// </summary>
public class LiveAlligatorStrategy : Strategy
{
	private readonly StrategyParam<int> _alligatorPeriod;
	private readonly StrategyParam<int> _livePeriod;
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<bool> _checkHour;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevLips;
	private decimal? _prevJaws;
	private decimal? _trailPrev;
	private int _lastTrend;

	/// <summary>
	/// Alligator base period.
	/// </summary>
	public int AlligatorPeriod { get => _alligatorPeriod.Value; set => _alligatorPeriod.Value = value; }

	/// <summary>
	/// EMA period.
	/// </summary>
	public int LivePeriod { get => _livePeriod.Value; set => _livePeriod.Value = value; }

	/// <summary>
	/// Trailing SMMA period.
	/// </summary>
	public int TrailPeriod { get => _trailPeriod.Value; set => _trailPeriod.Value = value; }

	/// <summary>
	/// Enable trading hours filter.
	/// </summary>
	public bool CheckHour { get => _checkHour.Value; set => _checkHour.Value = value; }

	/// <summary>
	/// Start trading hour.
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// End trading hour.
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>
	/// Stop-loss in absolute price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="LiveAlligatorStrategy"/>.
	/// </summary>
	public LiveAlligatorStrategy()
	{
		_alligatorPeriod = Param(nameof(AlligatorPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Alligator Period", "Base period for Alligator calculations", "Alligator");

		_livePeriod = Param(nameof(LivePeriod), 46)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for confirmation EMAs", "EMA");

		_trailPeriod = Param(nameof(TrailPeriod), 113)
			.SetGreaterThanZero()
			.SetDisplay("Trail Period", "Period for trailing SMMA", "Trailing");

		_checkHour = Param(nameof(CheckHour), false)
			.SetDisplay("Use Trading Hours", "Enable trading hours filter", "Trading");

		_startHour = Param(nameof(StartHour), 17)
			.SetDisplay("Start Hour", "Trading start hour", "Trading");

		_endHour = Param(nameof(EndHour), 1)
			.SetDisplay("End Hour", "Trading end hour", "Trading");

		_stopLoss = Param(nameof(StopLoss), 75m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Stop-Loss", "Absolute stop-loss value", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var phi = 1.61803398874989m;
		var a1 = (int)Math.Round(AlligatorPeriod * phi);
		var a2 = (int)Math.Round(a1 * phi);
		var a3 = (int)Math.Round(a2 * phi);

		var jaw = new SmoothedMovingAverage { Length = a3 };
		var teeth = new SmoothedMovingAverage { Length = a2 };
		var lips = new SmoothedMovingAverage { Length = a1 };

		var emaClose = new ExponentialMovingAverage { Length = LivePeriod };
		var emaWeighted = new ExponentialMovingAverage { Length = LivePeriod };
		var emaTypical = new ExponentialMovingAverage { Length = LivePeriod };
		var emaMedian = new ExponentialMovingAverage { Length = LivePeriod };
		var emaOpen = new ExponentialMovingAverage { Length = LivePeriod };

		var trail = new SmoothedMovingAverage { Length = TrailPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var median = (candle.HighPrice + candle.LowPrice) / 2m;

			var jawVal = jaw.Process(median);
			var teethVal = teeth.Process(median);
			var lipsVal = lips.Process(median);
			var trailVal = trail.Process(candle.ClosePrice);

			var ma1Val = emaClose.Process(candle.ClosePrice);
			var ma2Val = emaWeighted.Process((candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m);
			var ma3Val = emaTypical.Process((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);
			var ma4Val = emaMedian.Process(median);
			var ma5Val = emaOpen.Process(candle.OpenPrice);

			if (!jawVal.IsFinal || !teethVal.IsFinal || !lipsVal.IsFinal ||
				!ma1Val.IsFinal || !ma2Val.IsFinal || !ma3Val.IsFinal || !ma4Val.IsFinal || !ma5Val.IsFinal ||
				!trailVal.IsFinal)
				return;

			var jawMa = jawVal.GetValue<decimal>();
			var teethMa = teethVal.GetValue<decimal>();
			var lipsMa = lipsVal.GetValue<decimal>();
			var ma1 = ma1Val.GetValue<decimal>();
			var ma2 = ma2Val.GetValue<decimal>();
			var ma3 = ma3Val.GetValue<decimal>();
			var ma4 = ma4Val.GetValue<decimal>();
			var ma5 = ma5Val.GetValue<decimal>();
			var trailPrev = _trailPrev ?? trailVal.GetValue<decimal>();

			var hourOk = !CheckHour || (candle.OpenTime.Hour > StartHour && candle.OpenTime.Hour < EndHour);

			var bull = _prevLips < _prevJaws && lipsMa > jawMa && teethMa < jawMa &&
				ma1 > ma2 && ma2 > ma3 && ma3 > ma4 && ma4 > ma5;

			var bear = _prevLips > _prevJaws && lipsMa < jawMa && teethMa > jawMa &&
				ma1 < ma2 && ma2 < ma3 && ma3 < ma4 && ma4 < ma5;

			if (_lastTrend == 0)
			{
				if (bull) _lastTrend = 1;
				else if (bear) _lastTrend = 2;
			}

			if (Position == 0 && hourOk)
			{
				if (bull && _lastTrend == 2)
				{
					BuyMarket(Volume);

					if (StopLoss > 0)
						StartProtection(stop: new Unit(StopLoss, UnitTypes.Absolute));

					_lastTrend = 1;
				}
				else if (bear && _lastTrend == 1)
				{
					SellMarket(Volume);

					if (StopLoss > 0)
						StartProtection(stop: new Unit(StopLoss, UnitTypes.Absolute));

					_lastTrend = 2;
				}
			}
			else if (Position > 0 && candle.ClosePrice < trailPrev)
			{
				SellMarket(Position);
			}
			else if (Position < 0 && candle.ClosePrice > trailPrev)
			{
				BuyMarket(Math.Abs(Position));
			}

			_prevLips = lipsMa;
			_prevJaws = jawMa;
			_trailPrev = trailVal.GetValue<decimal>();
		}
	}
}
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var phi = 1.61803398874989m;
		var a1 = (int)Math.Round(AlligatorPeriod * phi);
		var a2 = (int)Math.Round(a1 * phi);
		var a3 = (int)Math.Round(a2 * phi);

		var jaw = new SmoothedMovingAverage { Length = a3 };
		var teeth = new SmoothedMovingAverage { Length = a2 };
		var lips = new SmoothedMovingAverage { Length = a1 };

		var emaClose = new ExponentialMovingAverage { Length = LivePeriod };
		var emaWeighted = new ExponentialMovingAverage { Length = LivePeriod };
		var emaTypical = new ExponentialMovingAverage { Length = LivePeriod };
		var emaMedian = new ExponentialMovingAverage { Length = LivePeriod };
		var emaOpen = new ExponentialMovingAverage { Length = LivePeriod };

		var trail = new SmoothedMovingAverage { Length = TrailPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jaw);
			DrawIndicator(area, teeth);
			DrawIndicator(area, lips);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var median = (candle.HighPrice + candle.LowPrice) / 2m;

			var jawVal = jaw.Process(median);
			var teethVal = teeth.Process(median);
			var lipsVal = lips.Process(median);
			var trailVal = trail.Process(candle.ClosePrice);

			var ma1Val = emaClose.Process(candle.ClosePrice);
			var ma2Val = emaWeighted.Process((candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m);
			var ma3Val = emaTypical.Process((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);
			var ma4Val = emaMedian.Process(median);
			var ma5Val = emaOpen.Process(candle.OpenPrice);
			if (!jawVal.IsFinal || !teethVal.IsFinal || !lipsVal.IsFinal ||
				!ma1Val.IsFinal || !ma2Val.IsFinal || !ma3Val.IsFinal || !ma4Val.IsFinal || !ma5Val.IsFinal ||
				!trailVal.IsFinal)
				return;

			var jawMa = jawVal.GetValue<decimal>();
			var teethMa = teethVal.GetValue<decimal>();
			var lipsMa = lipsVal.GetValue<decimal>();
			var ma1 = ma1Val.GetValue<decimal>();
			var ma2 = ma2Val.GetValue<decimal>();
			var ma3 = ma3Val.GetValue<decimal>();
			var ma4 = ma4Val.GetValue<decimal>();
			var ma5 = ma5Val.GetValue<decimal>();
			var trailPrev = _trailPrev ?? trailVal.GetValue<decimal>();

			var hourOk = !CheckHour || (candle.OpenTime.Hour > StartHour && candle.OpenTime.Hour < EndHour);

			var bull = _prevLips < _prevJaws && lipsMa > jawMa && teethMa < jawMa &&
				ma1 > ma2 && ma2 > ma3 && ma3 > ma4 && ma4 > ma5;

			var bear = _prevLips > _prevJaws && lipsMa < jawMa && teethMa > jawMa &&
				ma1 < ma2 && ma2 < ma3 && ma3 < ma4 && ma4 < ma5;

			if (_lastTrend == 0)
			{
				if (bull) _lastTrend = 1;
				else if (bear) _lastTrend = 2;
			}

			if (Position == 0 && hourOk)
			{
				if (bull && _lastTrend == 2)
				{
					BuyMarket(Volume);

					if (StopLoss > 0)
						StartProtection(stop: new Unit(StopLoss, UnitTypes.Absolute));

					_lastTrend = 1;
				}
				else if (bear && _lastTrend == 1)
				{
					SellMarket(Volume);

					if (StopLoss > 0)
						StartProtection(stop: new Unit(StopLoss, UnitTypes.Absolute));

					_lastTrend = 2;
				}
			}
			else if (Position > 0 && candle.ClosePrice < trailPrev)
			{
				SellMarket(Position);
			}
			else if (Position < 0 && candle.ClosePrice > trailPrev)
			{
				BuyMarket(Math.Abs(Position));
			}

			_prevLips = lipsMa;
			_prevJaws = jawMa;
			_trailPrev = trailVal.GetValue<decimal>();
		}
	}
}