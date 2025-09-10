using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout and retest strategy with trailing stop.
/// </summary>
public class BreaksAndRetestsStrategy : Strategy
{
	public enum Direction
	{
		Both,
		LongOnly,
		ShortOnly
	}

	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _retestBarsSinceBreakout;
	private readonly StrategyParam<int> _retestDetectionLimit;
	private readonly StrategyParam<bool> _enableBreakouts;
	private readonly StrategyParam<bool> _enableRetests;
	private readonly StrategyParam<Direction> _tradeDirection;
	private readonly StrategyParam<decimal> _profitThresholdPercent;
	private readonly StrategyParam<decimal> _trailingStopGapPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal? _resistanceLevel;
	private decimal? _supportLevel;
	private int _barsSinceResBreak = int.MaxValue;
	private int _barsSinceSupBreak = int.MaxValue;

	private bool _trailingStopActive;
	private decimal _entryPrice;
	private decimal _highestSinceTrailing;
	private decimal _lowestSinceTrailing;

	/// <summary>
	/// Lookback period for support and resistance calculation.
	/// </summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }

	/// <summary>
	/// Bars after breakout before checking retest.
	/// </summary>
	public int RetestBarsSinceBreakout { get => _retestBarsSinceBreakout.Value; set => _retestBarsSinceBreakout.Value = value; }

	/// <summary>
	/// Maximum bars to validate retest.
	/// </summary>
	public int RetestDetectionLimit { get => _retestDetectionLimit.Value; set => _retestDetectionLimit.Value = value; }

	/// <summary>
	/// Enable breakout entries.
	/// </summary>
	public bool EnableBreakouts { get => _enableBreakouts.Value; set => _enableBreakouts.Value = value; }

	/// <summary>
	/// Enable retest entries.
	/// </summary>
	public bool EnableRetests { get => _enableRetests.Value; set => _enableRetests.Value = value; }

