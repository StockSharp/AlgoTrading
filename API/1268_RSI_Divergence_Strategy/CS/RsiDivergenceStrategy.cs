using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI Divergence strategy.
/// </summary>
public class RsiDivergenceStrategy : Strategy
{
	private const int _lookbackLeft = 5;
	private const int _lookbackRight = 5;
	private const int _rangeLower = 5;
	private const int _rangeUpper = 60;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _longEntryRsi;
	private readonly StrategyParam<decimal> _shortEntryRsi;
	private readonly StrategyParam<decimal> _longExitRsi;
	private readonly StrategyParam<decimal> _shortExitRsi;
	private readonly StrategyParam<DataType> _candleType;

	private sealed class BarInfo
	{
		public required ICandleMessage Candle { get; init; }
		public required decimal Rsi { get; init; }
		public required int Index { get; init; }
	}

	private readonly List<BarInfo> _history = [];
	private BarInfo? _lastPivotLow;
	private BarInfo? _lastPivotHigh;
	private int _index;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal LongEntryRsi { get => _longEntryRsi.Value; set => _longEntryRsi.Value = value; }
	public decimal ShortEntryRsi { get => _shortEntryRsi.Value; set => _shortEntryRsi.Value = value; }
	public decimal LongExitRsi { get => _longExitRsi.Value; set => _longExitRsi.Value = value; }
	public decimal ShortExitRsi { get => _shortExitRsi.Value; set => _shortExitRsi.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiDivergenceStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators");

		_longEntryRsi = Param(nameof(LongEntryRsi), 35m)
			.SetDisplay("Long Entry RSI", "RSI level for long entries", "Levels");

		_shortEntryRsi = Param(nameof(ShortEntryRsi), 76m)
			.SetDisplay("Short Entry RSI", "RSI level for short entries", "Levels");

		_longExitRsi = Param(nameof(LongExitRsi), 80m)
			.SetDisplay("Long Exit RSI", "RSI level to exit longs", "Levels");

		_shortExitRsi = Param(nameof(ShortExitRsi), 54.1m)
			.SetDisplay("Short Exit RSI", "RSI level to exit shorts", "Levels");

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

		_history.Clear();
		_lastPivotLow = null;
		_lastPivotHigh = null;
		_index = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_index++;
		var info = new BarInfo { Candle = candle, Rsi = rsiValue, Index = _index };
		_history.Add(info);

		var max = _rangeUpper + _lookbackLeft + _lookbackRight + 5;
		_history.RemoveAll(b => b.Index < _index - max);

		var pivotPos = _history.Count - 1 - _lookbackRight;
		if (pivotPos < _lookbackLeft)
			return;

		var pivot = _history[pivotPos];

		var isPivotLow = true;
		for (var i = 1; i <= _lookbackLeft; i++)
		{
			if (pivot.Rsi >= _history[pivotPos - i].Rsi)
			{
				isPivotLow = false;
				break;
			}
		}
		for (var i = 1; i <= _lookbackRight && isPivotLow; i++)
		{
			if (pivot.Rsi > _history[pivotPos + i].Rsi)
				isPivotLow = false;
		}

		if (isPivotLow)
		{
			if (_lastPivotLow != null)
			{
				var bars = pivot.Index - _lastPivotLow.Index;
				if (bars >= _rangeLower && bars <= _rangeUpper)
				{
					var rsiHigher = pivot.Rsi > _lastPivotLow.Rsi;
					var priceLower = pivot.Candle.LowPrice < _lastPivotLow.Candle.LowPrice;
					if (rsiHigher && priceLower && rsiValue < LongEntryRsi)
						BuyMarket();
				}
			}

			_lastPivotLow = pivot;
		}

		var isPivotHigh = true;
		for (var i = 1; i <= _lookbackLeft; i++)
		{
			if (pivot.Rsi <= _history[pivotPos - i].Rsi)
			{
				isPivotHigh = false;
				break;
			}
		}
		for (var i = 1; i <= _lookbackRight && isPivotHigh; i++)
		{
			if (pivot.Rsi < _history[pivotPos + i].Rsi)
				isPivotHigh = false;
		}

		if (isPivotHigh)
		{
			if (_lastPivotHigh != null)
			{
				var bars = pivot.Index - _lastPivotHigh.Index;
				if (bars >= _rangeLower && bars <= _rangeUpper)
				{
					var rsiLower = pivot.Rsi < _lastPivotHigh.Rsi;
					var priceHigher = pivot.Candle.HighPrice > _lastPivotHigh.Candle.HighPrice;
					if (rsiLower && priceHigher && rsiValue > ShortEntryRsi)
						SellMarket();
				}
			}

			_lastPivotHigh = pivot;
		}

		if (Position > 0 && rsiValue >= LongExitRsi)
			SellMarket();
		else if (Position < 0 && rsiValue <= ShortExitRsi)
			BuyMarket();
	}
}
