using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// LANZ Strategy 4.0 Backtest.
/// A pivot high (low) is a candle whose high (low) is the extreme of the SwingLength candles on each side of it. A close crossing above
/// the last pivot high goes long and a close crossing below the last pivot low goes short, reversing an opposite position. The stop sits
/// SlBufferPoints price steps beyond the opposite pivot, the target RiskReward times the stop distance away, and the volume risks
/// RiskPercent of equity with PipValueUsd per price step and lot.
/// </summary>
public class Lanz40BacktestStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<decimal> _slBufferPoints;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _pipValueUsd;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];

	private decimal? _pivotHigh;
	private decimal? _pivotLow;
	private decimal? _prevClose;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Candles on each side of a pivot.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Stop buffer beyond the pivot in price steps.
	/// </summary>
	public decimal SlBufferPoints
	{
		get => _slBufferPoints.Value;
		set => _slBufferPoints.Value = value;
	}

	/// <summary>
	/// Take profit as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Percent of equity risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Money value of one price step for one lot.
	/// </summary>
	public decimal PipValueUsd
	{
		get => _pipValueUsd.Value;
		set => _pipValueUsd.Value = value;
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
	/// Initialize <see cref="Lanz40BacktestStrategy"/>.
	/// </summary>
	public Lanz40BacktestStrategy()
	{
		_swingLength = Param(nameof(SwingLength), 180)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Candles on each side of a pivot", "General");

		_slBufferPoints = Param(nameof(SlBufferPoints), 50m)
			.SetNotNegative()
			.SetDisplay("SL Buffer", "Stop buffer beyond the pivot in price steps", "Risk");

		_riskReward = Param(nameof(RiskReward), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit as a multiple of the stop distance", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk");

		_pipValueUsd = Param(nameof(PipValueUsd), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Value USD", "Money value of one price step for one lot", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_highs.Clear();
		_lows.Clear();
		_pivotHigh = _pivotLow = null;
		_prevClose = null;
		_stopPrice = _takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdatePivots(candle);

		var prevClose = _prevClose;
		var close = candle.ClosePrice;
		_prevClose = close;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake
			&& (candle.LowPrice <= longStop || candle.HighPrice >= longTake))
		{
			SellMarket(Position);
			_stopPrice = _takePrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake
			&& (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake))
		{
			BuyMarket(-Position);
			_stopPrice = _takePrice = null;
			return;
		}

		if (prevClose is not decimal last || _pivotHigh is not decimal pivotHigh || _pivotLow is not decimal pivotLow)
			return;

		var step = Security.PriceStep ?? 1m;
		var buffer = SlBufferPoints * step;

		if (last <= pivotHigh && close > pivotHigh && Position <= 0)
		{
			var stop = pivotLow - buffer;
			if (stop >= close)
				return;

			BuyMarket(CalculateVolume(close - stop, step) + Math.Abs(Position));
			_stopPrice = stop;
			_takePrice = close + (close - stop) * RiskReward;
		}
		else if (last >= pivotLow && close < pivotLow && Position >= 0)
		{
			var stop = pivotHigh + buffer;
			if (stop <= close)
				return;

			SellMarket(CalculateVolume(stop - close, step) + Math.Abs(Position));
			_stopPrice = stop;
			_takePrice = close - (stop - close) * RiskReward;
		}
	}

	private void UpdatePivots(ICandleMessage candle)
	{
		var window = SwingLength * 2 + 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > window)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < window)
			return;

		// The candle in the middle of the window is a pivot once SwingLength candles have closed after it.
		var high = _highs[SwingLength];
		if (high == _highs.Max())
			_pivotHigh = high;

		var low = _lows[SwingLength];
		if (low == _lows.Min())
			_pivotLow = low;
	}

	private decimal CalculateVolume(decimal stopDistance, decimal step)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var steps = stopDistance / step;
		var volume = steps > 0m ? equity * RiskPercent / 100m / (steps * PipValueUsd) : 0m;

		if (Security.VolumeStep is decimal volumeStep && volumeStep > 0m)
			volume = Math.Floor(volume / volumeStep) * volumeStep;

		return volume > 0m ? volume : Volume;
	}
}
