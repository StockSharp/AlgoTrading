namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pin Bar Magic Strategy
/// </summary>
public class PinBarMagicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<decimal> _equityRisk;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _mediumEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _cancelEntryBars;

	private SimpleMovingAverage _slowSma;
	private ExponentialMovingAverage _mediumEma;
	private ExponentialMovingAverage _fastEma;
	private AverageTrueRange _atr;

	private int _barsSinceSignal;
	private bool _pendingLong;
	private bool _pendingShort;
	private decimal _entryPrice;
	private decimal _stopLoss;

	public PinBarMagicStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_equityRisk = Param(nameof(EquityRisk), 3m)
			.SetDisplay("Equity Risk %", "Equity risk percentage", "Risk Management");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetDisplay("ATR Multiplier", "Stop loss ATR multiplier", "Risk Management");

		_slowSmaLength = Param(nameof(SlowSmaLength), 50)
			.SetDisplay("Slow SMA Period", "Slow SMA period", "Indicators");

		_mediumEmaLength = Param(nameof(MediumEmaLength), 18)
			.SetDisplay("Medium EMA Period", "Medium EMA period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 6)
			.SetDisplay("Fast EMA Period", "Fast EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_cancelEntryBars = Param(nameof(CancelEntryBars), 3)
			.SetDisplay("Cancel Entry Bars", "Cancel entry after X bars", "Strategy");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public decimal EquityRisk
	{
		get => _equityRisk.Value;
		set => _equityRisk.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	public int MediumEmaLength
	{
		get => _mediumEmaLength.Value;
		set => _mediumEmaLength.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int CancelEntryBars
	{
		get => _cancelEntryBars.Value;
		set => _cancelEntryBars.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		_mediumEma = new ExponentialMovingAverage { Length = MediumEmaLength };
		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_slowSma, _mediumEma, _fastEma, _atr, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _slowSma);
			DrawIndicator(area, _mediumEma);
			DrawIndicator(area, _fastEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal slowSma, decimal mediumEma, decimal fastEma, decimal atrValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_slowSma.IsFormed || !_mediumEma.IsFormed || !_fastEma.IsFormed || !_atr.IsFormed)
			return;

		// Check pin bar patterns
		var candleRange = candle.HighPrice - candle.LowPrice;
		if (candleRange == 0)
			return;

		var bullishPinBar = false;
		var bearishPinBar = false;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			// Green candle
			var lowerWick = candle.OpenPrice - candle.LowPrice;
			bullishPinBar = lowerWick > 0.66m * candleRange;

			var upperWick = candle.HighPrice - candle.ClosePrice;
			bearishPinBar = upperWick > 0.66m * candleRange;
		}
		else
		{
			// Red candle
			var lowerWick = candle.ClosePrice - candle.LowPrice;
			bullishPinBar = lowerWick > 0.66m * candleRange;

			var upperWick = candle.HighPrice - candle.OpenPrice;
			bearishPinBar = upperWick > 0.66m * candleRange;
		}

		// Trend conditions
		var fanUpTrend = fastEma > mediumEma && mediumEma > slowSma;
		var fanDnTrend = fastEma < mediumEma && mediumEma < slowSma;

		// Piercing conditions
		var bullPierce = (candle.LowPrice < fastEma && candle.OpenPrice > fastEma && candle.ClosePrice > fastEma) ||
						 (candle.LowPrice < mediumEma && candle.OpenPrice > mediumEma && candle.ClosePrice > mediumEma) ||
						 (candle.LowPrice < slowSma && candle.OpenPrice > slowSma && candle.ClosePrice > slowSma);

		var bearPierce = (candle.HighPrice > fastEma && candle.OpenPrice < fastEma && candle.ClosePrice < fastEma) ||
						 (candle.HighPrice > mediumEma && candle.OpenPrice < mediumEma && candle.ClosePrice < mediumEma) ||
						 (candle.HighPrice > slowSma && candle.OpenPrice < slowSma && candle.ClosePrice < slowSma);

		// Entry conditions
		var longEntry = fanUpTrend && bullishPinBar && bullPierce;
		var shortEntry = fanDnTrend && bearishPinBar && bearPierce;

		// Handle pending entries
		if (_pendingLong)
		{
			_barsSinceSignal++;
			if (_barsSinceSignal > CancelEntryBars)
			{
				_pendingLong = false;
				_barsSinceSignal = default;
			}
			else if (candle.HighPrice >= _entryPrice && Position <= 0)
			{
				// Execute long entry
				var risk = EquityRisk * 0.01m * Portfolio.CurrentValue;
				var units = risk / (_entryPrice - _stopLoss);
				BuyMarket(units);
				_pendingLong = false;
				_barsSinceSignal = default;
			}
		}

		if (_pendingShort)
		{
			_barsSinceSignal++;
			if (_barsSinceSignal > CancelEntryBars)
			{
				_pendingShort = false;
				_barsSinceSignal = default;
			}
			else if (candle.LowPrice <= _entryPrice && Position >= 0)
			{
				// Execute short entry
				var risk = EquityRisk * 0.01m * Portfolio.CurrentValue;
				var units = risk / (_stopLoss - _entryPrice);
				SellMarket(units);
				_pendingShort = false;
				_barsSinceSignal = default;
			}
		}

		// Setup new signals
		if (longEntry && !_pendingLong && !_pendingShort && Position == 0)
		{
			_pendingLong = true;
			_entryPrice = candle.HighPrice;
			_stopLoss = candle.LowPrice - atrValue * AtrMultiplier;
			_barsSinceSignal = default;
		}
		else if (shortEntry && !_pendingLong && !_pendingShort && Position == 0)
		{
			_pendingShort = true;
			_entryPrice = candle.LowPrice;
			_stopLoss = candle.HighPrice + atrValue * AtrMultiplier;
			_barsSinceSignal = default;
		}

		// Exit on MA cross
		var prevFastEma = _fastEma.GetValue(1);
		var prevMediumEma = _mediumEma.GetValue(1);

		if (Position > 0 && fastEma < mediumEma && prevFastEma >= prevMediumEma)
		{
			ClosePosition();
		}
		else if (Position < 0 && fastEma > mediumEma && prevFastEma <= prevMediumEma)
		{
			ClosePosition();
		}
	}
}