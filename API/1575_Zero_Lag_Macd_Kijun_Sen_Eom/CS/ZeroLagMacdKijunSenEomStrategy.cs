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

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevMacd;
	private decimal _prevSignalEma;
	private decimal _prevMid;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _entryPrice;
	private bool _hasPrev;

	private readonly List<decimal> _eomValues = new();

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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_prevMacd = 0;
		_prevSignalEma = 0;
		_prevMid = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPrice = 0;
		_hasPrev = false;
		_eomValues.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var baseline = new SimpleMovingAverage { Length = 26 };

		_prevFast = 0;
		_prevSlow = 0;
		_prevMacd = 0;
		_prevSignalEma = 0;
		_prevMid = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPrice = 0;
		_hasPrev = false;
		_eomValues.Clear();

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
			_prevMid = (candle.HighPrice + candle.LowPrice) / 2m;
			_hasPrev = true;
			return;
		}

		var k = 2m / (SignalLength + 1);
		signal = macd * k + _prevSignalEma * (1 - k);

		// Ease of Movement (simplified)
		var mid = (candle.HighPrice + candle.LowPrice) / 2m;
		var range = candle.HighPrice - candle.LowPrice;
		var eomRaw = range > 0 && candle.TotalVolume > 0
			? (mid - _prevMid) * range / (candle.TotalVolume / 10000m)
			: 0;
		_prevMid = mid;

		_eomValues.Add(eomRaw);
		if (_eomValues.Count > 14)
			_eomValues.RemoveAt(0);
		var eom = _eomValues.Count > 0 ? _eomValues.Average() : 0;

		// MACD cross detection
		var macdCrossUp = _prevMacd <= _prevSignalEma && macd > signal;
		var macdCrossDown = _prevMacd >= _prevSignalEma && macd < signal;

		var price = candle.ClosePrice;

		// Exit management
		if (Position > 0 && _entryPrice > 0)
		{
			if (price <= _stopPrice || price >= _takeProfitPrice)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (price >= _stopPrice || price <= _takeProfitPrice)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry
		if (_entryPrice == 0)
		{
			if (macdCrossUp && signal < 0 && price > baselineVal && eom > 0 && Position <= 0)
			{
				BuyMarket();
				_entryPrice = price;
				var sl = price * StopPct / 100m;
				_stopPrice = price - sl;
				_takeProfitPrice = price + sl * RiskReward;
			}
			else if (macdCrossDown && signal > 0 && price < baselineVal && eom < 0 && Position >= 0)
			{
				SellMarket();
				_entryPrice = price;
				var sl = price * StopPct / 100m;
				_stopPrice = price + sl;
				_takeProfitPrice = price - sl * RiskReward;
			}
		}

		_prevMacd = macd;
		_prevSignalEma = signal;
	}
}
