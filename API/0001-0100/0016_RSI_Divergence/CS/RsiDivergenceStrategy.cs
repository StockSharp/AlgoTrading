namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades divergences between consecutive confirmed price pivots and their RSI values.
/// A three-bar pivot is confirmed only after the bar to its right has closed.
/// </summary>
public class RsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly List<(decimal High, decimal Low, decimal Rsi)> _bars = new();
	private decimal _previousLowPrice;
	private decimal _previousLowRsi;
	private decimal _previousHighPrice;
	private decimal _previousHighRsi;
	private bool _hasLow;
	private bool _hasHigh;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiDivergenceStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero();
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetRange(0m, 100m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetPivots();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetPivots();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ResetPivots()
	{
		_bars.Clear();
		_previousLowPrice = _previousLowRsi = _previousHighPrice = _previousHighRsi = 0m;
		_hasLow = _hasHigh = false;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		_bars.Add((candle.HighPrice, candle.LowPrice, rsiValue));
		if (_bars.Count < 3)
			return;

		var left = _bars[0];
		var pivot = _bars[1];
		var right = _bars[2];
		var bullish = false;
		var bearish = false;

		if (pivot.Low < left.Low && pivot.Low < right.Low)
		{
			bullish = _hasLow && pivot.Low < _previousLowPrice && pivot.Rsi > _previousLowRsi;
			_previousLowPrice = pivot.Low;
			_previousLowRsi = pivot.Rsi;
			_hasLow = true;
		}

		if (pivot.High > left.High && pivot.High > right.High)
		{
			bearish = _hasHigh && pivot.High > _previousHighPrice && pivot.Rsi < _previousHighRsi;
			_previousHighPrice = pivot.High;
			_previousHighRsi = pivot.Rsi;
			_hasHigh = true;
		}
		_bars.RemoveAt(0);

		if (bullish && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
