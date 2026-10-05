using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mutanabby AI Algo Pro strategy.
/// Long only. Enters on a bullish engulfing candle whose body is at least CandleStabilityIndex of its range, while RSI is below
/// RsiIndex and the close is below the close CandleDeltaLength bars ago. Exits on a bearish engulfing candle or when the stop is hit.
/// The stop is either EntryStopLossPercent below the entry price or StopLossBufferPercent below the lowest low of LookbackPeriod bars.
/// </summary>
public class MutanabbyAiAlgoProStrategy : Strategy
{
	/// <summary>
	/// Defines stop loss calculation modes.
	/// </summary>
	public enum StopLossModes
	{
		/// <summary>
		/// Stop loss is calculated from entry price.
		/// </summary>
		EntryPriceBased,

		/// <summary>
		/// Stop loss is based on the lowest low over a period.
		/// </summary>
		LowestLowBased
	}

	private readonly StrategyParam<decimal> _candleStabilityIndex;
	private readonly StrategyParam<int> _rsiIndex;
	private readonly StrategyParam<int> _candleDeltaLength;
	private readonly StrategyParam<bool> _disableRepeatingSignals;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<StopLossModes> _stopLossMethod;
	private readonly StrategyParam<decimal> _entryStopLossPercent;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossBufferPercent;
	private readonly StrategyParam<DataType> _candleType;

	private const int _rsiLength = 14;

	private decimal? _prevOpen;
	private decimal? _prevClose;
	private readonly Queue<decimal> _closes = new();
	private bool? _lastSignalBuy;
	private decimal _stopLossPrice;

	/// <summary>
	/// Minimum ratio between candle body and true range.
	/// </summary>
	public decimal CandleStabilityIndex
	{
		get => _candleStabilityIndex.Value;
		set => _candleStabilityIndex.Value = value;
	}

	/// <summary>
	/// RSI threshold.
	/// </summary>
	public int RsiIndex
	{
		get => _rsiIndex.Value;
		set => _rsiIndex.Value = value;
	}

	/// <summary>
	/// Bars for price comparison.
	/// </summary>
	public int CandleDeltaLength
	{
		get => _candleDeltaLength.Value;
		set => _candleDeltaLength.Value = value;
	}

	/// <summary>
	/// Prevent consecutive identical signals.
	/// </summary>
	public bool DisableRepeatingSignals
	{
		get => _disableRepeatingSignals.Value;
		set => _disableRepeatingSignals.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss calculation method.
	/// </summary>
	public StopLossModes StopLossMethod
	{
		get => _stopLossMethod.Value;
		set => _stopLossMethod.Value = value;
	}

	/// <summary>
	/// Entry based stop loss percent.
	/// </summary>
	public decimal EntryStopLossPercent
	{
		get => _entryStopLossPercent.Value;
		set => _entryStopLossPercent.Value = value;
	}

	/// <summary>
	/// Lookback period for lowest low stop.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Buffer percent below lowest low.
	/// </summary>
	public decimal StopLossBufferPercent
	{
		get => _stopLossBufferPercent.Value;
		set => _stopLossBufferPercent.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MutanabbyAiAlgoProStrategy"/> class.
	/// </summary>
	public MutanabbyAiAlgoProStrategy()
	{
		_candleStabilityIndex = Param(nameof(CandleStabilityIndex), 0.5m)
			.SetDisplay("Candle Stability Index", "Minimum body/true range ratio", "Technical")
			.SetRange(0m, 1m);

		_rsiIndex = Param(nameof(RsiIndex), 50)
			.SetDisplay("RSI Index", "RSI threshold for entries", "Technical")
			.SetRange(0, 100);

		_candleDeltaLength = Param(nameof(CandleDeltaLength), 5)
			.SetDisplay("Candle Delta Length", "Bars for price comparison", "Technical")
			.SetRange(1, 50);

		_disableRepeatingSignals = Param(nameof(DisableRepeatingSignals), false)
			.SetDisplay("Disable Repeating Signals", "Avoid consecutive identical signals", "Technical");

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Activate stop loss", "Risk Management");

		_stopLossMethod = Param(nameof(StopLossMethod), StopLossModes.EntryPriceBased)
			.SetDisplay("Stop Loss Method", "Entry price or lowest low based", "Risk Management");

		_entryStopLossPercent = Param(nameof(EntryStopLossPercent), 2.0m)
			.SetDisplay("Entry Stop Loss %", "Stop loss percent from entry", "Risk Management")
			.SetGreaterThanZero();

		_lookbackPeriod = Param(nameof(LookbackPeriod), 10)
			.SetDisplay("Lookback Period", "Bars for lowest low stop", "Risk Management")
			.SetGreaterThanZero();

		_stopLossBufferPercent = Param(nameof(StopLossBufferPercent), 0.5m)
			.SetDisplay("Stop Loss Buffer %", "Additional buffer below lowest low", "Risk Management");

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
		_prevOpen = null;
		_prevClose = null;
		_closes.Clear();
		_lastSignalBuy = null;
		_stopLossPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = _rsiLength };
		var lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevOpen = _prevOpen;
		var prevClose = _prevClose;
		var closeN = _closes.Count == CandleDeltaLength ? _closes.Peek() : (decimal?)null;

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_closes.Enqueue(candle.ClosePrice);
		while (_closes.Count > CandleDeltaLength)
			_closes.Dequeue();

		if (prevOpen is not decimal po || prevClose is not decimal pc || closeN is not decimal cn)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (Position > 0)
		{
			var stopHit = EnableStopLoss && _stopLossPrice > 0 && candle.LowPrice <= _stopLossPrice;
			var bearishEngulfing = pc > po && close < open && close < po;

			if (stopHit || bearishEngulfing)
			{
				SellMarket(Position);
				_lastSignalBuy = false;
				_stopLossPrice = 0m;
			}

			return;
		}

		var range = candle.HighPrice - candle.LowPrice;
		var stable = range > 0 && Math.Abs(close - open) / range >= CandleStabilityIndex;
		var bullishEngulfing = pc < po && close > open && close > po;

		if (!bullishEngulfing || !stable || rsiValue >= RsiIndex || close >= cn)
			return;

		if (DisableRepeatingSignals && _lastSignalBuy == true)
			return;

		BuyMarket(Volume + Math.Abs(Position));
		_lastSignalBuy = true;

		_stopLossPrice = !EnableStopLoss
			? 0m
			: StopLossMethod == StopLossModes.EntryPriceBased
				? close * (1 - EntryStopLossPercent / 100m)
				: lowestLow * (1 - StopLossBufferPercent / 100m);
	}
}
