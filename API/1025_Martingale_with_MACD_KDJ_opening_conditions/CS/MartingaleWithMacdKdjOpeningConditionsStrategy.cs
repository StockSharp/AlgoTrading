using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MartingaleWithMacdKdjOpeningConditionsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;
	private StochasticOscillator _stoch;

	private decimal _prevMacd;
	private decimal _prevK;
	private decimal _prevD;
	private bool _hasPrev;
	private decimal _entryPrice;

	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartingaleWithMacdKdjOpeningConditionsStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m);
		_stopLossPercent = Param(nameof(StopLossPercent), 6m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();
		_stoch = new StochasticOscillator();

		_prevMacd = 0;
		_prevK = 0;
		_prevD = 0;
		_hasPrev = false;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manually process stochastic
		var stochResult = _stoch.Process(candle);

		if (!_macd.IsFormed || !_stoch.IsFormed)
		{
			_prevMacd = macd;
			_hasPrev = true;
			return;
		}

		var stochVal = stochResult as StochasticOscillatorValue;
		decimal k = 50, d = 50;
		if (stochVal != null)
		{
			if (stochVal.K is decimal kv) k = kv;
			if (stochVal.D is decimal dv) d = dv;
		}

		if (!_hasPrev)
		{
			_prevMacd = macd;
			_prevK = k;
			_prevD = d;
			_hasPrev = true;
			return;
		}

		var price = candle.ClosePrice;

		// MACD cross up and K cross above D
		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;

		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				_entryPrice = price;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = price;
			}
		}
		else if (Position > 0)
		{
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			if (price >= tp || price <= sl || crossDown)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			if (price <= tp || price >= sl || crossUp)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		_prevMacd = macd;
		_prevK = k;
		_prevD = d;
	}
}
