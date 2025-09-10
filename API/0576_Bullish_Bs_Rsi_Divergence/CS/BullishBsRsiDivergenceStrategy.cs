using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI divergence strategy using pivots.
/// </summary>
public class BullishBsRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _pivotRight;
	private readonly StrategyParam<int> _pivotLeft;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _rangeUpper;
	private readonly StrategyParam<int> _rangeLower;
	private readonly StrategyParam<TrailingStopType> _stopType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private readonly List<decimal> _rsiBuffer = [];
	private readonly List<decimal> _lowBuffer = [];
	private readonly List<decimal> _highBuffer = [];
	private int _barIndex;

	private decimal? _prevPivotLowRsi;
	private decimal? _prevPivotLowPrice;
	private int _prevPivotLowIndex = int.MinValue;

	private decimal? _prevPivotHighRsi;
	private decimal? _prevPivotHighPrice;
	private int _prevPivotHighIndex = int.MinValue;

	private decimal? _trailingStop;
	private decimal _prevRsi;

	private readonly DateTimeOffset _startDate = new(2019, 1, 1, 0, 0, 0, TimeSpan.Zero);
	private readonly DateTimeOffset _endDate = new(2069, 12, 31, 23, 59, 0, TimeSpan.Zero);


	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int PivotLookbackRight { get => _pivotRight.Value; set => _pivotRight.Value = value; }
	public int PivotLookbackLeft { get => _pivotLeft.Value; set => _pivotLeft.Value = value; }
	public int TakeProfitRsiLevel { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int RangeUpper { get => _rangeUpper.Value; set => _rangeUpper.Value = value; }
	public int RangeLower { get => _rangeLower.Value; set => _rangeLower.Value = value; }
	public TrailingStopType StopType { get => _stopType.Value; set => _stopType.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	public BullishBsRsiDivergenceStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 9).SetDisplay("RSI Period", "RSI calculation period", "Indicators").SetCanOptimize(true).SetOptimize(5, 20, 1);
		_pivotRight = Param(nameof(PivotLookbackRight), 3).SetDisplay("Pivot Right", "Bars to the right", "General");
		_pivotLeft = Param(nameof(PivotLookbackLeft), 1).SetDisplay("Pivot Left", "Bars to the left", "General");
		_takeProfit = Param(nameof(TakeProfitRsiLevel), 80).SetDisplay("RSI Take Profit", "RSI level", "Risk Management");
		_rangeUpper = Param(nameof(RangeUpper), 60).SetDisplay("Range Upper", "Max bars between pivots", "General");
		_rangeLower = Param(nameof(RangeLower), 5).SetDisplay("Range Lower", "Min bars between pivots", "General");
		_stopType = Param(nameof(StopType), TrailingStopType.None).SetDisplay("Stop Type", "Trailing stop type", "Risk Management");
		_stopLoss = Param(nameof(StopLoss), 5m).SetDisplay("Stop Loss %", "Percent stop", "Risk Management");
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR Length", "ATR period", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3.5m).SetDisplay("ATR Multiplier", "ATR multiplier", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_rsiBuffer.Clear();
		_lowBuffer.Clear();
		_highBuffer.Clear();
		_barIndex = 0;
		_prevPivotLowRsi = null;
		_prevPivotLowPrice = null;
		_prevPivotLowIndex = int.MinValue;
		_prevPivotHighRsi = null;
		_prevPivotHighPrice = null;
		_prevPivotHighIndex = int.MinValue;
		_trailingStop = null;
		_prevRsi = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, _atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < _startDate || candle.OpenTime > _endDate)
		{
			if (Position > 0)
				RegisterSell();
			return;
		}

		_barIndex++;
		_rsiBuffer.Add(rsi);
		_lowBuffer.Add(candle.LowPrice);
		_highBuffer.Add(candle.HighPrice);
		var maxCount = PivotLookbackLeft + PivotLookbackRight + 1;
		if (_rsiBuffer.Count > maxCount)
		{
			_rsiBuffer.RemoveAt(0);
			_lowBuffer.RemoveAt(0);
			_highBuffer.RemoveAt(0);
		}

		var bullCond = false;
		var hiddenBullCond = false;
		var bearCond = false;

		if (_rsiBuffer.Count == maxCount)
		{
			var idx = PivotLookbackLeft;
			var pivotRsi = _rsiBuffer[idx];
			var pivotLow = _lowBuffer[idx];
			var pivotHigh = _highBuffer[idx];
			var isLow = true;
			var isHigh = true;
			for (var i = 0; i < maxCount; i++)
			{
				if (i == idx)
					continue;
				if (_rsiBuffer[i] <= pivotRsi)
					isLow = false;
				if (_rsiBuffer[i] >= pivotRsi)
					isHigh = false;
			}
			var pivotIndex = _barIndex - PivotLookbackRight - 1;

			if (isLow)
			{
				if (_prevPivotLowRsi is decimal pr && _prevPivotLowPrice is decimal pp)
				{
					var dist = pivotIndex - _prevPivotLowIndex;
					var inRange = dist >= RangeLower && dist <= RangeUpper;
					var oscHL = pivotRsi > pr && inRange;
					var priceLL = pivotLow < pp;
					bullCond = priceLL && oscHL;
					var oscLL = pivotRsi < pr && inRange;
					var priceHL = pivotLow > pp;
					hiddenBullCond = priceHL && oscLL;
				}
				_prevPivotLowRsi = pivotRsi;
				_prevPivotLowPrice = pivotLow;
				_prevPivotLowIndex = pivotIndex;
			}

			if (isHigh)
			{
				if (_prevPivotHighRsi is decimal pr && _prevPivotHighPrice is decimal pp)
				{
					var dist = pivotIndex - _prevPivotHighIndex;
					var inRange = dist >= RangeLower && dist <= RangeUpper;
					var oscLH = pivotRsi < pr && inRange;
					var priceHH = pivotHigh > pp;
					bearCond = priceHH && oscLH;
				}
				_prevPivotHighRsi = pivotRsi;
				_prevPivotHighPrice = pivotHigh;
				_prevPivotHighIndex = pivotIndex;
			}
		}

		var longCondition = bullCond || hiddenBullCond;
		var rsiCross = _prevRsi < TakeProfitRsiLevel && rsi >= TakeProfitRsiLevel;
		_prevRsi = rsi;
		var longCloseCondition = rsiCross || bearCond;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (longCondition && Position <= 0)
		{
			RegisterBuy();
			_trailingStop = null;
		}
		else if (Position > 0)
		{
			if (StopType != TrailingStopType.None)
			{
				var slVal = StopType == TrailingStopType.Atr ? atr * AtrMultiplier : candle.ClosePrice * StopLoss / 100m;
				var newStop = candle.LowPrice - slVal;
				_trailingStop = _trailingStop is decimal prev ? Math.Max(prev, newStop) : newStop;
				if (candle.ClosePrice < _trailingStop)
				{
					RegisterSell();
					_trailingStop = null;
					return;
				}
			}
			if (longCloseCondition)
			{
				RegisterSell();
				_trailingStop = null;
			}
		}
	}

}

/// <summary>
/// Trailing stop-loss types.
/// </summary>
public enum TrailingStopType
{
	/// <summary> No trailing stop. </summary>
	None,
	/// <summary> ATR based trailing stop. </summary>
	Atr,
	/// <summary> Percent based trailing stop. </summary>
	Percent
}
