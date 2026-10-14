namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Uses a causal ATR rail selected by RSI or MFI and confirmed crosses of its two-bar lag.
/// Opposite signals fully reverse or close a position when that entry direction is disabled.
/// </summary>
public class PresentTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _isUseMfi;
	private readonly StrategyParam<bool> _isLongEnabled;
	private readonly StrategyParam<bool> _isShortEnabled;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _previousLines = new();
	private decimal? _line;
	private int _barIndex;
	private int? _lastLongSignal;
	private int? _lastShortSignal;
	private Order _pendingOrder;

	/// <summary>Initializes period14, ATR multiplier1, RSI and both entry directions on five-minute candles.</summary>
	public PresentTrendStrategy()
	{
		_length = Param(nameof(Length), 14).SetGreaterThanZero()
			.SetDisplay("Length", "Shared Wilder ATR and selected RSI/MFI period", "Indicators");
		_multiplier = Param(nameof(Multiplier), 1m).SetGreaterThanZero()
			.SetDisplay("Multiplier", "ATR distance of the causal low/high rail", "Indicators");
		_isUseMfi = Param(nameof(IsUseMfi), false)
			.SetDisplay("Use MFI", "Use volume-weighted typical-price MFI instead of close RSI", "Indicators");
		_isLongEnabled = Param(nameof(IsLongEnabled), true)
			.SetDisplay("Enable Long", "Allow long entries; short positions still close on an opposite signal", "Trading");
		_isShortEnabled = Param(nameof(IsShortEnabled), true)
			.SetDisplay("Enable Short", "Allow short entries; long positions still close on an opposite signal", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying the rail and oscillator", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Positive shared ATR and RSI/MFI period.</summary>
	public int Length { get => _length.Value; set => _length.Value = value; }

	/// <summary>Positive ATR multiplier for the rail.</summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>Whether to use native typical-price/volume MFI; otherwise native close RSI.</summary>
	public bool IsUseMfi { get => _isUseMfi.Value; set => _isUseMfi.Value = value; }

	/// <summary>Whether long entries are allowed; opposite signals can still close shorts.</summary>
	public bool IsLongEnabled { get => _isLongEnabled.Value; set => _isLongEnabled.Value = value; }

	/// <summary>Whether short entries are allowed; opposite signals can still close longs.</summary>
	public bool IsShortEnabled { get => _isShortEnabled.Value; set => _isShortEnabled.Value = value; }

	/// <summary>Finished candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private void ResetState()
	{
		_previousLines.Clear();
		_line = null;
		_barIndex = 0;
		_lastLongSignal = _lastShortSignal = null;
		_pendingOrder = null;
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
		if (Length <= 0 || Multiplier <= 0m || !IsLongEnabled && !IsShortEnabled)
			throw new ArgumentException("Period and multiplier must be positive and at least one entry direction enabled.");
		ResetState();
		var atr = new AverageTrueRange { Length = Length };
		IIndicator oscillator = IsUseMfi ? new MoneyFlowIndex { Length = Length } : new RelativeStrengthIndex { Length = Length };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx([atr, oscillator], ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || values.Any(value => value.IsEmpty || !value.IsFormed))
			return;
		var bullish = values[1].ToDecimal() >= 50m;
		var candidate = bullish ? candle.LowPrice - Multiplier * values[0].ToDecimal() : candle.HighPrice + Multiplier * values[0].ToDecimal();
		var line = _line is decimal previous ? bullish ? Math.Max(previous, candidate) : Math.Min(previous, candidate) : candidate;
		var history = _previousLines.ToArray();
		Sides? signal = null;
		if (history.Length == 3)
		{
			if (history[2] <= history[0] && line > history[1])
			{
				if (_lastShortSignal is int lastShort && (_lastLongSignal is null || lastShort > _lastLongSignal))
					signal = Sides.Buy;
				_lastLongSignal = _barIndex;
			}
			else if (history[2] >= history[0] && line < history[1])
			{
				if (_lastLongSignal is int lastLong && (_lastShortSignal is null || lastLong > _lastShortSignal))
					signal = Sides.Sell;
				_lastShortSignal = _barIndex;
			}
		}
		_line = line;
		_previousLines.Enqueue(line);
		if (_previousLines.Count > 3)
			_previousLines.Dequeue();
		_barIndex++;
		if (signal is not Sides side || !IsFormedAndOnlineAndAllowTrading()
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)
			|| side == Sides.Buy && Position > 0m || side == Sides.Sell && Position < 0m)
			return;
		var enabled = side == Sides.Buy ? IsLongEnabled : IsShortEnabled;
		if (!enabled && Position == 0m)
			return;
		RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = side,
			Volume = Math.Abs(Position) + (enabled ? Volume : 0m),
			Comment = enabled ? "PresentTrend confirmed cross" : "PresentTrend opposite close",
		});
	}
}
