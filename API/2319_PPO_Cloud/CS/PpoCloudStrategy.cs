using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on PPO main and signal line crossovers.
/// PPO = ((ShortEMA - LongEMA) / LongEMA) * 100
/// Signal = EMA(PPO, signalPeriod)
/// Buy when PPO crosses above signal, sell when below.
/// </summary>
public class PpoCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;

	private decimal _prevPpo;
	private decimal _prevSignal;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

	public PpoCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA length", "PPO");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA length", "PPO");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal EMA length", "PPO");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPpo = 0;
		_prevSignal = 0;
		_hasPrev = false;

		var ppo = new PPO { ShortPeriod = FastPeriod, LongPeriod = SlowPeriod };
		var signalEma = new ExponentialMovingAverage { Length = SignalPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ppo, ProcessCandle).Start();

		void ProcessCandle(ICandleMessage candle, decimal ppoValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!ppo.IsFormed)
				return;

			// Compute signal line as EMA of PPO
			var sigResult = signalEma.Process(ppoValue, candle.CloseTime, true);
			if (!signalEma.IsFormed)
				return;

			var signalValue = sigResult.GetValue<decimal>();

			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_prevPpo = ppoValue;
				_prevSignal = signalValue;
				_hasPrev = true;
				return;
			}

			if (_hasPrev)
			{
				var crossUp = _prevPpo <= _prevSignal && ppoValue > signalValue;
				var crossDown = _prevPpo >= _prevSignal && ppoValue < signalValue;

				if (crossUp && Position <= 0)
					BuyMarket();
				else if (crossDown && Position >= 0)
					SellMarket();
			}

			_prevPpo = ppoValue;
			_prevSignal = signalValue;
			_hasPrev = true;
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ppo);
			DrawOwnTrades(area);
		}
	}
}
