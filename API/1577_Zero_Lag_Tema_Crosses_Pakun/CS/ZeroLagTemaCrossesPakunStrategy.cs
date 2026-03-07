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
/// Strategy based on zero-lag TEMA crossovers with stop and target from recent extremes.
/// </summary>
public class ZeroLagTemaCrossesPakunStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private bool _entryPlaced;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZeroLagTemaCrossesPakunStrategy()
	{
		_lookback = Param(nameof(Lookback), 20).SetDisplay("Lookback", "Lookback period", "Indicators");
		_fastPeriod = Param(nameof(FastPeriod), 20).SetDisplay("Fast Period", "Fast TEMA length", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 50).SetDisplay("Slow Period", "Slow TEMA length", "Indicators");
		_riskReward = Param(nameof(RiskReward), 1.5m).SetDisplay("Risk/Reward", "Take profit ratio", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame()).SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPlaced = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastTema = new TripleExponentialMovingAverage { Length = FastPeriod };
		var slowTema = new TripleExponentialMovingAverage { Length = SlowPeriod };
		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		_prevFast = 0;
		_prevSlow = 0;
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_entryPlaced = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastTema, slowTema, highest, lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal highestVal, decimal lowestVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;
		_prevFast = fast;
		_prevSlow = slow;

		var price = candle.ClosePrice;

		if (!_entryPlaced)
		{
			if (crossUp && Position <= 0)
			{
				BuyMarket();
				_entryPlaced = true;
				_stopPrice = lowestVal;
				_takeProfitPrice = price + (price - _stopPrice) * RiskReward;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket();
				_entryPlaced = true;
				_stopPrice = highestVal;
				_takeProfitPrice = price - (_stopPrice - price) * RiskReward;
			}
		}
		else
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				{
					SellMarket();
					_entryPlaced = false;
				}
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
				{
					BuyMarket();
					_entryPlaced = false;
				}
			}
		}
	}
}
