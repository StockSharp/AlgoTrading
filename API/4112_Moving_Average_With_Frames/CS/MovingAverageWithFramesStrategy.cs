using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving Average With Frames: SMA crossover with candle body confirmation.
/// Buys when close crosses above shifted SMA, sells when crosses below.
/// </summary>
public class MovingAverageWithFramesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private decimal _prevClose;

	public MovingAverageWithFramesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_smaLength = Param(nameof(SmaLength), 12)
			.SetDisplay("SMA Length", "SMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevClose = 0;

		var sma = new SimpleMovingAverage { Length = SmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;

		if (_prevClose == 0)
		{
			_prevClose = close;
			return;
		}

		if (Position > 0)
		{
			if (close < smaVal && _prevClose >= smaVal)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (close <= _entryPrice - atrVal * 2m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close > smaVal && _prevClose <= smaVal)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			else if (close >= _entryPrice + atrVal * 2m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (close > smaVal && _prevClose <= smaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close < smaVal && _prevClose >= smaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevClose = close;
	}
}
