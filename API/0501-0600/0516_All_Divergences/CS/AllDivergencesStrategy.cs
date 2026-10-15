using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// All Divergences strategy.
/// Confirms price swing lows and highs as pivots with PivotSide bars on each side and compares each new pivot with the previous
/// one. A lower price low with a higher RSI low while the close is above the moving average goes long; a higher price high
/// with a lower RSI high while the close is below the moving average goes short, reversing an opposite position. A position
/// is also closed after MaRiskCandles consecutive closes on the wrong side of the moving average, and optional percent
/// stop-loss and take-profit protect it.
/// </summary>
public class AllDivergencesStrategy : Strategy
{
	private const int _pivotSide = 5;

	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _maRiskCandles;
	private readonly StrategyParam<bool> _useProtection;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal high, decimal low, decimal rsi)> _bars = [];
	private decimal? _lastPivotLow;
	private decimal _lastPivotLowRsi;
	private decimal? _lastPivotHigh;
	private decimal _lastPivotHighRsi;
	private int _closesBelowMa;
	private int _closesAboveMa;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Consecutive closes against the moving average that close a position.
	/// </summary>
	public int MaRiskCandles
	{
		get => _maRiskCandles.Value;
		set => _maRiskCandles.Value = value;
	}

	/// <summary>
	/// Enable percent stop-loss and take-profit.
	/// </summary>
	public bool UseProtection
	{
		get => _useProtection.Value;
		set => _useProtection.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage used when protection is enabled.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage used when protection is enabled.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AllDivergencesStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_maRiskCandles = Param(nameof(MaRiskCandles), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Risk Candles", "Consecutive closes against the MA that close a position", "Risk");

		_useProtection = Param(nameof(UseProtection), false)
			.SetDisplay("Use Protection", "Enable percent stop-loss and take-profit", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage used when protection is enabled", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage used when protection is enabled", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_bars.Clear();
		_lastPivotLow = null;
		_lastPivotLowRsi = 0m;
		_lastPivotHigh = null;
		_lastPivotHighRsi = 0m;
		_closesBelowMa = 0;
		_closesAboveMa = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ma, ProcessCandle)
			.Start();

		if (UseProtection)
			StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_bars.Add((candle.HighPrice, candle.LowPrice, rsi));

		var window = 2 * _pivotSide + 1;
		if (_bars.Count > window)
			_bars.RemoveAt(0);

		var close = candle.ClosePrice;
		_closesBelowMa = close < ma ? _closesBelowMa + 1 : 0;
		_closesAboveMa = close > ma ? _closesAboveMa + 1 : 0;

		if (_bars.Count < window)
			return;

		var bullishDivergence = false;
		var bearishDivergence = false;

		// The middle bar of the window is a pivot once PivotSide bars on each side have finished.
		var (pivotHigh, pivotLow, pivotRsi) = _bars[_pivotSide];
		var isPivotLow = true;
		var isPivotHigh = true;

		for (var i = 0; i < window; i++)
		{
			if (i == _pivotSide)
				continue;

			if (_bars[i].low <= pivotLow)
				isPivotLow = false;

			if (_bars[i].high >= pivotHigh)
				isPivotHigh = false;
		}

		if (isPivotLow)
		{
			if (_lastPivotLow is decimal prevLow)
				bullishDivergence = pivotLow < prevLow && pivotRsi > _lastPivotLowRsi;

			_lastPivotLow = pivotLow;
			_lastPivotLowRsi = pivotRsi;
		}

		if (isPivotHigh)
		{
			if (_lastPivotHigh is decimal prevHigh)
				bearishDivergence = pivotHigh > prevHigh && pivotRsi < _lastPivotHighRsi;

			_lastPivotHigh = pivotHigh;
			_lastPivotHighRsi = pivotRsi;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullishDivergence && close > ma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearishDivergence && close < ma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && _closesBelowMa >= MaRiskCandles)
			SellMarket(Position);
		else if (Position < 0 && _closesAboveMa >= MaRiskCandles)
			BuyMarket(-Position);
	}
}
