using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// Strategy trades Bollinger channel rebounds. Enters long after price dips below the lower band and closes above it.
public class Strategy1Strategy : Strategy
{
	private StrategyParam<int> _length;
	private StrategyParam<decimal> _bufferFactor;
	private StrategyParam<DataType> _candleType;

	private bool _wasBelowLower;
	private decimal _prevClose;

	public Strategy1Strategy()
	{
		_length = Param(nameof(Length), 20)
			.SetDisplay("Length", "Period for Bollinger Bands", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_bufferFactor = Param(nameof(BufferFactor), 0.2m)
			.SetDisplay("Buffer Factor", "Stop-loss buffer factor", "General")
			.SetGreaterThanZero()
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal BufferFactor { get => _bufferFactor.Value; set => _bufferFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_wasBelowLower = false;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var bands = new BollingerBands
		{
			Length = Length,
			Width = 1m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bands, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stdev = upper - middle;
		var buffer = stdev * BufferFactor;
		var stopLossPrice = lower - buffer;

		if (candle.ClosePrice < lower)
			_wasBelowLower = true;

		if (Position == 0 && _wasBelowLower && candle.ClosePrice > lower)
		{
			BuyMarket();
			_wasBelowLower = true;
		}

		var longPosition = Position > 0;

		var midPoint = longPosition && _prevClose < middle && candle.ClosePrice >= middle;
		var upperPoint = longPosition && candle.HighPrice > upper && candle.ClosePrice <= upper;
		if (midPoint || upperPoint)
			SellMarket();

		if (longPosition && candle.LowPrice <= stopLossPrice)
			SellMarket();

		_prevClose = candle.ClosePrice;
	}
}
