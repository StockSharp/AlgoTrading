using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dynamic support and resistance strategy using recent pivot highs and lows.
/// </summary>
public class DynamicSupportAndResistancePivotStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<decimal> _distancePercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _resistanceLevel;
	private decimal? _supportLevel;
	private decimal _prevClose;

	/// <summary>
	/// Pivot size to identify peaks and troughs.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Distance in percent to consider level touched.
	/// </summary>
	public decimal SupportResistanceDistance
	{
		get => _distancePercent.Value;
		set => _distancePercent.Value = value;
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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DynamicSupportAndResistancePivotStrategy"/>.
	/// </summary>
	public DynamicSupportAndResistancePivotStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars on each side for pivot detection", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_distancePercent = Param(nameof(SupportResistanceDistance), 0.4m)
			.SetGreaterThanZero()
			.SetDisplay("Support/Resistance Distance %", "Tolerance around level", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 26m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_resistanceLevel = null;
		_supportLevel = null;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highBuffer = new decimal[PivotLength * 2 + 1];
		_lowBuffer = new decimal[PivotLength * 2 + 1];
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
			var pivotIndex = PivotLength;

			var isPivotHigh = true;
			var pivotHigh = _highBuffer[pivotIndex];
			for (var i = 0; i < _highBuffer.Length; i++)
			{
				if (i == pivotIndex)
					continue;
				if (pivotHigh <= _highBuffer[i])
				{
					isPivotHigh = false;
					break;
				}
			}
			if (isPivotHigh)
				_resistanceLevel = pivotHigh;

			var isPivotLow = true;
			var pivotLow = _lowBuffer[pivotIndex];
			for (var i = 0; i < _lowBuffer.Length; i++)
			{
				if (i == pivotIndex)
					continue;
				if (pivotLow >= _lowBuffer[i])
				{
					isPivotLow = false;
					break;
				}
			}
			if (isPivotLow)
				_supportLevel = pivotLow;
		}

		var support = _supportLevel;
		var resistance = _resistanceLevel;

		var longCond = false;
		if (support is decimal sup && _prevClose != 0m)
		{
			var nearSup = candle.ClosePrice >= sup * (1 - SupportResistanceDistance / 100m) &&
				candle.ClosePrice <= sup * (1 + SupportResistanceDistance / 100m);
			var crossUp = _prevClose < sup && candle.ClosePrice > sup;
			longCond = nearSup && crossUp;
		}

		var shortCond = false;
		if (resistance is decimal res && _prevClose != 0m)
		{
			var nearRes = candle.ClosePrice >= res * (1 - SupportResistanceDistance / 100m) &&
				candle.ClosePrice <= res * (1 + SupportResistanceDistance / 100m);
			var crossDown = _prevClose > res && candle.ClosePrice < res;
			shortCond = nearRes && crossDown;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (longCond && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCond && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
	}
}
