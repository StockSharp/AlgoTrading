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
/// TrendTwister strategy combining fast/slow MA crossover with RSI filter.
/// Enters on MA crossover confirmed by RSI momentum, exits on TP/SL or reverse signal.
/// </summary>
public class TrendTwisterV15Strategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _profitFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal ProfitFactor { get => _profitFactor.Value; set => _profitFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendTwisterV15Strategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Period for fast MA", "Indicators");

		_slowLength = Param(nameof(SlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Period for slow MA", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI", "Indicators");

		_profitFactor = Param(nameof(ProfitFactor), 1.65m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Multiplier for take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_stopPrice = 0;
		_targetPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastLength };
		var slowMa = new ExponentialMovingAverage { Length = SlowLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var crossAbove = _prevFast <= _prevSlow && fastVal > slowVal;
		var crossBelow = _prevFast >= _prevSlow && fastVal < slowVal;

		var longCondition = crossAbove && candle.ClosePrice > slowVal && rsiVal > 50m;
		var shortCondition = crossBelow && candle.ClosePrice < slowVal && rsiVal < 50m;

		// Check exits first
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice || shortCondition)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice || longCondition)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entries
		if (Position == 0)
		{
			if (longCondition)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = Math.Min(candle.LowPrice, slowVal);
				var slDistance = candle.ClosePrice - _stopPrice;
				if (slDistance > 0)
					_targetPrice = candle.ClosePrice + slDistance * ProfitFactor;
				else
					_targetPrice = candle.ClosePrice * 1.02m;
			}
			else if (shortCondition)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = Math.Max(candle.HighPrice, slowVal);
				var slDistance = _stopPrice - candle.ClosePrice;
				if (slDistance > 0)
					_targetPrice = candle.ClosePrice - slDistance * ProfitFactor;
				else
					_targetPrice = candle.ClosePrice * 0.98m;
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
