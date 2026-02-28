using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rubber Bands Grid: Mean reversion grid using SMA+ATR bands.
/// </summary>
public class RubberBandsGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private int _gridCount;

	public RubberBandsGridStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_smaLength = Param(nameof(SmaLength), 20)
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
		_gridCount = 0;

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
		var lower = smaVal - atrVal * 2m;
		var upper = smaVal + atrVal * 2m;

		if (Position > 0)
		{
			if (close >= smaVal)
			{
				SellMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (close <= _entryPrice - atrVal * 4m)
			{
				SellMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (_gridCount < 3 && close <= _entryPrice - atrVal)
			{
				_entryPrice = (_entryPrice + close) / 2m;
				_gridCount++;
				BuyMarket();
			}
		}
		else if (Position < 0)
		{
			if (close <= smaVal)
			{
				BuyMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (close >= _entryPrice + atrVal * 4m)
			{
				BuyMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (_gridCount < 3 && close >= _entryPrice + atrVal)
			{
				_entryPrice = (_entryPrice + close) / 2m;
				_gridCount++;
				SellMarket();
			}
		}

		if (Position == 0)
		{
			if (close <= lower)
			{
				_entryPrice = close;
				_gridCount = 0;
				BuyMarket();
			}
			else if (close >= upper)
			{
				_entryPrice = close;
				_gridCount = 0;
				SellMarket();
			}
		}
	}
}
