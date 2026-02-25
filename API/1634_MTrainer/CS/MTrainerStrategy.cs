using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Training strategy that enters on EMA crossover and manages position with
/// stop loss and take profit based on ATR distance.
/// </summary>
public class MTrainerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MTrainerStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SL/TP", "Indicators");

		_slMultiplier = Param(nameof(SlMultiplier), 2m)
			.SetDisplay("SL Multiplier", "ATR multiplier for stop loss", "Risk");

		_tpMultiplier = Param(nameof(TpMultiplier), 3m)
			.SetDisplay("TP Multiplier", "ATR multiplier for take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopLoss = 0;
		_takeProfit = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check SL/TP for open positions
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (_hasPrev && atrVal > 0)
		{
			var crossUp = _prevFast <= _prevSlow && fast > slow;
			var crossDown = _prevFast >= _prevSlow && fast < slow;

			if (crossUp && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopLoss = _entryPrice - atrVal * SlMultiplier;
				_takeProfit = _entryPrice + atrVal * TpMultiplier;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopLoss = _entryPrice + atrVal * SlMultiplier;
				_takeProfit = _entryPrice - atrVal * TpMultiplier;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_hasPrev = true;
	}
}
