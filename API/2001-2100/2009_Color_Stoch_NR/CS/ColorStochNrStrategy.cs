using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic oscillator with five selectable entry modes and percentage protection.
/// </summary>
public class ColorStochNrStrategy : Strategy
{
	public enum AlgModes
	{
		Breakdown,
		OscTwist,
		SignalTwist,
		OscDisposition,
		SignalBreakdown,
	}

	private readonly StrategyParam<AlgModes> _mode;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;
	private decimal? _prevKDelta;
	private decimal? _prevDDelta;

	public AlgModes Mode { get => _mode.Value; set => _mode.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorStochNrStrategy()
	{
		_mode = Param(nameof(Mode), AlgModes.OscDisposition)
			.SetDisplay("Algorithm Mode", "Trading algorithm to use", "Parameters");
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Length for %K line", "Stochastic")
			.SetOptimize(5, 20, 1);
		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Length for %D line", "Stochastic")
			.SetOptimize(3, 10, 1);
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetOptimize(1m, 5m, 1m);
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetOptimize(1m, 5m, 1m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = _prevD = _prevKDelta = _prevDDelta = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, ProcessCandle).Start();
		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent));
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var stoch = (IStochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;
		if (_prevK is not decimal prevK || _prevD is not decimal prevD)
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		var deltaK = k - prevK;
		var deltaD = d - prevD;
		var buy = false;
		var sell = false;

		switch (Mode)
		{
			case AlgModes.Breakdown:
				buy = prevK <= 50m && k > 50m;
				sell = prevK >= 50m && k < 50m;
				break;
			case AlgModes.OscTwist:
				buy = _prevKDelta is decimal prevKDelta && prevKDelta <= 0m && deltaK > 0m;
				sell = _prevKDelta is decimal prevKDelta2 && prevKDelta2 >= 0m && deltaK < 0m;
				break;
			case AlgModes.SignalTwist:
				buy = _prevDDelta is decimal prevDDelta && prevDDelta <= 0m && deltaD > 0m;
				sell = _prevDDelta is decimal prevDDelta2 && prevDDelta2 >= 0m && deltaD < 0m;
				break;
			case AlgModes.OscDisposition:
				buy = prevK <= prevD && k > d;
				sell = prevK >= prevD && k < d;
				break;
			case AlgModes.SignalBreakdown:
				buy = prevD <= 50m && d > 50m;
				sell = prevD >= 50m && d < 50m;
				break;
			default:
				throw new InvalidOperationException($"Unsupported stochastic signal mode: {Mode}.");
		}

		var position = Position;
		if (buy && position <= 0m)
		{
			if (position < 0m)
				BuyMarket(-position);
			BuyMarket(Volume);
		}
		else if (sell && position >= 0m)
		{
			if (position > 0m)
				SellMarket(position);
			SellMarket(Volume);
		}

		_prevKDelta = deltaK;
		_prevDDelta = deltaD;
		_prevK = k;
		_prevD = d;
	}
}
