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
/// Labouchere betting system strategy using Stochastic oscillator for signals.
/// Adjusts position sizing based on the Labouchere sequence.
/// </summary>
public class LabouchereEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private decimal? _prevK;
	private decimal? _prevD;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	public LabouchereEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");

		_kPeriod = Param(nameof(KPeriod), 10)
			.SetDisplay("K Period", "Stochastic %K period", "Indicator")
			.SetGreaterThanZero();

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("D Period", "Stochastic %D period", "Indicator")
			.SetGreaterThanZero();

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
			.SetGreaterThanZero();

		_takeProfitPct = Param(nameof(TakeProfitPct), 1.5m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stoch = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stoch, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal || !stochValue.IsFormed)
			return;

		var stoch = (IStochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		var signal = 0;

		if (_prevK.HasValue && _prevD.HasValue)
		{
			if (_prevK <= _prevD && k > d)
				signal = 1;
			else if (_prevK >= _prevD && k < d)
				signal = -1;
		}

		_prevK = k;
		_prevD = d;

		if (signal == 0)
			return;

		if (signal > 0 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (signal < 0 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevK = null;
		_prevD = null;
		_entryPrice = 0m;
	}
}
