using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD Sample 1010: SMA band mean reversion with ATR stops.
/// </summary>
public class MacdSample1010Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _bandMult;

	private decimal _entryPrice;

	public MacdSample1010Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_smaLength = Param(nameof(SmaLength), 20)
			.SetDisplay("SMA Length", "Band center period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_bandMult = Param(nameof(BandMult), 2.0m)
			.SetDisplay("Band Multiplier", "ATR multiplier for bands.", "Indicators");
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

	public decimal BandMult
	{
		get => _bandMult.Value;
		set => _bandMult.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;
		var upper = smaVal + atrVal * BandMult;
		var lower = smaVal - atrVal * BandMult;

		if (Position > 0)
		{
			if (close >= smaVal || close <= _entryPrice - atrVal * 2m)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= smaVal || close >= _entryPrice + atrVal * 2m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (close < lower)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (close > upper)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
