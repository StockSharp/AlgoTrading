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
/// Parabolic SAR strategy with sentiment divergence.
/// </summary>
public class ParabolicSarSentimentDivergenceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _startAf;
	private readonly StrategyParam<decimal> _maxAf;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ParabolicSar _parabolicSar;
	private decimal _prevPrice;
	private bool _prevAboveSar;
	private bool _isFirstCandle = true;
	private int _cooldownRemaining;

	/// <summary>
	/// SAR Starting acceleration factor.
	/// </summary>
	public decimal StartAf
	{
		get => _startAf.Value;
		set => _startAf.Value = value;
	}

	/// <summary>
	/// SAR Maximum acceleration factor.
	/// </summary>
	public decimal MaxAf
	{
		get => _maxAf.Value;
		set => _maxAf.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ParabolicSarSentimentDivergenceStrategy"/>.
	/// </summary>
	public ParabolicSarSentimentDivergenceStrategy()
	{
		_startAf = Param(nameof(StartAf), 0.02m)
		.SetRange(0.01m, 0.1m)
		
		.SetDisplay("Starting AF", "Starting acceleration factor for Parabolic SAR", "SAR Parameters");

		_maxAf = Param(nameof(MaxAf), 0.2m)
		.SetRange(0.1m, 0.5m)
		
		.SetDisplay("Maximum AF", "Maximum acceleration factor for Parabolic SAR", "SAR Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 24)
		.SetNotNegative()
		.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");

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

		_parabolicSar = null;
		_prevPrice = default;
		_prevAboveSar = default;
		_isFirstCandle = true;
		_cooldownRemaining = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicator
		_parabolicSar = new ParabolicSar
		{
			Acceleration = StartAf,
			AccelerationMax = MaxAf,
		};


		// Create subscription
		var subscription = SubscribeCandles(CandleType);

		// Bind indicator and processor
		subscription
		.Bind(_parabolicSar, ProcessCandle)
		.Start();

		// Setup visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}

		// Start position protection
		StartProtection(
		new Unit(2, UnitTypes.Percent),   // Take profit 2%
		new Unit(2, UnitTypes.Percent),   // Stop loss 2%
		true							 // Use trailing stop
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarPrice)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var price = candle.ClosePrice;
		var priceAboveSar = price > sarPrice;

		if (_isFirstCandle)
		{
			_prevPrice = price;
			_prevAboveSar = priceAboveSar;
			_isFirstCandle = false;
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullishFlip = !_prevAboveSar && priceAboveSar;
		var bearishFlip = _prevAboveSar && !priceAboveSar;

		if (_cooldownRemaining == 0 && Position == 0)
		{
			if (bullishFlip)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (bearishFlip)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevPrice = price;
		_prevAboveSar = priceAboveSar;
	}

	private decimal GetSentiment(ICandleMessage candle)
	{
		var totalRange = candle.HighPrice - candle.LowPrice;
		if (totalRange <= 0)
			return 0m;

		var body = candle.ClosePrice - candle.OpenPrice;
		var bodyRatio = body / totalRange;
		var rangeRatio = totalRange / Math.Max(candle.OpenPrice, 1m);
		var sentiment = (bodyRatio * 0.7m) + (Math.Sign(body) * Math.Min(0.3m, rangeRatio * 10m));

		return Math.Max(-1m, Math.Min(1m, sentiment));
	}
}
