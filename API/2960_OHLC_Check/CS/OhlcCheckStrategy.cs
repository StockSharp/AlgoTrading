using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OHLC check strategy. Trades based on candle structure (bullish/bearish body).
/// </summary>
public class OhlcCheckStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _confirmBars;

	private int _bullCount;
	private int _bearCount;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int ConfirmBars
	{
		get => _confirmBars.Value;
		set => _confirmBars.Value = value;
	}

	public OhlcCheckStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");

		_confirmBars = Param(nameof(ConfirmBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Confirm Bars", "Consecutive candles to confirm direction", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bullCount = 0;
		_bearCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bullCount = 0;
		_bearCount = 0;

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.ClosePrice > candle.OpenPrice)
		{
			_bullCount++;
			_bearCount = 0;
		}
		else if (candle.ClosePrice < candle.OpenPrice)
		{
			_bearCount++;
			_bullCount = 0;
		}

		// Consecutive bullish candles → buy
		if (_bullCount >= ConfirmBars && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_bullCount = 0;
		}
		// Consecutive bearish candles → sell
		else if (_bearCount >= ConfirmBars && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_bearCount = 0;
		}
	}
}
