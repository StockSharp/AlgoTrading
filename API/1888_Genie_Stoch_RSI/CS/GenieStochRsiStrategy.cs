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
/// Strategy that trades based on RSI and Stochastic oscillator cross signals.
/// </summary>
public class GenieStochRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;
	private decimal _prevK;
	private decimal _prevD;
	private bool _initialized;

	/// <summary>RSI calculation period.</summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	/// <summary>RSI overbought level.</summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	/// <summary>RSI oversold level.</summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	/// <summary>Stochastic overbought level.</summary>
	public decimal StochOverbought { get => _stochOverbought.Value; set => _stochOverbought.Value = value; }
	/// <summary>Stochastic oversold level.</summary>
	public decimal StochOversold { get => _stochOversold.Value; set => _stochOversold.Value = value; }
	/// <summary>Take profit in price points.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Trailing stop in price points.</summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	/// <summary>Candle type to process.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="GenieStochRsiStrategy"/>.
	/// </summary>
	public GenieStochRsiStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "Parameters");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Signals");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stoch Overbought", "Stochastic overbought level", "Signals");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stoch Oversold", "Stochastic oversold level", "Signals");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetDisplay("Trailing Stop", "Trailing stop in price points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevK = default;
		_prevD = default;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_stochastic, ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(TrailingStop, UnitTypes.Absolute),
			isStopTrailing: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process RSI manually
		var rsiResult = _rsi.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!_rsi.IsFormed || !_stochastic.IsFormed)
			return;

		var rsiValue = rsiResult.ToDecimal();

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal kValue || stochTyped.D is not decimal dValue)
			return;

		if (!_initialized)
		{
			_prevK = kValue;
			_prevD = dValue;
			_initialized = true;
			return;
		}

		// Sell when RSI overbought + stochastic K crosses below D in overbought zone
		var sellSignal = rsiValue > RsiOverbought &&
			kValue > StochOverbought &&
			_prevK > _prevD &&
			kValue < dValue;

		// Buy when RSI oversold + stochastic K crosses above D in oversold zone
		var buySignal = rsiValue < RsiOversold &&
			kValue < StochOversold &&
			_prevK < _prevD &&
			kValue > dValue;

		if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
		else if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}

		_prevK = kValue;
		_prevD = dValue;
	}
}
