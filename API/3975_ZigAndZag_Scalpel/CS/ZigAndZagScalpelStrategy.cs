namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// ZigAndZagScalpel translation that trades on breakouts from short-term pivots confirmed by a long-term ZigZag trend.
/// </summary>
public class ZigAndZagScalpelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _keelOverLength;
	private readonly StrategyParam<int> _slalomLength;
	private readonly StrategyParam<decimal> _deviationPoints;
	private readonly StrategyParam<int> _backstep;
	private readonly StrategyParam<decimal> _breakoutDistancePoints;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<bool> _closeOnOppositePivot;

	private decimal _priceStep = 1m;
	private decimal _deviation;
	private decimal _breakoutDistance;

	private decimal _previousMajorPivot;
	private decimal _lastMajorPivot;
	private decimal _previousMinorPivot;
	private decimal _lastMinorPivot;
	private DateTime _currentDay = DateTime.MinValue;
	private int _tradesToday;
	private bool _trendUp;
	private PivotTypes _lastMinorPivotType = PivotTypes.None;
	private bool _minorPivotUsed;

	/// <summary>
	/// Initializes a new instance of the <see cref="ZigAndZagScalpelStrategy"/> class.
	/// </summary>
	public ZigAndZagScalpelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for all calculations", "General");

		_keelOverLength = Param(nameof(KeelOverLength), 55)
		.SetDisplay("KeelOver Length", "Lookback for the trend-defining ZigZag", "ZigZag");

		_slalomLength = Param(nameof(SlalomLength), 17)
		.SetDisplay("Slalom Length", "Lookback for the entry ZigZag", "ZigZag");

		_deviationPoints = Param(nameof(DeviationPoints), 5m)
		.SetDisplay("Deviation (pts)", "Minimum price movement to confirm a new pivot", "ZigZag");

		_backstep = Param(nameof(Backstep), 3)
		.SetDisplay("Backstep", "Bars that must separate consecutive pivots", "ZigZag");

		_breakoutDistancePoints = Param(nameof(BreakoutDistancePoints), 2m)
		.SetDisplay("Breakout Distance (pts)", "Required distance from the pivot to trigger an order", "Trading");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 1)
		.SetDisplay("Max Trades Per Day", "Daily limit matching the original expert advisor", "Trading");

		_closeOnOppositePivot = Param(nameof(CloseOnOppositePivot), true)
		.SetDisplay("Close On Opposite Pivot", "Exit when the entry ZigZag prints the opposite swing", "Risk");
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
	/// Lookback for the long-term ZigZag that defines the global trend.
	/// </summary>
	public int KeelOverLength
	{
		get => _keelOverLength.Value;
		set => _keelOverLength.Value = value;
	}

	/// <summary>
	/// Lookback for the short-term ZigZag that produces entries.
	/// </summary>
	public int SlalomLength
	{
		get => _slalomLength.Value;
		set => _slalomLength.Value = value;
	}

	/// <summary>
	/// Minimum movement in points required to register a ZigZag swing.
	/// </summary>
	public decimal DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	/// <summary>
	/// Bars that must separate consecutive ZigZag swings.
	/// </summary>
	public int Backstep
	{
		get => _backstep.Value;
		set => _backstep.Value = value;
	}

	/// <summary>
	/// Distance from a pivot (in points) required before firing an order.
	/// </summary>
	public decimal BreakoutDistancePoints
	{
		get => _breakoutDistancePoints.Value;
		set => _breakoutDistancePoints.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed per trading day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Determines whether open positions should be closed on the opposite entry pivot.
	/// </summary>
	public bool CloseOnOppositePivot
	{
		get => _closeOnOppositePivot.Value;
		set => _closeOnOppositePivot.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		_deviation = Math.Max(_priceStep, Math.Abs(DeviationPoints) * _priceStep);
		_breakoutDistance = Math.Max(0m, Math.Abs(BreakoutDistancePoints) * _priceStep);

		var majorZigZag = new ZigZagIndicator
		{
			Depth = Math.Max(2, KeelOverLength),
			Deviation = _deviation,
			BackStep = Math.Max(1, Backstep)
		};

		var minorZigZag = new ZigZagIndicator
		{
			Depth = Math.Max(2, SlalomLength),
			Deviation = _deviation,
			BackStep = Math.Max(1, Backstep)
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(majorZigZag, minorZigZag, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, majorZigZag);
			DrawIndicator(area, minorZigZag);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal majorValue, decimal minorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateDailyCounter(candle.OpenTime.UtcDateTime);

		UpdateMajorTrend(majorValue);
		UpdateMinorPivot(minorValue);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ManageExistingPosition();

		if (Position != 0)
		return;

		if (_minorPivotUsed)
		return;

		if (_lastMinorPivotType == PivotTypes.None)
		return;

		if (_tradesToday >= MaxTradesPerDay)
		return;

		var navel = CalculateNavel(candle);

		if (_lastMinorPivotType == PivotTypes.Low && _trendUp)
		{
			if (navel - _lastMinorPivot >= _breakoutDistance)
			{
				BuyMarket();
				_minorPivotUsed = true;
				_tradesToday++;
			}
		}
		else if (_lastMinorPivotType == PivotTypes.High && !_trendUp)
		{
			if (_lastMinorPivot - navel >= _breakoutDistance)
			{
				SellMarket();
				_minorPivotUsed = true;
				_tradesToday++;
			}
		}
	}

	private void UpdateDailyCounter(DateTime time)
	{
		var date = time.Date;
		if (date == _currentDay)
		return;

		_currentDay = date;
		_tradesToday = 0;
	}

	private void UpdateMajorTrend(decimal majorValue)
	{
		if (majorValue == 0m)
		return;

		if (_lastMajorPivot == 0m)
		{
			_lastMajorPivot = majorValue;
			_previousMajorPivot = majorValue;
			return;
		}

		if (majorValue == _lastMajorPivot)
		return;

		_previousMajorPivot = _lastMajorPivot;
		_lastMajorPivot = majorValue;
		_trendUp = _lastMajorPivot < _previousMajorPivot;
	}

	private void UpdateMinorPivot(decimal minorValue)
	{
		if (minorValue == 0m)
		return;

		if (_lastMinorPivot == 0m)
		{
			_lastMinorPivot = minorValue;
			_previousMinorPivot = minorValue;
			_lastMinorPivotType = PivotTypes.Low;
			_minorPivotUsed = false;
			return;
		}

		if (minorValue == _lastMinorPivot)
		return;

		_previousMinorPivot = _lastMinorPivot;
		_lastMinorPivot = minorValue;
		_lastMinorPivotType = _lastMinorPivot < _previousMinorPivot ? PivotTypes.Low : PivotTypes.High;
		_minorPivotUsed = false;
	}

	private void ManageExistingPosition()
	{
		if (Position > 0)
		{
			if (!_trendUp || (CloseOnOppositePivot && _lastMinorPivotType == PivotTypes.High))
			ClosePosition();
		}
		else if (Position < 0)
		{
			if (_trendUp || (CloseOnOppositePivot && _lastMinorPivotType == PivotTypes.Low))
			ClosePosition();
		}
	}

	private static decimal CalculateNavel(ICandleMessage candle)
	{
		return (5m * candle.ClosePrice + 2m * candle.OpenPrice + candle.HighPrice + candle.LowPrice) / 9m;
	}

	private enum PivotTypes
	{
		None,
		Low,
		High
	}
}

