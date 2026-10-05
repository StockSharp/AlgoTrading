using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Bollinger Bands Signals strategy.
/// Two Bollinger Bands of the same Length use Width1 and Width2 standard deviations. A close crossing above the lower Width2 band
/// goes long and a close crossing below the upper Width2 band goes short, reversing an opposite position. A long closes when the
/// close crosses above the upper Width1 band and a short when it crosses below the lower Width1 band.
/// </summary>
public class DoubleBollingerBandsSignalsStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _width1;
	private readonly StrategyParam<decimal> _width2;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevUpper1;
	private decimal? _prevLower1;
	private decimal? _prevUpper2;
	private decimal? _prevLower2;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviations of the inner (exit) bands.
	/// </summary>
	public decimal Width1
	{
		get => _width1.Value;
		set => _width1.Value = value;
	}

	/// <summary>
	/// Standard deviations of the outer (entry) bands.
	/// </summary>
	public decimal Width2
	{
		get => _width2.Value;
		set => _width2.Value = value;
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
	/// Constructor.
	/// </summary>
	public DoubleBollingerBandsSignalsStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bollinger Bands period", "Indicators");

		_width1 = Param(nameof(Width1), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Width 1", "Standard deviations of the exit bands", "Indicators");

		_width2 = Param(nameof(Width2), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Width 2", "Standard deviations of the entry bands", "Indicators");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevUpper1 = null;
		_prevLower1 = null;
		_prevUpper2 = null;
		_prevLower2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var inner = new BollingerBands { Length = Length, Width = Width1 };
		var outer = new BollingerBands { Length = Length, Width = Width2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(inner, outer, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, inner);
			DrawIndicator(area, outer);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue innerValue, IIndicatorValue outerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!innerValue.IsFormed || !outerValue.IsFormed)
			return;

		var innerBands = (BollingerBandsValue)innerValue;
		var outerBands = (BollingerBandsValue)outerValue;

		if (innerBands.UpBand is not decimal upper1 || innerBands.LowBand is not decimal lower1
			|| outerBands.UpBand is not decimal upper2 || outerBands.LowBand is not decimal lower2)
			return;

		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevUpper1 = _prevUpper1;
		var prevLower1 = _prevLower1;
		var prevUpper2 = _prevUpper2;
		var prevLower2 = _prevLower2;

		_prevClose = close;
		_prevUpper1 = upper1;
		_prevLower1 = lower1;
		_prevUpper2 = upper2;
		_prevLower2 = lower2;

		if (prevClose is not decimal pc || prevUpper1 is not decimal pu1 || prevLower1 is not decimal pl1
			|| prevUpper2 is not decimal pu2 || prevLower2 is not decimal pl2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longEntry = pc <= pl2 && close > lower2;
		var shortEntry = pc >= pu2 && close < upper2;
		var longExit = pc <= pu1 && close > upper1;
		var shortExit = pc >= pl1 && close < lower1;

		if (longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && longExit)
			SellMarket(Position);
		else if (Position < 0 && shortExit)
			BuyMarket(-Position);
	}
}
