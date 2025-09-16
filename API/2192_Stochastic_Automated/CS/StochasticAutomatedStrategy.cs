using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic oscillator based strategy with trailing stop and fixed risk
/// parameters.
/// </summary>
public class StochasticAutomatedStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _overBought;
	private readonly StrategyParam<decimal> _overSold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;
	private decimal _prevK1;
	private decimal _prevK2;
	private decimal _prevD1;
	private decimal _prevD2;
	private decimal _entryPrice;
	private decimal _highest;
	private decimal _lowest;
	private decimal _tickSize;

	/// <summary>
	/// Stochastic %K period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic slowing value.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal OverBought
	{
		get => _overBought.Value;
		set => _overBought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal OverSold
	{
		get => _overSold.Value;
		set => _overSold.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Candle type used for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StochasticAutomatedStrategy"/>.
	/// </summary>
	public StochasticAutomatedStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
					   .SetGreaterThanZero()
					   .SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
					   .SetCanOptimize(true);

		_dPeriod = Param(nameof(DPeriod), 3)
					   .SetGreaterThanZero()
					   .SetDisplay("%D Period", "Stochastic %D period", "Stochastic")
					   .SetCanOptimize(true);

		_slowing = Param(nameof(Slowing), 3)
					   .SetGreaterThanZero()
					   .SetDisplay("Slowing", "Stochastic slowing", "Stochastic")
					   .SetCanOptimize(true);

		_overBought = Param(nameof(OverBought), 80m)
						  .SetDisplay("Overbought", "Overbought threshold", "Stochastic")
						  .SetCanOptimize(true);

		_overSold = Param(nameof(OverSold), 20m)
						.SetDisplay("Oversold", "Oversold threshold", "Stochastic")
						.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 150m)
						  .SetNotNegative()
						  .SetDisplay("Take Profit", "Profit target in price", "Risk")
						  .SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 100m)
						.SetNotNegative()
						.SetDisplay("Stop Loss", "Stop loss in price", "Risk")
						.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 30m)
							.SetNotNegative()
							.SetDisplay("Trailing Stop", "Trailing stop in price", "Risk")
							.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Time frame", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stochastic = null;
		_prevK1 = _prevK2 = _prevD1 = _prevD2 = 0m;
		_entryPrice = _highest = _lowest = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;

		_stochastic = new StochasticOscillator {
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription.BindEx(_stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			var indArea = CreateChartArea();
			if (indArea != null)
				DrawIndicator(indArea, _stochastic);

			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfit * _tickSize, UnitTypes.Price),
						new Unit(StopLoss * _tickSize, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (Position > 0)
		{
			_highest = Math.Max(_highest, candle.HighPrice);
			if (TrailingStop > 0 && candle.LowPrice <= _highest - TrailingStop * _tickSize)
				SellMarket(Math.Abs(Position));
			else if (k > OverBought || d < _prevD1)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			_lowest = Math.Min(_lowest, candle.LowPrice);
			if (TrailingStop > 0 && candle.HighPrice >= _lowest + TrailingStop * _tickSize)
				BuyMarket(Math.Abs(Position));
			else if (k < OverSold || d > _prevD1)
				BuyMarket(Math.Abs(Position));
		}
		else
		{
			if (_prevD2 < OverSold && _prevK2 < OverSold && _prevD2 > _prevK2 && _prevD1 < _prevK1 && _prevD1 < d)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_highest = candle.ClosePrice;
				_lowest = candle.ClosePrice;
			}
			else if (_prevD2 > OverBought && _prevK2 > OverBought && _prevD2 < _prevK2 && _prevD1 > _prevK1 &&
					 _prevD1 > d)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_highest = candle.ClosePrice;
				_lowest = candle.ClosePrice;
			}
		}

		_prevK2 = _prevK1;
		_prevD2 = _prevD1;
		_prevK1 = k;
		_prevD1 = d;
	}
}
