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
/// Strategy based on Hull Moving Average trend.
/// It turns long when the Hull MA starts rising and short when it starts falling,
/// and protects the position with a stop that trails the close by AtrMultiplier ATRs.
/// </summary>
public class HullMaTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHmaValue;
	// Direction of the last slope change: 1 rising, -1 falling, 0 none yet.
	private int _slope;
	private decimal _stopPrice;

	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR calculation (stop-loss).
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR to determine stop-loss distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Initialize the Hull MA Trend strategy.
	/// </summary>
	public HullMaTrendStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")

			.SetOptimize(5, 15, 2);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for Average True Range (stop-loss)", "Risk parameters")
			
			.SetOptimize(10, 20, 2);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Distance of the trailing stop in ATR multiples", "Risk parameters")
			
			.SetOptimize(1, 3, 0.5m);

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
		_prevHmaValue = null;
		_slope = 0;
		_stopPrice = 0m;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicators
		var hma = new HullMovingAverage { Length = HmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hma, atr, ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}

	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hmaIndicatorValue, IIndicatorValue atrIndicatorValue)
	{
		if (candle.State != CandleStates.Finished || !hmaIndicatorValue.IsFormed || !atrIndicatorValue.IsFormed)
			return;

		var hmaValue = hmaIndicatorValue.GetValue<decimal>();
		var atrValue = atrIndicatorValue.GetValue<decimal>();

		var previous = _prevHmaValue;
		_prevHmaValue = hmaValue;

		if (previous is not decimal prevHma || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var distance = AtrMultiplier * atrValue;
		var slope = hmaValue > prevHma ? 1 : hmaValue < prevHma ? -1 : 0;

		// Only a change of the slope direction is a new signal, so a stopped trade is not reopened at once.
		if (slope != 0 && slope != _slope)
		{
			_slope = slope;

			if (slope > 0 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = close - distance;
				return;
			}

			if (slope < 0 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = close + distance;
				return;
			}
		}

		if (Position > 0)
		{
			if (close <= _stopPrice)
				SellMarket(Position);
			else
				_stopPrice = Math.Max(_stopPrice, close - distance);
		}
		else if (Position < 0)
		{
			if (close >= _stopPrice)
				BuyMarket(-Position);
			else
				_stopPrice = Math.Min(_stopPrice, close + distance);
		}
	}
}
