using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on price crossing moving averages of highs and lows.
/// Buys when price closes above the high-based moving average and sells when closing below the low-based moving average.
/// Optional take profit and stop loss manage open positions.
/// </summary>
public class HighLowMaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _maHighPeriod;
	private readonly StrategyParam<int> _maLowPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _maHigh;
	private SimpleMovingAverage _maLow;
	private decimal? _prevMaHigh;
	private decimal? _prevMaLow;
	private decimal? _prevClose;
	private decimal _entryPrice;
	private decimal _takePrice;
	private decimal _stopPrice;

	/// <summary>
	/// High moving average period.
	/// </summary>
	public int MaHighPeriod
	{
		get => _maHighPeriod.Value;
		set => _maHighPeriod.Value = value;
	}

	/// <summary>
	/// Low moving average period.
	/// </summary>
	public int MaLowPeriod
	{
		get => _maLowPeriod.Value;
		set => _maLowPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance from entry.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance from entry.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Initializes <see cref="HighLowMaBreakoutStrategy"/>.
	/// </summary>
	public HighLowMaBreakoutStrategy()
	{
		_maHighPeriod = Param(nameof(MaHighPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("High MA Period", "Period of high price MA", "Parameters");

		_maLowPeriod = Param(nameof(MaLowPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Low MA Period", "Period of low price MA", "Parameters");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetDisplay("Take Profit", "Distance from entry for take profit", "Risk");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetDisplay("Stop Loss", "Distance from entry for stop loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevMaHigh = null;
		_prevMaLow = null;
		_prevClose = null;
		_entryPrice = 0m;
		_takePrice = 0m;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_maHigh = new SimpleMovingAverage { Length = MaHighPeriod };
		_maLow = new SimpleMovingAverage { Length = MaLowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(c => c.HighPrice, _maHigh, c => c.LowPrice, _maLow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maHigh);
			DrawIndicator(area, _maLow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maHigh, decimal maLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if ((StopLoss > 0m && close <= _stopPrice) || (TakeProfit > 0m && close >= _takePrice))
				ClosePosition();
		}
		else if (Position < 0)
		{
			if ((StopLoss > 0m && close >= _stopPrice) || (TakeProfit > 0m && close <= _takePrice))
				ClosePosition();
		}
		else
		{
			if (_prevClose is null || _prevMaHigh is null || _prevMaLow is null)
			{
				_prevClose = close;
				_prevMaHigh = maHigh;
				_prevMaLow = maLow;
				return;
			}

			if (_prevClose > _prevMaHigh && close > maHigh)
			{
				BuyMarket();
				_entryPrice = close;
				_takePrice = TakeProfit > 0m ? _entryPrice + TakeProfit : 0m;
				_stopPrice = StopLoss > 0m ? _entryPrice - StopLoss : 0m;
			}
			else if (_prevClose < _prevMaLow && close < maLow)
			{
				SellMarket();
				_entryPrice = close;
				_takePrice = TakeProfit > 0m ? _entryPrice - TakeProfit : 0m;
				_stopPrice = StopLoss > 0m ? _entryPrice + StopLoss : 0m;
			}
		}

		_prevClose = close;
		_prevMaHigh = maHigh;
		_prevMaLow = maLow;
	}
}

