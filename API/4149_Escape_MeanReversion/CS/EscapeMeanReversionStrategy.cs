using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Escape Mean Reversion: SMA crossover with ATR stops.
/// </summary>
public class EscapeMeanReversionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevClose;
	private decimal _entryPrice;

	public EscapeMeanReversionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_smaLength = Param(nameof(SmaLength), 5)
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

		_prevClose = 0;
		_entryPrice = 0;

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

		var close = candle.ClosePrice;

		if (_prevClose == 0 || atrVal <= 0)
		{
			_prevClose = close;
			return;
		}

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 1.5m || close > smaVal)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 1.5m || close < smaVal)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (close < smaVal && _prevClose >= smaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close > smaVal && _prevClose <= smaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevClose = close;
	}
}
