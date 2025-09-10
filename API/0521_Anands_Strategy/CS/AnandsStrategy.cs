using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Anand's breakout strategy based on daily trend and 15-minute levels.
/// </summary>
public class AnandsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle1;
	private ICandleMessage _prevCandle2;

	private decimal _prevHighDay;
	private decimal _prevLowDay;
	private decimal _prevCloseDay;
	private bool _hasDaily;

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AnandsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Trading timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, TimeSpan.FromDays(1).TimeFrame())
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevCandle1 = null;
		_prevCandle2 = null;
		_prevHighDay = default;
		_prevLowDay = default;
		_prevCloseDay = default;
		_hasDaily = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var daySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		daySubscription.Bind(ProcessDaily).Start();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_prevHighDay = candle.HighPrice;
		_prevLowDay = candle.LowPrice;
		_prevCloseDay = candle.ClosePrice;
		_hasDaily = true;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			ShiftCandles(candle);
			return;
		}

		if (!_hasDaily || _prevCandle1 == null || _prevCandle2 == null)
		{
			ShiftCandles(candle);
			return;
		}

		var tradeDirection = _prevCloseDay > _prevHighDay ? 1 : _prevCloseDay < _prevLowDay ? -1 : 0;

		var currentClose = _prevCandle1.ClosePrice;
		var prevHigh15m = _prevCandle2.HighPrice;
		var prevLow15m = _prevCandle2.LowPrice;

		if (tradeDirection == 1 && currentClose > prevHigh15m && Position <= 0)
			BuyMarket();
		else if (tradeDirection == -1 && currentClose < prevLow15m && Position >= 0)
			SellMarket();

		ShiftCandles(candle);
	}

	private void ShiftCandles(ICandleMessage candle)
	{
		_prevCandle2 = _prevCandle1;
		_prevCandle1 = candle;
	}
}

