using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Band strategy - buys when price crosses below lower band (reversal),
/// sells when price crosses above upper band (reversal). Uses cooldown between trades.
/// </summary>
public class ArpitBollingerBandStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ArpitBollingerBandStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bollinger Bands length", "Bollinger");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "StdDev multiplier", "Bollinger");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");
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
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (bbValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bbValue;
		var upper = bb.UpBand ?? 0m;
		var lower = bb.LowBand ?? 0m;

		if (upper == 0 || lower == 0)
			return;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// Price crosses below lower band then returns above = buy reversal
		var crossUpFromBelow = _prevClose <= _prevLower && _prevLower > 0 && candle.ClosePrice > lower;
		// Price crosses above upper band then returns below = sell reversal
		var crossDownFromAbove = _prevClose >= _prevUpper && _prevUpper > 0 && candle.ClosePrice < upper;

		if (crossUpFromBelow && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (crossDownFromAbove && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
