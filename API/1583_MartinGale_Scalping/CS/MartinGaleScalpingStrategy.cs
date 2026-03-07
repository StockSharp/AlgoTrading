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
/// MartinGale scalping strategy based on SMA cross with pyramiding entries.
/// </summary>
public class MartinGaleScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxPyramids;

	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevSlow;
	private int _pyramids;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public string TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaxPyramids { get => _maxPyramids.Value; set => _maxPyramids.Value = value; }

	public MartinGaleScalpingStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Length for fast SMA", "General");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Length for slow SMA", "General");

		_takeProfit = Param(nameof(TakeProfit), 1.03m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Mult", "Take profit multiplier", "Risk");

		_stopLoss = Param(nameof(StopLoss), 0.95m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Mult", "Stop loss multiplier", "Risk");

		_tradeDirection = Param(nameof(TradeDirection), "Long")
			.SetDisplay("Trade Direction", "Trade direction", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_maxPyramids = Param(nameof(MaxPyramids), 2)
			.SetGreaterThanZero()
			.SetDisplay("Max Pyramids", "Maximum pyramid entries", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevSlow = 0m;
		_pyramids = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SimpleMovingAverage { Length = FastLength };
		var slowSma = new SimpleMovingAverage { Length = SlowLength };

		_stopPrice = 0m;
		_takePrice = 0m;
		_prevSlow = 0m;
		_pyramids = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastSma, slowSma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var crossover = fast > slow;
		var crossunder = fast < slow;

		if (Position == 0)
		{
			_pyramids = 0;
			if (crossover && AllowLong())
				EnterLong(candle, slow);
			else if (crossunder && AllowShort())
				EnterShort(candle, slow);
		}
		else if (Position > 0)
		{
			if ((candle.ClosePrice > _takePrice || candle.ClosePrice < _stopPrice) && crossunder)
			{
				SellMarket();
				ResetLevels();
			}
			else if (crossover && AllowLong() && _pyramids < MaxPyramids)
			{
				BuyMarket();
				_pyramids++;
				UpdateLevels(candle, slow);
			}
		}
		else if (Position < 0)
		{
			if ((candle.ClosePrice > _takePrice || candle.ClosePrice < _stopPrice) && crossover)
			{
				BuyMarket();
				ResetLevels();
			}
			else if (crossunder && AllowShort() && _pyramids < MaxPyramids)
			{
				SellMarket();
				_pyramids++;
				UpdateLevels(candle, slow);
			}
		}

		_prevSlow = slow;
	}

	private void EnterLong(ICandleMessage candle, decimal slow)
	{
		BuyMarket();
		_pyramids = 1;
		UpdateLevels(candle, slow);
	}

	private void EnterShort(ICandleMessage candle, decimal slow)
	{
		SellMarket();
		_pyramids = 1;
		UpdateLevels(candle, slow);
	}

	private void UpdateLevels(ICandleMessage candle, decimal slow)
	{
		if (_prevSlow == 0m)
			return;

		_stopPrice = Position > 0
			? candle.ClosePrice - StopLoss * _prevSlow
			: candle.ClosePrice + StopLoss * _prevSlow;

		_takePrice = Position > 0
			? candle.ClosePrice + TakeProfit * _prevSlow
			: candle.ClosePrice - TakeProfit * _prevSlow;
	}

	private void ResetLevels()
	{
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	private bool AllowLong() => TradeDirection != "Short";
	private bool AllowShort() => TradeDirection != "Long";
}
