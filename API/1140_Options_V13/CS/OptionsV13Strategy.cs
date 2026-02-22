using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OptionsV13Strategy : Strategy
{
	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpSlRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEmaShort;
	private decimal _prevEmaLong;
	private bool _readyLong;
	private bool _readyShort;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public int EmaShortLength { get => _emaShortLength.Value; set => _emaShortLength.Value = value; }
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public decimal TpSlRatio { get => _tpSlRatio.Value; set => _tpSlRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OptionsV13Strategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 9).SetGreaterThanZero();
		_emaLongLength = Param(nameof(EmaLongLength), 21).SetGreaterThanZero();
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero();
		_slMultiplier = Param(nameof(SlMultiplier), 1.5m).SetGreaterThanZero();
		_tpSlRatio = Param(nameof(TpSlRatio), 2.0m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevEmaShort = 0;
		_prevEmaLong = 0;
		_readyLong = false;
		_readyShort = false;
		_stopPrice = 0;
		_targetPrice = 0;

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevEmaShort == 0)
		{
			_prevEmaShort = emaShort;
			_prevEmaLong = emaLong;
			return;
		}

		// Exit management
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket(Math.Abs(Position));
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
			}
		}

		var crossOver = _prevEmaShort <= _prevEmaLong && emaShort > emaLong;
		var crossUnder = _prevEmaShort >= _prevEmaLong && emaShort < emaLong;

		if (crossOver) { _readyLong = true; _readyShort = false; }
		else if (crossUnder) { _readyShort = true; _readyLong = false; }

		if (Position == 0 && atrValue > 0)
		{
			if (_readyLong && rsiValue >= 50)
			{
				BuyMarket(Volume);
				_stopPrice = candle.ClosePrice - atrValue * SlMultiplier;
				_targetPrice = candle.ClosePrice + atrValue * SlMultiplier * TpSlRatio;
				_readyLong = false;
			}
			else if (_readyShort && rsiValue <= 50)
			{
				SellMarket(Volume);
				_stopPrice = candle.ClosePrice + atrValue * SlMultiplier;
				_targetPrice = candle.ClosePrice - atrValue * SlMultiplier * TpSlRatio;
				_readyShort = false;
			}
		}

		_prevEmaShort = emaShort;
		_prevEmaLong = emaLong;
	}
}
