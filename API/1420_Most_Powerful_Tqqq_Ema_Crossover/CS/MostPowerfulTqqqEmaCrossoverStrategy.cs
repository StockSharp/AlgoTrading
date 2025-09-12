using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MostPowerfulTqqqEmaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _takePrice;
	private decimal _stopPrice;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	public MostPowerfulTqqqEmaCrossoverStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20)
			.SetDisplay("Fast EMA Length", "Period of the fast EMA", "Indicators");

		_slowLength = Param(nameof(SlowLength), 50)
			.SetDisplay("Slow EMA Length", "Period of the slow EMA", "Indicators");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 1.3m)
			.SetDisplay("Take Profit Multiplier", "Multiplier for take profit price", "Protection");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0.95m)
			.SetDisplay("Stop Loss Multiplier", "Multiplier for stop loss price", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
	}

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_takePrice = 0m;
		_stopPrice = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_initialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossedUp = _prevFast <= _prevSlow && fast > slow;

		if (crossedUp && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_takePrice = _entryPrice * TakeProfitMultiplier;
			_stopPrice = _entryPrice * StopLossMultiplier;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position > 0)
		{
			if (candle.HighPrice >= _takePrice || candle.LowPrice <= _stopPrice)
				SellMarket(Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
