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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion spread strategy using short-term price oscillations.
/// Simplified from the two-leg MetaTrader "Spreader" to single security.
/// Enters when price deviates from its moving average and exits on profit target.
/// </summary>
public class SpreaderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetProfit;
	private readonly StrategyParam<int> _shiftLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private SimpleMovingAverage _sma;
	private readonly List<decimal> _closes = new();
	private decimal _entryPrice;

	/// <summary>
	/// Profit target in price units.
	/// </summary>
	public decimal TargetProfit
	{
		get => _targetProfit.Value;
		set => _targetProfit.Value = value;
	}

	/// <summary>
	/// Number of bars between comparison points for pullback detection.
	/// </summary>
	public int ShiftLength
	{
		get => _shiftLength.Value;
		set => _shiftLength.Value = value;
	}

	/// <summary>
	/// Candle type for spread calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for mean reversion.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SpreaderStrategy()
	{
		_targetProfit = Param(nameof(TargetProfit), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Target Profit", "Profit target for closing position", "Risk")
			.SetOptimize(1m, 20m, 1m);

		_shiftLength = Param(nameof(ShiftLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Shift Length", "Bars between comparison points for pullback detection", "Logic")
			.SetOptimize(10, 60, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_sma = null;
		_closes.Clear();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaPeriod };

		SubscribeCandles(CandleType)
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		_closes.Add(price);

		// Trim buffer
		var maxCount = Math.Max(1, ShiftLength * 2 + 1);
		while (_closes.Count > maxCount)
			_closes.RemoveAt(0);

		if (!IsFormed)
			return;

		// Check profit target for open position
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			if (TargetProfit > 0m && pnl >= TargetProfit)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				return;
			}
		}

		// Entry: detect pullback/reversal pattern
		if (Position == 0)
		{
			var shift = ShiftLength;
			var required = shift * 2 + 1;
			if (_closes.Count < required)
				return;

			var idx = _closes.Count - 1;
			var shiftIdx = idx - shift;
			var doubleShiftIdx = idx - shift * 2;

			if (shiftIdx < 0 || doubleShiftIdx < 0)
				return;

			var x1 = _closes[idx] - _closes[shiftIdx];
			var x2 = _closes[shiftIdx] - _closes[doubleShiftIdx];

			// Pullback: recent move opposes the prior move
			if (x1 * x2 >= 0m)
				return;

			// Mean reversion: enter towards the average
			if (price < smaValue && x1 < 0m)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (price > smaValue && x1 > 0m)
			{
				SellMarket();
				_entryPrice = price;
			}
		}
	}
}
