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

/// <summary>
/// Trend following strategy using zero lag moving average and EMA breakout boxes.
/// </summary>
public class ZeroLagMaTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevZlma;
	private decimal _prevEma;
	private bool _longSetup;
	private bool _shortSetup;
	private decimal _boxTop;
	private decimal _boxBottom;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _entryPlaced;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZeroLagMaTrendFollowingStrategy()
	{
		_length = Param(nameof(Length), 34).SetDisplay("Length", "MA length", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetDisplay("ATR Period", "ATR length", "Indicators");
		_riskReward = Param(nameof(RiskReward), 2m).SetDisplay("Risk/Reward", "Take profit ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevZlma = 0;
		_prevEma = 0;
		_longSetup = false;
		_shortSetup = false;
		_boxTop = 0;
		_boxBottom = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPlaced = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var zlma = new ZeroLagExponentialMovingAverage { Length = Length };
		var ema = new ExponentialMovingAverage { Length = Length };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		_prevZlma = 0;
		_prevEma = 0;
		_longSetup = false;
		_shortSetup = false;
		_boxTop = 0;
		_boxBottom = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPlaced = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(zlma, ema, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal zlmaVal, decimal emaVal, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevZlma == 0)
		{
			_prevZlma = zlmaVal;
			_prevEma = emaVal;
			return;
		}

		var crossUp = _prevZlma <= _prevEma && zlmaVal > emaVal;
		var crossDown = _prevZlma >= _prevEma && zlmaVal < emaVal;
		_prevZlma = zlmaVal;
		_prevEma = emaVal;

		if (crossUp)
		{
			_boxTop = zlmaVal;
			_boxBottom = zlmaVal - atrValue;
			_longSetup = true;
			_shortSetup = false;
		}
		else if (crossDown)
		{
			_boxTop = zlmaVal + atrValue;
			_boxBottom = zlmaVal;
			_shortSetup = true;
			_longSetup = false;
		}

		var price = candle.ClosePrice;

		if (!_entryPlaced)
		{
			if (_longSetup && candle.LowPrice > _boxTop && Position <= 0)
			{
				BuyMarket();
				_entryPlaced = true;
				_stopPrice = _boxBottom;
				_takeProfitPrice = price + (price - _stopPrice) * RiskReward;
				_longSetup = false;
			}
			else if (_shortSetup && candle.HighPrice < _boxBottom && Position >= 0)
			{
				SellMarket();
				_entryPlaced = true;
				_stopPrice = _boxTop;
				_takeProfitPrice = price - (_stopPrice - price) * RiskReward;
				_shortSetup = false;
			}
		}
		else
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket();
					_entryPlaced = false;
				}
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				{
					BuyMarket();
					_entryPlaced = false;
				}
			}
		}
	}
}
