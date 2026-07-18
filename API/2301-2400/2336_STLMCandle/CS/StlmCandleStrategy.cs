using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle color strategy based on open-close comparison.
/// Buys when the close price is above the open price and sells on the opposite condition.
/// </summary>
public class StlmCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private int _barsSinceSignal;
	private int _prevDirection;

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StlmCandleStrategy"/>.
	/// </summary>
	public StlmCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");
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

		_barsSinceSignal = 2;
		_prevDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsSinceSignal = 2;
		_prevDirection = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		_barsSinceSignal++;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevDirection = GetDirection(candle);
			return;
		}

		var direction = GetDirection(candle);

		if (_barsSinceSignal >= 2 && direction == 1 && _prevDirection == 1)
		{
			if (Position <= 0)
			{
				BuyMarket();
				_barsSinceSignal = 0;
			}
		}
		else if (_barsSinceSignal >= 2 && direction == -1 && _prevDirection == -1)
		{
			if (Position >= 0)
			{
				SellMarket();
				_barsSinceSignal = 0;
			}
		}

		_prevDirection = direction;
	}

	private static int GetDirection(ICandleMessage candle)
	{
		if (candle.ClosePrice > candle.OpenPrice)
			return 1;

		if (candle.ClosePrice < candle.OpenPrice)
			return -1;

		return 0;
	}
}
