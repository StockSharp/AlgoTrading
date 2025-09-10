using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Beta Weighted Moving Average (BWMA).
/// Buys when price crosses above BWMA and sells when crosses below.
/// </summary>
public class BetaWeightedMaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _alpha;
	private readonly StrategyParam<decimal> _beta;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = [];
	private readonly List<decimal> _weights = [];
	private decimal _denominator;
	private decimal _prevMa;
	private decimal _prevPrice;

	/// <summary>
	/// Period for BWMA calculation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Alpha (+Lag) parameter.
	/// </summary>
	public decimal Alpha
	{
		get => _alpha.Value;
		set => _alpha.Value = value;
	}

	/// <summary>
	/// Beta (-Lag) parameter.
	/// </summary>
	public decimal Beta
	{
		get => _beta.Value;
		set => _beta.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BetaWeightedMaStrategy"/>.
	/// </summary>
	public BetaWeightedMaStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetDisplay("BWMA Length", "Number of periods for Beta Weighted MA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_alpha = Param(nameof(Alpha), 3m)
			.SetDisplay("Alpha (+Lag)", "Alpha parameter for Beta weighting", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_beta = Param(nameof(Beta), 3m)
			.SetDisplay("Beta (-Lag)", "Beta parameter for Beta weighting", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prices.Clear();
		_weights.Clear();
		_denominator = default;
		_prevMa = default;
		_prevPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Precompute weights based on Beta distribution
		_weights.Clear();
		_denominator = 0m;

		var alpha = (double)Alpha;
		var beta = (double)Beta;

		for (var i = 0; i < Length; i++)
		{
			var x = (double)i / (Length - 1);
			var w = Math.Pow(x, alpha - 1) * Math.Pow(1 - x, beta - 1);
			var wd = (decimal)w;
			_weights.Add(wd);
			_denominator += wd;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_prices.Insert(0, candle.ClosePrice);

		if (_prices.Count > Length)
			_prices.RemoveAt(_prices.Count - 1);

		if (_prices.Count < Length)
			return;

		decimal sum = 0m;

		for (var i = 0; i < Length; i++)
			sum += _prices[i] * _weights[i];

		var ma = sum / _denominator;

		if (_prevMa == 0m)
		{
			_prevMa = ma;
			_prevPrice = candle.ClosePrice;
			return;
		}

		var crossedAbove = candle.ClosePrice > ma && _prevPrice <= _prevMa;
		var crossedBelow = candle.ClosePrice < ma && _prevPrice >= _prevMa;

		if (crossedAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (crossedBelow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevPrice = candle.ClosePrice;
		_prevMa = ma;
	}
}
