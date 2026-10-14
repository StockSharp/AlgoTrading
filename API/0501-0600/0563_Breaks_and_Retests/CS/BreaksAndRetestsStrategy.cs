using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breaks and retests strategy.
/// Resistance and support are the highest and lowest closes of the previous LookbackPeriod candles. A close above resistance goes long
/// and a close below support goes short, reversing an opposite position. When not already in the breakout direction, a retest of the
/// broken level between RetestBarsSinceBreakout and RetestBarsSinceBreakout + RetestDetectionLimit bars after the breakout (the
/// candle touches the level and closes back on the breakout side) also enters. A StopLossPercent stop protects the trade until the
/// close is ProfitThresholdPercent in profit, after which a trailing stop TrailingStopGapPercent from the best close takes over.
/// </summary>
public class BreaksAndRetestsStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _retestBarsSinceBreakout;
	private readonly StrategyParam<int> _retestDetectionLimit;
	private readonly StrategyParam<decimal> _profitThresholdPercent;
	private readonly StrategyParam<decimal> _trailingStopGapPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();

	private decimal? _brokenResistance;
	private int _barsSinceBullBreak;
	private decimal? _brokenSupport;
	private int _barsSinceBearBreak;

	private decimal _entryPrice;
	private decimal _bestPrice;
	private bool _trailingActive;

	/// <summary>
	/// Previous candles whose closes define support and resistance.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Bars after a breakout before a retest can be detected.
	/// </summary>
	public int RetestBarsSinceBreakout
	{
		get => _retestBarsSinceBreakout.Value;
		set => _retestBarsSinceBreakout.Value = value;
	}

	/// <summary>
	/// Additional bars during which a retest is still detected.
	/// </summary>
	public int RetestDetectionLimit
	{
		get => _retestDetectionLimit.Value;
		set => _retestDetectionLimit.Value = value;
	}

	/// <summary>
	/// Profit in percent that switches to the trailing stop.
	/// </summary>
	public decimal ProfitThresholdPercent
	{
		get => _profitThresholdPercent.Value;
		set => _profitThresholdPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop distance from the best close in percent.
	/// </summary>
	public decimal TrailingStopGapPercent
	{
		get => _trailingStopGapPercent.Value;
		set => _trailingStopGapPercent.Value = value;
	}

	/// <summary>
	/// Initial stop loss in percent from the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public BreaksAndRetestsStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Previous candles whose closes define support and resistance", "Levels");

		_retestBarsSinceBreakout = Param(nameof(RetestBarsSinceBreakout), 2)
			.SetNotNegative()
			.SetDisplay("Retest Bars Since Breakout", "Bars after a breakout before a retest can be detected", "Retest");

		_retestDetectionLimit = Param(nameof(RetestDetectionLimit), 2)
			.SetNotNegative()
			.SetDisplay("Retest Detection Limit", "Additional bars during which a retest is still detected", "Retest");

		_profitThresholdPercent = Param(nameof(ProfitThresholdPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Threshold %", "Profit that switches to the trailing stop", "Risk");

		_trailingStopGapPercent = Param(nameof(TrailingStopGapPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Gap %", "Trailing stop distance from the best close", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Initial stop loss from the entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

	private void ResetState()
	{
		_closes.Clear();
		_brokenResistance = null;
		_brokenSupport = null;
		_barsSinceBullBreak = 0;
		_barsSinceBearBreak = 0;
		_entryPrice = 0;
		_bestPrice = 0;
		_trailingActive = false;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Levels come from the closes before this candle.
		decimal? resistance = null;
		decimal? support = null;
		if (_closes.Count == LookbackPeriod)
		{
			resistance = _closes.Max();
			support = _closes.Min();
		}

		_closes.Enqueue(close);
		while (_closes.Count > LookbackPeriod)
			_closes.Dequeue();

		if (_brokenResistance != null)
			_barsSinceBullBreak++;
		if (_brokenSupport != null)
			_barsSinceBearBreak++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 && CheckStops(candle))
			return;

		if (resistance is not decimal res || support is not decimal sup)
			return;

		var bullBreak = close > res;
		var bearBreak = close < sup;

		var maxRetestBars = RetestBarsSinceBreakout + RetestDetectionLimit;

		var bullRetest = !bullBreak && _brokenResistance is decimal brokenRes
			&& _barsSinceBullBreak >= RetestBarsSinceBreakout && _barsSinceBullBreak <= maxRetestBars
			&& candle.LowPrice <= brokenRes && close > brokenRes;

		var bearRetest = !bearBreak && _brokenSupport is decimal brokenSup
			&& _barsSinceBearBreak >= RetestBarsSinceBreakout && _barsSinceBearBreak <= maxRetestBars
			&& candle.HighPrice >= brokenSup && close < brokenSup;

		if (bullBreak)
		{
			_brokenResistance = res;
			_barsSinceBullBreak = 0;
		}
		else if (_brokenResistance != null && _barsSinceBullBreak > maxRetestBars)
		{
			_brokenResistance = null;
		}

		if (bearBreak)
		{
			_brokenSupport = sup;
			_barsSinceBearBreak = 0;
		}
		else if (_brokenSupport != null && _barsSinceBearBreak > maxRetestBars)
		{
			_brokenSupport = null;
		}

		if ((bullBreak || bullRetest) && Position <= 0)
		{
			if (bullRetest)
				_brokenResistance = null;

			Enter(true, close);
		}
		else if ((bearBreak || bearRetest) && Position >= 0)
		{
			if (bearRetest)
				_brokenSupport = null;

			Enter(false, close);
		}
	}

	private void Enter(bool isLong, decimal price)
	{
		var volume = Volume + Math.Abs(Position);

		if (isLong)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_entryPrice = price;
		_bestPrice = price;
		_trailingActive = false;
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (_entryPrice <= 0)
			return false;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			_bestPrice = Math.Max(_bestPrice, close);

			if (!_trailingActive && (close - _entryPrice) / _entryPrice * 100m >= ProfitThresholdPercent)
				_trailingActive = true;

			var stop = _trailingActive
				? _bestPrice * (1m - TrailingStopGapPercent / 100m)
				: _entryPrice * (1m - StopLossPercent / 100m);

			if (candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_entryPrice = 0;
				return true;
			}
		}
		else if (Position < 0)
		{
			_bestPrice = Math.Min(_bestPrice, close);

			if (!_trailingActive && (_entryPrice - close) / _entryPrice * 100m >= ProfitThresholdPercent)
				_trailingActive = true;

			var stop = _trailingActive
				? _bestPrice * (1m + TrailingStopGapPercent / 100m)
				: _entryPrice * (1m + StopLossPercent / 100m);

			if (candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				_entryPrice = 0;
				return true;
			}
		}

		return false;
	}
}
