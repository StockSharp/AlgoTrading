using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish divergence strategy using RSI pivots.
/// </summary>
public class BullishDivergenceShortTermLongTradeFinderStrategy : Strategy
{
	private const int PivotRight = 5;
	private const int MinRange = 5;
	private const int MaxRange = 50;

	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _sellWhenRsi;
	private readonly StrategyParam<decimal> _rsiBullConditionMin;
	private readonly StrategyParam<decimal> _rsiBearConditionMin;
	private readonly StrategyParam<decimal> _rsiBearConditionSellMin;
	private readonly StrategyParam<decimal> _rsiHourEntryThreshold;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _pivotLeft;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private RelativeStrengthIndex _rsiHour;

	private decimal? _rsiHourValue;

	private decimal[] _rsiBuffer;
	private decimal[] _lowBuffer;
	private decimal[] _highBuffer;
	private int _bufferCount;

	private decimal? _prevPivotLowRsi;
	private decimal _prevPivotLowPrice;
	private int _barsSincePrevPivotLow;

	private decimal? _prevPivotHighRsi;
	private decimal _prevPivotHighPrice;
	private int _barsSincePrevPivotHigh;

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// RSI value to trigger exit.
	/// </summary>
	public decimal SellWhenRsi
	{
		get => _sellWhenRsi.Value;
		set => _sellWhenRsi.Value = value;
	}

	/// <summary>
	/// Minimum RSI for bullish pivot.
	/// </summary>
	public decimal RsiBullConditionMin
	{
		get => _rsiBullConditionMin.Value;
		set => _rsiBullConditionMin.Value = value;
	}

	/// <summary>
	/// Minimum RSI for storing bearish pivot.
	/// </summary>
	public decimal RsiBearConditionMin
	{
		get => _rsiBearConditionMin.Value;
		set => _rsiBearConditionMin.Value = value;
	}

	/// <summary>
	/// RSI level used to validate bearish divergence.
	/// </summary>
	public decimal RsiBearConditionSellMin
	{
		get => _rsiBearConditionSellMin.Value;
		set => _rsiBearConditionSellMin.Value = value;
	}

	/// <summary>
	/// Hourly RSI threshold to allow entry.
	/// </summary>
	public decimal RsiHourEntryThreshold
	{
		get => _rsiHourEntryThreshold.Value;
		set => _rsiHourEntryThreshold.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Pivot left bars count.
	/// </summary>
	public int PivotLeft
	{
		get => _pivotLeft.Value;
		set => _pivotLeft.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public BullishDivergenceShortTermLongTradeFinderStrategy()
	{
		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2m, 10m, 1m);

		_sellWhenRsi = Param(nameof(SellWhenRsi), 75m)
			.SetDisplay("Exit RSI", "Exit when RSI above", "Exit");

		_rsiBullConditionMin = Param(nameof(RsiBullConditionMin), 40m)
			.SetDisplay("RSI Bull Min", "Minimum RSI for bullish pivot", "Indicators");

		_rsiBearConditionMin = Param(nameof(RsiBearConditionMin), 50m)
			.SetDisplay("RSI Bear Min", "Minimum RSI for bearish pivot", "Indicators");

		_rsiBearConditionSellMin = Param(nameof(RsiBearConditionSellMin), 60m)
			.SetDisplay("RSI Bear Sell Min", "RSI level to validate bearish divergence", "Exit");

		_rsiHourEntryThreshold = Param(nameof(RsiHourEntryThreshold), 40m)
			.SetDisplay("Hourly RSI Entry", "Hourly RSI must be below", "Entry");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_pivotLeft = Param(nameof(PivotLeft), 25)
			.SetDisplay("Pivot Left", "Bars to the left for pivot", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromHours(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsiHourValue = null;
		_rsiBuffer = null;
		_lowBuffer = null;
		_highBuffer = null;
		_bufferCount = 0;

		_prevPivotLowRsi = null;
		_prevPivotLowPrice = 0;
		_barsSincePrevPivotLow = 0;

		_prevPivotHighRsi = null;
		_prevPivotHighPrice = 0;
		_barsSincePrevPivotHigh = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var len = PivotLeft + PivotRight + 1;
		_rsiBuffer = new decimal[len];
		_lowBuffer = new decimal[len];
		_highBuffer = new decimal[len];
		_bufferCount = 0;

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_rsiHour = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		SubscribeCandles(TimeSpan.FromHours(1).TimeFrame())
			.Bind(_rsiHour, ProcessHourCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection(stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessHourCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiHourValue = rsi;
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = 0; i < _rsiBuffer.Length - 1; i++)
		{
			_rsiBuffer[i] = _rsiBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
			_highBuffer[i] = _highBuffer[i + 1];
		}

		_rsiBuffer[^1] = rsi;
		_lowBuffer[^1] = candle.LowPrice;
		_highBuffer[^1] = candle.HighPrice;

		if (_bufferCount < _rsiBuffer.Length)
		{
			_bufferCount++;
			_barsSincePrevPivotLow++;
			_barsSincePrevPivotHigh++;
			return;
		}

		_barsSincePrevPivotLow++;
		_barsSincePrevPivotHigh++;

		var pivotIndex = _rsiBuffer.Length - 1 - PivotRight;
		var pivotRsi = _rsiBuffer[pivotIndex];
		var pivotLowPrice = _lowBuffer[pivotIndex];
		var pivotHighPrice = _highBuffer[pivotIndex];

		var pivotLow = true;
		var pivotHigh = true;

		for (var i = 0; i < _rsiBuffer.Length; i++)
		{
			if (i == pivotIndex)
				continue;

			var value = _rsiBuffer[i];

			if (pivotLow && pivotRsi >= value)
				pivotLow = false;

			if (pivotHigh && pivotRsi <= value)
				pivotHigh = false;
		}

		var bullCond = false;
		var bearCond = false;

		if (pivotLow && pivotRsi < RsiBullConditionMin)
		{
			var rsiHlCheck = _prevPivotLowRsi is decimal prevRsi && pivotRsi > prevRsi &&
				_barsSincePrevPivotLow >= MinRange && _barsSincePrevPivotLow <= MaxRange;

			var priceLlCheck = _prevPivotLowPrice != 0 && candle.LowPrice < _prevPivotLowPrice;

			bullCond = rsiHlCheck && priceLlCheck;

			_prevPivotLowRsi = pivotRsi;
			_prevPivotLowPrice = pivotLowPrice;
			_barsSincePrevPivotLow = 0;
		}

		if (pivotHigh && pivotRsi > RsiBearConditionMin)
		{
			var rsiLhCheck = _prevPivotHighRsi is decimal prevRsi && pivotRsi < prevRsi &&
				_barsSincePrevPivotHigh >= MinRange && _barsSincePrevPivotHigh <= MaxRange;

			var priceHhCheck = _prevPivotHighPrice != 0 && candle.HighPrice > _prevPivotHighPrice;

			var rsi3 = _rsiBuffer[^4];

			bearCond = rsiLhCheck && priceHhCheck && rsi3 > RsiBearConditionSellMin;

			_prevPivotHighRsi = pivotRsi;
			_prevPivotHighPrice = pivotHighPrice;
			_barsSincePrevPivotHigh = 0;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullCond && Position == 0 &&
			_prevPivotLowPrice != 0 &&
			candle.ClosePrice < _prevPivotLowPrice &&
			_rsiHourValue is decimal rsiHour && rsiHour < RsiHourEntryThreshold)
		{
			BuyMarket(Volume);
		}
		else if (Position > 0 && (rsi > SellWhenRsi || bearCond))
		{
			SellMarket(Position);
		}
	}
}
