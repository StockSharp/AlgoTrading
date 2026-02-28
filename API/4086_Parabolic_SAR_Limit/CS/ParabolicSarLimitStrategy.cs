using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy: enters on SAR flip (price crosses SAR level).
/// Buys when price moves above SAR, sells when price drops below SAR.
/// </summary>
public class ParabolicSarLimitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevEma;
	private decimal _prevClose;
	private decimal _entryPrice;
	private bool _wasBullish;
	private bool _hasPrev;

	public ParabolicSarLimitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 14)
			.SetDisplay("EMA Length", "EMA period acting as dynamic SAR proxy.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR for volatility.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevEma = 0;
		_prevClose = 0;
		_entryPrice = 0;
		_wasBullish = false;
		_hasPrev = false;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevEma = emaVal;
			_prevClose = close;
			_wasBullish = close > emaVal;
			_hasPrev = true;
			return;
		}

		var isBullish = close > emaVal;
		var flip = isBullish != _wasBullish;

		// Exit on SAR flip
		if (Position > 0 && flip && !isBullish)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && flip && isBullish)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Entry on flip
		if (Position == 0 && flip)
		{
			if (isBullish)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevEma = emaVal;
		_prevClose = close;
		_wasBullish = isBullish;
	}
}
