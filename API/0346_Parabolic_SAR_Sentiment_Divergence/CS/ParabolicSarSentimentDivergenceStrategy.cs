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
	private decimal _prevSentiment;
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

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevSentiment = default;
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
		.BindEx(_parabolicSar, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
		return;

		// Get SAR value
		var sarPrice = sarValue.ToDecimal();

		// Get current price and sentiment
		var price = candle.ClosePrice;
		var sentiment = GetSentiment(candle);
		var priceAboveSar = price > sarPrice;

		// Skip first candle to initialize previous values
		if (_isFirstCandle)
		{
			_prevPrice = price;
			_prevSentiment = sentiment;
			_prevAboveSar = priceAboveSar;
			_isFirstCandle = false;
			return;
		}

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		// Bullish divergence: Price falling but sentiment rising
		bool bullishDivergence = price < _prevPrice && sentiment > _prevSentiment;

		// Bearish divergence: Price rising but sentiment falling
		bool bearishDivergence = price > _prevPrice && sentiment < _prevSentiment;
		var bullishFlip = !_prevAboveSar && priceAboveSar;
		var bearishFlip = _prevAboveSar && !priceAboveSar;

		// Entry logic
		if (_cooldownRemaining == 0 && bullishFlip && bullishDivergence && Position <= 0)
		{
			// Bullish divergence and price above SAR - Long entry
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			LogInfo($"Buy Signal: SAR={sarPrice}, Price={price}, Sentiment={sentiment}");
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && bearishFlip && bearishDivergence && Position >= 0)
		{
			// Bearish divergence and price below SAR - Short entry
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			LogInfo($"Sell Signal: SAR={sarPrice}, Price={price}, Sentiment={sentiment}");
			_cooldownRemaining = CooldownBars;
		}

		// Exit logic - handled by Parabolic SAR itself
		if (Position > 0 && price < sarPrice)
		{
			// Long position and price below SAR - Exit
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit Long: SAR={sarPrice}, Price={price}");
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && price > sarPrice)
		{
			// Short position and price above SAR - Exit
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit Short: SAR={sarPrice}, Price={price}");
			_cooldownRemaining = CooldownBars;
		}

		// Update previous values
		_prevPrice = price;
		_prevSentiment = sentiment;
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
