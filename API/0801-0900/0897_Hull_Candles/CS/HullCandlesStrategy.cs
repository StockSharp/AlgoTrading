using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Candles strategy.
/// A Hull moving average of BodyLength is built on the OHLC4 price and smoothed by an SMA of SmaLength. When the HMA rises and the
/// close is above that SMA the strategy goes long; when the HMA falls and the close is below it the strategy goes short, reversing
/// an opposite position.
/// </summary>
public class HullCandlesStrategy : Strategy
{
	private readonly StrategyParam<int> _bodyLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private SimpleMovingAverage _sma;
	private decimal? _prevHma;

	/// <summary>
	/// Hull moving average length.
	/// </summary>
	public int BodyLength
	{
		get => _bodyLength.Value;
		set => _bodyLength.Value = value;
	}

	/// <summary>
	/// Length of the SMA that smooths the HMA.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
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
	public HullCandlesStrategy()
	{
		_bodyLength = Param(nameof(BodyLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Body Length", "Hull moving average length", "Indicators");

		_smaLength = Param(nameof(SmaLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length of the SMA that smooths the HMA", "Indicators");

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
		_prevHma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHma = null;
		_hma = new HullMovingAverage { Length = BodyLength };
		_sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var ohlc4 = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		var hmaValue = _hma.Process(new DecimalIndicatorValue(_hma, ohlc4, candle.OpenTime) { IsFinal = true });
		if (!_hma.IsFormed)
			return;

		var hma = hmaValue.GetValue<decimal>();
		var smaValue = _sma.Process(new DecimalIndicatorValue(_sma, hma, candle.OpenTime) { IsFinal = true });

		var prevHma = _prevHma;
		_prevHma = hma;

		if (!_sma.IsFormed || prevHma is not decimal previous)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		if (hma > previous && close > sma && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (hma < previous && close < sma && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
