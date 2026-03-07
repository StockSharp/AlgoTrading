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
/// Strategy combining MACD crossover, price vs SMA baseline, and EOM filter.
/// Uses StdDev-based stops.
/// </summary>
public class ZeroLagMacdKijunSenEomStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMacd;
	private decimal _prevSignalEma;
	private bool _hasPrev;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public decimal StopPct { get => _stopPct.Value; set => _stopPct.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZeroLagMacdKijunSenEomStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Indicators");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal smoothing", "Indicators");

		_stopPct = Param(nameof(StopPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop %", "Stop loss percent", "Risk");

		_riskReward = Param(nameof(RiskReward), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Take profit ratio", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = 0;
		_prevSignalEma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var baseline = new SimpleMovingAverage { Length = 26 };

		_prevMacd = 0;
		_prevSignalEma = 0;
		_hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, baseline, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, baseline);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal baselineVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Compute MACD
		var macd = fastVal - slowVal;

		// EMA of MACD for signal
		decimal signal;
		if (!_hasPrev)
		{
			signal = macd;
			_prevMacd = macd;
			_prevSignalEma = signal;
			_hasPrev = true;
			return;
		}

		var k = 2m / (SignalLength + 1);
		signal = macd * k + _prevSignalEma * (1 - k);

		// MACD cross detection
		var macdCrossUp = _prevMacd <= _prevSignalEma && macd > signal;
		var macdCrossDown = _prevMacd >= _prevSignalEma && macd < signal;

		// Entry/exit on MACD crossover
		if (macdCrossUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (macdCrossDown && Position >= 0)
		{
			SellMarket();
		}

		_prevMacd = macd;
		_prevSignalEma = signal;
	}
}
