using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OctopusNestStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _rrRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	public decimal RrRatio { get => _rrRatio.Value; set => _rrRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OctopusNestStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100).SetGreaterThanZero();
		_bbLength = Param(nameof(BbLength), 20).SetGreaterThanZero();
		_rrRatio = Param(nameof(RrRatio), 1.125m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_longStop = _longTake = _shortStop = _shortTake = 0m;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var bb = new BollingerBands { Length = BbLength, Width = 2m };
		var psar = new ParabolicSar();
		var highest = new Highest { Length = 20 };
		var lowest = new Lowest { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, bb, psar, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue bbValue, IIndicatorValue psarValue, IIndicatorValue highValue, IIndicatorValue lowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFinal || !emaValue.IsFormed || !bbValue.IsFormed || !psarValue.IsFormed || !highValue.IsFormed || !lowValue.IsFormed)
			return;

		var emaVal = emaValue.ToDecimal();
		var psar = psarValue.ToDecimal();
		var highest = highValue.ToDecimal();
		var lowest = lowValue.ToDecimal();

		// Get BB upper/lower from complex value
		decimal bbUpper, bbLower;
		var complexBb = bbValue as IComplexIndicatorValue;
		if (complexBb != null)
		{
			var vals = complexBb.InnerValues.Select(v => v.Value.ToDecimal()).ToArray();
			if (vals.Length >= 3)
			{
				bbUpper = vals[0]; // upper
				bbLower = vals[2]; // lower
			}
			else
				return;
		}
		else
			return;

		// Simplified squeeze: BB width < some threshold relative to price
		var bbWidth = bbUpper - bbLower;
		var squeeze = bbWidth < candle.ClosePrice * 0.01m;

		var longCond = !squeeze && candle.ClosePrice > emaVal && candle.ClosePrice > psar;
		var shortCond = !squeeze && candle.ClosePrice < emaVal && candle.ClosePrice < psar;

		// Exit management
		if (Position > 0)
		{
			if (_longStop > 0 && (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake))
			{
				SellMarket(Math.Abs(Position));
				_longStop = 0;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop > 0 && (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake))
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = 0;
				return;
			}
		}

		// Entry
		if (longCond && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_longStop = lowest * 0.98m;
			_longTake = candle.ClosePrice + (candle.ClosePrice - _longStop) * RrRatio;
		}
		else if (shortCond && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_shortStop = highest * 1.02m;
			_shortTake = candle.ClosePrice - (_shortStop - candle.ClosePrice) * RrRatio;
		}
	}
}
