using System;
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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;

	private decimal _prevMacd;
	private bool _hasPrev;
	private decimal _entryPrice;
	private int _barsFromTrade;

	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartingaleWithMacdKdjOpeningConditionsStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m);
		_stopLossPercent = Param(nameof(StopLossPercent), 8m);
		_cooldownBars = Param(nameof(CooldownBars), 30);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_macd = null;
		_prevMacd = 0m;
		_hasPrev = false;
		_entryPrice = 0m;
		_barsFromTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();

		_prevMacd = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_barsFromTrade = CooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_macd, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevMacd = macd;
			_hasPrev = true;
			return;
		}

		var price = candle.ClosePrice;
		_barsFromTrade++;
		var canTrade = _barsFromTrade >= CooldownBars;

		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;

		if (Position == 0 && canTrade)
		{
			if (crossUp)
			{
				BuyMarket();
				_entryPrice = price;
				_barsFromTrade = 0;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = price;
				_barsFromTrade = 0;
			}
		}
		else if (Position > 0)
		{
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			if (price >= tp || price <= sl || (canTrade && crossDown))
			{
				SellMarket();
				_entryPrice = 0;
				_barsFromTrade = 0;
			}
		}
		else if (Position < 0)
		{
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			if (price <= tp || price >= sl || (canTrade && crossUp))
			{
				BuyMarket();
				_entryPrice = 0;
				_barsFromTrade = 0;
			}
		}

		_prevMacd = macd;
	}
}
