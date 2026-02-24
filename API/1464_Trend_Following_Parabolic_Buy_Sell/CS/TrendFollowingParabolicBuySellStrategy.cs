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
/// Trend following with EMA crossovers and risk/reward exits.
/// </summary>
public class TrendFollowingParabolicBuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _stopLevel;
	private decimal _takeProfit;

	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendFollowingParabolicBuySellStrategy()
	{
		_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Trendline period", "Moving Averages");

		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "Moving Averages");

		_slowLength = Param(nameof(SlowLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "Moving Averages");

		_riskReward = Param(nameof(RiskReward), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Take profit to stop ratio", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for strategy", "General");
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
		_entryPrice = 0;
		_stopLevel = 0;
		_takeProfit = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var trendMa = new SimpleMovingAverage { Length = TrendLength };
		var fastMa = new ExponentialMovingAverage { Length = FastLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(trendMa, fastMa, slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, trendMa);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendVal, decimal fastVal, decimal slowVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check TP/SL exits first
		if (Position > 0 && _stopLevel > 0)
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _takeProfit)
			{
				SellMarket();
				_stopLevel = 0;
				_takeProfit = 0;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}
		else if (Position < 0 && _stopLevel > 0)
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _takeProfit)
			{
				BuyMarket();
				_stopLevel = 0;
				_takeProfit = 0;
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}
		}

		if (_prevFast == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var close = candle.ClosePrice;

		// Long entry: above trend, fast crosses above slow
		if (close > trendVal && _prevFast <= _prevSlow && fastVal > slowVal && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_stopLevel = trendVal;
			var risk = close - trendVal;
			_takeProfit = close + risk * RiskReward;
		}
		// Short entry: below trend, fast crosses below slow
		else if (close < trendVal && _prevFast >= _prevSlow && fastVal < slowVal && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_stopLevel = trendVal;
			var risk = trendVal - close;
			_takeProfit = close - risk * RiskReward;
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
