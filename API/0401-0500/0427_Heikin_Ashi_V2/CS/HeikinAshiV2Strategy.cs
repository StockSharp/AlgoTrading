using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi V2 strategy.
/// Goes long when the Heikin Ashi candle is bullish and the close is above the EMA, and short when the Heikin Ashi
/// candle is bearish and the close is below the EMA. The opposite signal reverses the position.
/// </summary>
public class HeikinAshiV2Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public HeikinAshiV2Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = null;
		_prevHaClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHaOpen = null;
		_prevHaClose = null;

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _prevHaOpen is decimal prevOpen && _prevHaClose is decimal prevClose
			? (prevOpen + prevClose) / 2m
			: (candle.OpenPrice + candle.ClosePrice) / 2m;

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;

		if (!emaValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = emaValue.ToDecimal();
		var close = candle.ClosePrice;

		if (haClose > haOpen && close > ema && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (haClose < haOpen && close < ema && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
