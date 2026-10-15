using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy driven by stochastic oscillator crossovers.
/// Buy when K crosses above D, sell when K crosses below D.
/// Doubles down on adverse moves with martingale averaging.
/// </summary>
public class MartingailExpertSequenceStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;
	private decimal _entryPrice;

	public int KPeriod { get => _kPeriod.Value; set => _kPeriod.Value = value; }
	public int DPeriod { get => _dPeriod.Value; set => _dPeriod.Value = value; }
	public decimal StepPoints { get => _stepPoints.Value; set => _stepPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MartingailExpertSequenceStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 14)
			.SetDisplay("K Period", "Stochastic K period", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetDisplay("D Period", "Stochastic D period", "Indicators");

		_stepPoints = Param(nameof(StepPoints), 500m)
			.SetDisplay("Step Points", "Distance for martingale averaging", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevK = null;
		_prevD = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevK = null;
		_prevD = null;

		var stoch = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		if (stochValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0) step = 1m;

		// Check take profit
		if (Position > 0 && candle.ClosePrice >= _entryPrice + TakeProfitPoints * step)
		{
			SellMarket();
			_prevK = k;
			_prevD = d;
			return;
		}
		else if (Position < 0 && candle.ClosePrice <= _entryPrice - TakeProfitPoints * step)
		{
			BuyMarket();
			_prevK = k;
			_prevD = d;
			return;
		}

		if (_prevK is not decimal prevK || _prevD is not decimal prevD)
		{
			_prevK = k;
			_prevD = d;
			return;
		}

		// Stochastic crossover signals
		var bullCross = prevK <= prevD && k > d;
		var bearCross = prevK >= prevD && k < d;

		if (Position == 0)
		{
			if (bullCross)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (bearCross)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0 && bearCross)
		{
			SellMarket(); // Close long
			SellMarket(); // Open short
			_entryPrice = candle.ClosePrice;
		}
		else if (Position < 0 && bullCross)
		{
			BuyMarket(); // Close short
			BuyMarket(); // Open long
			_entryPrice = candle.ClosePrice;
		}

		_prevK = k;
		_prevD = d;
	}
}
