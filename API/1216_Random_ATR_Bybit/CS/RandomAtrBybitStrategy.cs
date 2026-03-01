using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RandomAtrBybitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slAtrRatio;
	private readonly StrategyParam<decimal> _tpSlRatio;

	private bool _hasPrev;
	private decimal _prevClose;
	private decimal _entryPrice;
	private decimal _stopOffset;
	private decimal _takeOffset;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal SlAtrRatio { get => _slAtrRatio.Value; set => _slAtrRatio.Value = value; }
	public decimal TpSlRatio { get => _tpSlRatio.Value; set => _tpSlRatio.Value = value; }

	public RandomAtrBybitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_atrLength = Param(nameof(AtrLength), 14);
		_slAtrRatio = Param(nameof(SlAtrRatio), 3m);
		_tpSlRatio = Param(nameof(TpSlRatio), 1m);
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_hasPrev = true;
			return;
		}

		// Check SL/TP for existing position
		if (Position > 0)
		{
			if (candle.LowPrice <= _entryPrice - _stopOffset || candle.HighPrice >= _entryPrice + _takeOffset)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _entryPrice + _stopOffset || candle.LowPrice <= _entryPrice - _takeOffset)
				BuyMarket();
		}

		var highestClose = _prevClose > candle.ClosePrice ? _prevClose : candle.ClosePrice;
		var lowestClose = _prevClose < candle.ClosePrice ? _prevClose : candle.ClosePrice;
		var randNumber = Math.Abs(highestClose - lowestClose);

		var openTime = candle.OpenTime;
		var timeSeed = openTime.Year * 10000 + openTime.Month * 100 + openTime.Day;
		var randomSignal = ((randNumber * timeSeed) % 2m) == 1m;

		_stopOffset = SlAtrRatio * atr;
		_takeOffset = _stopOffset * TpSlRatio;

		if (Position == 0)
		{
			if (randomSignal)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
