using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Averaging down strategy.
/// Every candle that closes with RSI below RsiBuyThreshold buys another Volume, averaging the entry price of the long. The whole
/// long closes when a close exceeds the previous candle's high. Long only.
/// </summary>
public class AveragingDown2Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuyThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHigh;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level below which the strategy buys.
	/// </summary>
	public decimal RsiBuyThreshold
	{
		get => _rsiBuyThreshold.Value;
		set => _rsiBuyThreshold.Value = value;
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
	public AveragingDown2Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiBuyThreshold = Param(nameof(RsiBuyThreshold), 33m)
			.SetDisplay("RSI Buy Threshold", "RSI level below which the strategy buys", "Signals");

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
		_prevHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHigh = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevHigh = _prevHigh;
		_prevHigh = candle.HighPrice;

		if (prevHigh is not decimal ph)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && candle.ClosePrice > ph)
			SellMarket(Position);
		else if (rsi < RsiBuyThreshold)
			BuyMarket(Volume);
	}
}