	/// <summary>
	/// Trade direction.
	/// </summary>
	public Direction TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }

	/// <summary>
	/// Profit percent threshold to activate trailing stop.
	/// </summary>
	public decimal ProfitThresholdPercent { get => _profitThresholdPercent.Value; set => _profitThresholdPercent.Value = value; }

	/// <summary>
	/// Gap of trailing stop in percent.
	/// </summary>
	public decimal TrailingStopGapPercent { get => _trailingStopGapPercent.Value; set => _trailingStopGapPercent.Value = value; }

	/// <summary>
	/// Initial stop loss in percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	private bool AllowLong => TradeDirection != Direction.ShortOnly;
	private bool AllowShort => TradeDirection != Direction.LongOnly;

	/// <summary>
	/// Constructor.
	/// </summary>
	public BreaksAndRetestsStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Number of bars for support/resistance", "Levels");

		_retestBarsSinceBreakout = Param(nameof(RetestBarsSinceBreakout), 2)
			.SetGreaterThanZero()
			.SetDisplay("Bars Since Breakout", "Bars after breakout before retest", "Retest");

		_retestDetectionLimit = Param(nameof(RetestDetectionLimit), 2)
			.SetGreaterThanZero()
			.SetDisplay("Retest Detection Limit", "Max bars to validate retest", "Retest");

		_enableBreakouts = Param(nameof(EnableBreakouts), true)
			.SetDisplay("Breakouts", "Allow breakout entries", "General");

		_enableRetests = Param(nameof(EnableRetests), true)
			.SetDisplay("Retests", "Allow retest entries", "General");

		_tradeDirection = Param(nameof(TradeDirection), Direction.Both)
			.SetDisplay("Trade Direction", "Allowed direction", "General");

		_profitThresholdPercent = Param(nameof(ProfitThresholdPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Threshold %", "Activate trailing after profit", "Risk");

		_trailingStopGapPercent = Param(nameof(TrailingStopGapPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Gap %", "Gap for trailing stop", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Initial stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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
		_prevHighest = 0m;
		_prevLowest = 0m;
		_resistanceLevel = null;
		_supportLevel = null;
		_barsSinceResBreak = int.MaxValue;
		_barsSinceSupBreak = int.MaxValue;
		_trailingStopActive = false;
		_entryPrice = 0m;
		_highestSinceTrailing = 0m;
		_lowestSinceTrailing = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_highest.IsFormed && candle.ClosePrice > _prevHighest)
		{
			_resistanceLevel = _prevHighest;
			_barsSinceResBreak = 0;
			if (EnableBreakouts && AllowLong && Position <= 0)
			{
				CancelActiveOrders();
				RegisterBuy();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (_barsSinceResBreak < int.MaxValue)
		{
			_barsSinceResBreak++;
		}

		if (_lowest.IsFormed && candle.ClosePrice < _prevLowest)
		{
			_supportLevel = _prevLowest;
			_barsSinceSupBreak = 0;
			if (EnableBreakouts && AllowShort && Position >= 0)
			{
				CancelActiveOrders();
				RegisterSell();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (_barsSinceSupBreak < int.MaxValue)
		{
			_barsSinceSupBreak++;
		}

		if (EnableRetests && AllowLong && Position <= 0 && _resistanceLevel is decimal res &&
			_barsSinceResBreak > RetestBarsSinceBreakout && _barsSinceResBreak <= RetestDetectionLimit &&
			candle.LowPrice <= res && candle.ClosePrice >= res)
		{
			CancelActiveOrders();
			RegisterBuy();
			_entryPrice = candle.ClosePrice;
			_resistanceLevel = null;
			_barsSinceResBreak = int.MaxValue;
		}

		if (EnableRetests && AllowShort && Position >= 0 && _supportLevel is decimal sup &&
			_barsSinceSupBreak > RetestBarsSinceBreakout && _barsSinceSupBreak <= RetestDetectionLimit &&
			candle.HighPrice >= sup && candle.ClosePrice <= sup)
		{
			CancelActiveOrders();
			RegisterSell();
			_entryPrice = candle.ClosePrice;
			_supportLevel = null;
			_barsSinceSupBreak = int.MaxValue;
		}

		_prevHighest = highest;
		_prevLowest = lowest;

		HandleStop(candle);
	}

	private void HandleStop(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var profitPercent = (candle.ClosePrice - _entryPrice) / _entryPrice * 100m;
			if (!_trailingStopActive && profitPercent >= ProfitThresholdPercent)
			{
				_trailingStopActive = true;
				_highestSinceTrailing = candle.ClosePrice;
			}

			if (_trailingStopActive)
			{
				_highestSinceTrailing = Math.Max(_highestSinceTrailing, candle.ClosePrice);
				var stop = _highestSinceTrailing * (1 - TrailingStopGapPercent / 100m);
				if (candle.ClosePrice <= stop)
				{
					CancelActiveOrders();
					RegisterSell();
					_entryPrice = 0m;
					_trailingStopActive = false;
					_highestSinceTrailing = 0m;
				}
			}
			else
			{
				var stop = _entryPrice * (1 - StopLossPercent / 100m);
				if (candle.ClosePrice <= stop)
				{
					CancelActiveOrders();
					RegisterSell();
					_entryPrice = 0m;
				}
			}
		}
		else if (Position < 0)
		{
			var profitPercent = (_entryPrice - candle.ClosePrice) / _entryPrice * 100m;
			if (!_trailingStopActive && profitPercent >= ProfitThresholdPercent)
			{
				_trailingStopActive = true;
				_lowestSinceTrailing = candle.ClosePrice;
			}

			if (_trailingStopActive)
			{
				_lowestSinceTrailing = Math.Min(_lowestSinceTrailing, candle.ClosePrice);
				var stop = _lowestSinceTrailing * (1 + TrailingStopGapPercent / 100m);
				if (candle.ClosePrice >= stop)
				{
					CancelActiveOrders();
					RegisterBuy();
					_entryPrice = 0m;
					_trailingStopActive = false;
					_lowestSinceTrailing = 0m;
				}
			}
			else
			{
				var stop = _entryPrice * (1 + StopLossPercent / 100m);
				if (candle.ClosePrice >= stop)
				{
					CancelActiveOrders();
					RegisterBuy();
					_entryPrice = 0m;
				}
			}
		}
		else
		{
			_trailingStopActive = false;
			_highestSinceTrailing = 0m;
			_lowestSinceTrailing = 0m;
		}
	}
}