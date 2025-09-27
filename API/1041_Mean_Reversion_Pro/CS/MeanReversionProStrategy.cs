using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanReversionProStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<TradingMode> _tradingMode;
	private readonly StrategyParam<DataType> _candleType;

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

	public TradingMode Mode
	{
		get => _tradingMode.Value;
		set => _tradingMode.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MeanReversionProStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length of the fast moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of the slow moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(100, 200, 50);

		_tradingMode = Param(nameof(Mode), TradingMode.LongOnly)
			.SetDisplay("Trading Direction", "Allowed trading direction", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastSma = new SMA { Length = FastLength };
		var slowSma = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		StartProtection();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var range = candle.HighPrice - candle.LowPrice;
		var longThreshold = candle.LowPrice + 0.2m * range;
		var shortThreshold = candle.HighPrice - 0.2m * range;

		var longAllowed = Mode is TradingMode.LongOnly or TradingMode.Both;
		var shortAllowed = Mode is TradingMode.ShortOnly or TradingMode.Both;

		var longSignal = candle.ClosePrice < fast &&
			candle.ClosePrice < longThreshold &&
			candle.ClosePrice > slow &&
			Position == 0 &&
			longAllowed;

		var shortSignal = candle.ClosePrice > fast &&
			candle.ClosePrice > shortThreshold &&
			candle.ClosePrice < slow &&
			Position == 0 &&
			shortAllowed;

		var exitLong = candle.ClosePrice > fast && Position > 0;
		var exitShort = candle.ClosePrice < fast && Position < 0;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (exitLong && Position > 0)
		{
			ClosePosition();
		}
		else if (exitShort && Position < 0)
		{
			ClosePosition();
		}
	}

	public enum TradingMode
	{
		LongOnly,
		ShortOnly,
		Both,
	}
}
