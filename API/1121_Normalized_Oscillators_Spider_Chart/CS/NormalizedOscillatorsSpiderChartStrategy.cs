using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy calculating average of several normalized oscillators.
/// Goes long when the average exceeds 0.6 and shorts when below 0.4.
/// </summary>
public class NormalizedOscillatorsSpiderChartStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stochastic;
	private MoneyFlowIndex _mfi;
	private WilliamsR _wpr;
	private ChandeMomentumOscillator _cmo;
	private AroonOscillator _aos;

	private decimal[] _prices;
	private decimal[] _upFlags;
	private int _index;
	private decimal _lastClose;

	/// <summary>
	/// Lookback period for all oscillators.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set
		{
			_length.Value = value;
			_prices = new decimal[value];
			_upFlags = new decimal[value];
		}
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NormalizedOscillatorsSpiderChartStrategy()
	{
		_length = Param(nameof(Length), 14).SetDisplay("Length").SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle type");

		_prices = new decimal[Length];
		_upFlags = new decimal[Length];
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = Length };
		_stochastic = new StochasticOscillator { Length = Length };
		_mfi = new MoneyFlowIndex { Length = Length };
		_wpr = new WilliamsR { Length = Length };
		_cmo = new ChandeMomentumOscillator { Length = Length };
		_aos = new AroonOscillator { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_stochastic, _rsi, _mfi, _wpr, _cmo, _aos, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue rsiValue, IIndicatorValue mfiValue, IIndicatorValue wprValue, IIndicatorValue cmoValue, IIndicatorValue aosValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!stochValue.IsFinal || !rsiValue.IsFinal || !mfiValue.IsFinal || !wprValue.IsFinal || !cmoValue.IsFinal || !aosValue.IsFinal)
		return;

		_prices[_index % Length] = candle.ClosePrice;

		if (_index > 0)
		_upFlags[_index % Length] = candle.ClosePrice > _lastClose ? 1m : 0m;

		_lastClose = candle.ClosePrice;
		_index++;

		if (_index < Length)
		return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k)
		return;

		var rsi = rsiValue.GetValue<decimal>();
		var mfi = mfiValue.GetValue<decimal>();
		var wpr = wprValue.GetValue<decimal>();
		var cmo = cmoValue.GetValue<decimal>();
		var aos = aosValue.GetValue<decimal>();

		var correlation = CalculateCorrelation();

		decimal pr = 0m;
		for (var i = 0; i < Length; i++)
		pr += _upFlags[i];
		pr /= Length;

		var normalizedRsi = rsi / 100m;
		var normalizedStoch = k / 100m;
		var normalizedMfi = mfi / 100m;
		var normalizedWpr = wpr / 100m + 1m;
		var normalizedCmo = cmo / 200m + 0.5m;
		var normalizedAos = aos / 200m + 0.5m;
		var normalizedCorr = (correlation + 1m) / 2m;
		var normalizedPr = pr;

		var avg = (normalizedRsi + normalizedStoch + normalizedMfi + normalizedWpr + normalizedCmo + normalizedAos + normalizedCorr + normalizedPr) / 8m;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (avg > 0.6m && Position <= 0)
		BuyMarket();
		else if (avg < 0.4m && Position >= 0)
		SellMarket();
	}

	private decimal CalculateCorrelation()
	{
		var n = Length;
		decimal sumY = 0m;
		decimal sumY2 = 0m;
		decimal sumXY = 0m;

		for (var i = 0; i < n; i++)
		{
			var price = _prices[( _index - n + i) % n];
			var x = i + 1;
			sumY += price;
			sumY2 += price * price;
			sumXY += price * x;
		}

		var sumX = n * (n + 1m) / 2m;
		var sumX2 = n * (n + 1m) * (2m * n + 1m) / 6m;

		var numerator = n * sumXY - sumX * sumY;
		var denominator = (decimal)Math.Sqrt((double)((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY)));

		return denominator == 0m ? 0m : numerator / denominator;
	}
}
