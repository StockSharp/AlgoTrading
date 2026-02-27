using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Xbug Free v4 strategy based on moving average crossing median price.
/// </summary>
public class XbugFreeV4Strategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevSma;
	private decimal? _prevPrice;
	private decimal? _prev2Sma;
	private decimal? _prev2Price;
	private decimal _entryPrice;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XbugFreeV4Strategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stops", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSma = null; _prevPrice = null;
		_prev2Sma = null; _prev2Price = null;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(sma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;

		if (_prevSma is decimal prevSma && _prevPrice is decimal prevPrice
			&& _prev2Sma is decimal prev2Sma && _prev2Price is decimal prev2Price)
		{
			var buySignal = smaValue > median && prevSma > prevPrice && prev2Sma < prev2Price;
			var sellSignal = smaValue < median && prevSma < prevPrice && prev2Sma > prev2Price;

			if (buySignal && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (sellSignal && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		// Exit on ATR-based stop/take
		if (atrValue > 0 && _entryPrice > 0)
		{
			if (Position > 0)
			{
				if (candle.ClosePrice <= _entryPrice - atrValue * 2 || candle.ClosePrice >= _entryPrice + atrValue * 3)
				{
					SellMarket();
					_entryPrice = 0;
				}
			}
			else if (Position < 0)
			{
				if (candle.ClosePrice >= _entryPrice + atrValue * 2 || candle.ClosePrice <= _entryPrice - atrValue * 3)
				{
					BuyMarket();
					_entryPrice = 0;
				}
			}
		}

		_prev2Sma = _prevSma;
		_prev2Price = _prevPrice;
		_prevSma = smaValue;
		_prevPrice = median;
	}
}
