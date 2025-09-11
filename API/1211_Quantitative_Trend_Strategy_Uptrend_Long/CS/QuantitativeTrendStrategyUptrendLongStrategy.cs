
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Quantitative trend strategy that goes long on breakout above last pivot high.
/// </summary>
public class QuantitativeTrendStrategyUptrendLongStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLeft;
	private readonly StrategyParam<int> _pivotRight;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _pivotHigh;
	private decimal? _pivotLow;

	/// <summary>
	/// Number of bars to the left of pivot.
	/// </summary>
	public int PivotLeft
	{
		get => _pivotLeft.Value;
		set => _pivotLeft.Value = value;
	}

	/// <summary>
	/// Number of bars to the right of pivot.
	/// </summary>
	public int PivotRight
	{
		get => _pivotRight.Value;
		set => _pivotRight.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="QuantitativeTrendStrategyUptrendLongStrategy"/>.
	/// </summary>
	public QuantitativeTrendStrategyUptrendLongStrategy()
	{
		_pivotLeft = Param(nameof(PivotLeft), 46)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Left", "Bars to the left", "General");

		_pivotRight = Param(nameof(PivotRight), 32)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Right", "Bars to the right", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.75m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 1;
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

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferCount = 0;
		_pivotHigh = null;
		_pivotLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var size = PivotLeft + PivotRight + 1;
		_highBuffer = new decimal[size];
		_lowBuffer = new decimal[size];
		_bufferCount = 0;

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

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

		for (var i = 0; i < _highBuffer.Length - 1; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}

		_highBuffer[^1] = candle.HighPrice;
		_lowBuffer[^1] = candle.LowPrice;

		if (_bufferCount < _highBuffer.Length)
		{
			_bufferCount++;
		}
		else
		{
			var index = PivotRight;

			var isHigh = true;
			var high = _highBuffer[index];
			for (var i = 0; i < _highBuffer.Length; i++)
			{
				if (i == index)
					continue;
				if (high <= _highBuffer[i])
				{
					isHigh = false;
					break;
				}
			}
			if (isHigh)
				_pivotHigh = high;

			var isLow = true;
			var low = _lowBuffer[index];
			for (var i = 0; i < _lowBuffer.Length; i++)
			{
				if (i == index)
					continue;
				if (low >= _lowBuffer[i])
				{
					isLow = false;
					break;
				}
			}
			if (isLow)
				_pivotLow = low;
		}

		var lastHigh = _pivotHigh;
		var lastLow = _pivotLow;

		var priceForLong = lastHigh.HasValue && candle.ClosePrice > lastHigh.Value;
		var priceForShort = lastLow.HasValue && candle.ClosePrice < lastLow.Value;

		if (priceForLong && Position <= 0)
			BuyMarket();

		if (Position > 0 && priceForShort)
			SellMarket();

		if (Position > 0 && lastHigh.HasValue && lastLow.HasValue && lastHigh < lastLow)
			SellMarket();
	}
}
