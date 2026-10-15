namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ZigAndZagScalpel translation that trades on breakouts from short-term pivots confirmed by a long-term ZigZag trend.
/// </summary>
public class ZigAndZagScalpelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<bool> _closeOnOppositePivot;

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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for all calculations", "General");

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
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousMajorPivot = 0m;
		_lastMajorPivot = 0m;
		_previousMinorPivot = 0m;
		_lastMinorPivot = 0m;
		_currentDay = DateTime.MinValue;
		_tradesToday = 0;
		_trendUp = false;
		_lastMinorPivotType = PivotTypes.None;
		_minorPivotUsed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var majorZigZag = new ZigZag { Deviation = 0.02m };
		var minorZigZag = new ZigZag { Deviation = 0.005m };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindWithEmpty(majorZigZag, minorZigZag, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal? majorValue, decimal? minorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDailyCounter(candle.OpenTime);

		if (majorValue is not null)
			UpdateMajorTrend(majorValue.Value);

		if (minorValue is not null)
			UpdateMinorPivot(minorValue.Value);

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
			if (navel > _lastMinorPivot)
			{
				BuyMarket();
				_minorPivotUsed = true;
				_tradesToday++;
			}
		}
		else if (_lastMinorPivotType == PivotTypes.High && !_trendUp)
		{
			if (navel < _lastMinorPivot)
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
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_trendUp || (CloseOnOppositePivot && _lastMinorPivotType == PivotTypes.Low))
				BuyMarket(Position.Abs());
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

