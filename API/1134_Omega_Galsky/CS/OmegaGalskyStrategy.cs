using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OmegaGalskyStrategy : Strategy
{
	private readonly StrategyParam<int> _ema8Period;
	private readonly StrategyParam<int> _ema21Period;
	private readonly StrategyParam<int> _ema89Period;
	private readonly StrategyParam<decimal> _slPercentage;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private bool _wasEma8BelowEma21;
	private bool _isInitialized;

	public int Ema8Period { get => _ema8Period.Value; set => _ema8Period.Value = value; }
	public int Ema21Period { get => _ema21Period.Value; set => _ema21Period.Value = value; }
	public int Ema89Period { get => _ema89Period.Value; set => _ema89Period.Value = value; }
	public decimal SlPercentage { get => _slPercentage.Value; set => _slPercentage.Value = value; }
	public decimal TpPercentage { get => _tpPercentage.Value; set => _tpPercentage.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OmegaGalskyStrategy()
	{
		_ema8Period = Param(nameof(Ema8Period), 8).SetGreaterThanZero();
		_ema21Period = Param(nameof(Ema21Period), 21).SetGreaterThanZero();
		_ema89Period = Param(nameof(Ema89Period), 89).SetGreaterThanZero();
		_slPercentage = Param(nameof(SlPercentage), 0.01m).SetGreaterThanZero();
		_tpPercentage = Param(nameof(TpPercentage), 0.025m).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_stopLoss = 0;
		_takeProfit = 0;
		_wasEma8BelowEma21 = false;
		_isInitialized = false;

		var ema8 = new ExponentialMovingAverage { Length = Ema8Period };
		var ema21 = new ExponentialMovingAverage { Length = Ema21Period };
		var ema89 = new ExponentialMovingAverage { Length = Ema89Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema8, ema21, ema89, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema21);
			DrawIndicator(area, ema89);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema8Value, decimal ema21Value, decimal ema89Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_wasEma8BelowEma21 = ema8Value < ema21Value;
			_isInitialized = true;
			return;
		}

		var isEma8BelowEma21 = ema8Value < ema21Value;

		// Exit first
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		// Entry
		if (_wasEma8BelowEma21 && !isEma8BelowEma21 && candle.ClosePrice > ema89Value && candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			if (Position < 0) BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 - SlPercentage);
			_takeProfit = _entryPrice * (1 + TpPercentage);
		}
		else if (!_wasEma8BelowEma21 && isEma8BelowEma21 && candle.ClosePrice < ema89Value && candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			if (Position > 0) SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_stopLoss = _entryPrice * (1 + SlPercentage);
			_takeProfit = _entryPrice * (1 - TpPercentage);
		}

		_wasEma8BelowEma21 = isEma8BelowEma21;
	}
}
