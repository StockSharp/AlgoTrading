using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Flip: EMA trend following with ATR stops.
/// </summary>
public class ParabolicSarFlipAlertStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevClose;
	private decimal _entryPrice;

	public ParabolicSarFlipAlertStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");
		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevClose = 0; _entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = 0; _entryPrice = 0;
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, atr, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null) { DrawCandles(area, subscription); DrawIndicator(area, ema); DrawOwnTrades(area); }
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;
		var close = candle.ClosePrice;
		if (_prevClose == 0 || atrVal <= 0) { _prevClose = close; return; }

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 2.5m || close <= _entryPrice - atrVal * 1.5m || close < emaVal) { SellMarket(); _entryPrice = 0; }
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 2.5m || close >= _entryPrice + atrVal * 1.5m || close > emaVal) { BuyMarket(); _entryPrice = 0; }
		}

		if (Position == 0)
		{
			if (close > emaVal && _prevClose <= emaVal) { _entryPrice = close; BuyMarket(); }
			else if (close < emaVal && _prevClose >= emaVal) { _entryPrice = close; SellMarket(); }
		}

		_prevClose = close;
	}
}
