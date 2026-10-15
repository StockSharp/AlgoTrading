namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades direction changes of ATR bands around a center of confirmed swing pivots.
/// </summary>
public class PivotPointSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<(decimal High, decimal Low)> _pivots = [];
	private decimal? _center;
	private decimal? _support;
	private decimal? _resistance;
	private decimal? _previousClose;
	private bool? _pivotUp;

	/// <summary>
	/// Initializes the published pivot period, ATR settings and five-minute candles.
	/// </summary>
	public PivotPointSupertrendStrategy()
	{
		_pivotPeriod = Param(nameof(PivotPeriod), 2).SetGreaterThanZero()
			.SetDisplay("Pivot Period", "Finished candles on each side of a confirmed pivot", "Indicators");
		_atrFactor = Param(nameof(AtrFactor), 3m).SetGreaterThanZero()
			.SetDisplay("ATR Factor", "ATR width of the pivot bands", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 10).SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR period of the pivot bands", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of finished candles", "General");
	}

	/// <summary>
	/// Number of finished candles required on each side of a pivot.
	/// </summary>
	public int PivotPeriod { get => _pivotPeriod.Value; set => _pivotPeriod.Value = value; }

	/// <summary>
	/// Wilder ATR multiplier for the pivot bands.
	/// </summary>
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }

	/// <summary>
	/// Wilder ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Candle type; five minutes by default.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetSignals();
	}

	private void ResetSignals()
	{
		_pivots.Clear();
		_center = _support = _resistance = _previousClose = null;
		_pivotUp = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetSignals();
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(atr, ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var previousClose = _previousClose ?? candle.ClosePrice;
		_previousClose = candle.ClosePrice;
		_pivots.Add((candle.HighPrice, candle.LowPrice));
		if (_pivots.Count > 2 * PivotPeriod + 1)
			_pivots.RemoveAt(0);
		if (_pivots.Count == 2 * PivotPeriod + 1)
		{
			var candidate = _pivots[PivotPeriod];
			var isHigh = true;
			var isLow = true;
			for (var i = 0; i < _pivots.Count; i++)
			{
				if (i == PivotPeriod)
					continue;
				isHigh &= candidate.High > _pivots[i].High;
				isLow &= candidate.Low < _pivots[i].Low;
			}
			if (isHigh || isLow)
			{
				var pivot = isHigh ? candidate.High : candidate.Low;
				_center = _center is decimal center ? (2m * center + pivot) / 3m : pivot;
			}
		}
		var previousPivotUp = _pivotUp;
		if (_center is decimal pivotCenter && atrValue.IsFormed && !atrValue.IsEmpty)
		{
			var width = AtrFactor * atrValue.ToDecimal();
			var oldSupport = _support;
			var oldResistance = _resistance;
			_support = oldSupport is decimal support && previousClose > support ? Math.Max(pivotCenter - width, support) : pivotCenter - width;
			_resistance = oldResistance is decimal resistance && previousClose < resistance ? Math.Min(pivotCenter + width, resistance) : pivotCenter + width;
			_pivotUp = oldResistance is decimal upper && candle.ClosePrice > upper ? true
				: oldSupport is decimal lower && candle.ClosePrice < lower ? false : _pivotUp ?? true;
		}
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		if (previousPivotUp == false && _pivotUp == true && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (previousPivotUp == true && _pivotUp == false && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
