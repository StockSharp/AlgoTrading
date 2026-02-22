using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class PercentXTrendFollowerStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _reverseMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOscillator;
	private decimal _stopPrice;
	private decimal _prevHigh;
	private decimal _prevLow;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal ReverseMultiplier { get => _reverseMultiplier.Value; set => _reverseMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PercentXTrendFollowerStrategy()
	{
		_maLength = Param(nameof(MaLength), 40);
		_atrLength = Param(nameof(AtrLength), 14);
		_reverseMultiplier = Param(nameof(ReverseMultiplier), 3m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands { Length = MaLength, Width = 2m };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bb, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal bbMid, decimal atr)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var range = _prevHigh - _prevLow;
		if (range <= 0) range = 1;
		
		var oscillator = (close - bbMid) / range;

		if (_prevOscillator.HasValue)
		{
			var prev = _prevOscillator.Value;
			if (prev < 0.5m && oscillator >= 0.5m && Position <= 0)
			{
				CancelActiveOrders();
				if (Position < 0) BuyMarket(Math.Abs(Position));
				BuyMarket();
				_stopPrice = candle.LowPrice - atr * ReverseMultiplier;
			}
			else if (prev > -0.5m && oscillator <= -0.5m && Position >= 0)
			{
				CancelActiveOrders();
				if (Position > 0) SellMarket(Position);
				SellMarket();
				_stopPrice = candle.HighPrice + atr * ReverseMultiplier;
			}
		}

		_prevOscillator = oscillator;
		_prevHigh = Math.Max(_prevHigh, candle.HighPrice);
		_prevLow = _prevLow == 0 ? candle.LowPrice : Math.Min(_prevLow, candle.LowPrice);

		if (Position > 0 && _stopPrice > 0 && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			_stopPrice = 0;
		}
		else if (Position < 0 && _stopPrice > 0 && candle.HighPrice >= _stopPrice)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = 0;
		}
	}
}
