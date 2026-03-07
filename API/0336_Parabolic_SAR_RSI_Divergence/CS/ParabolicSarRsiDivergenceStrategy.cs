using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades Parabolic SAR trend direction with RSI divergence-style reversals.
/// </summary>
public class ParabolicSarRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarAccelerationFactor;
	private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevPrice;
	private bool _hasPrevValues;
	private int _cooldownRemaining;

	/// <summary>
	/// Strategy parameter: Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarAccelerationFactor
	{
		get => _sarAccelerationFactor.Value;
		set => _sarAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Parabolic SAR maximum acceleration factor.
	/// </summary>
	public decimal SarMaxAccelerationFactor
	{
		get => _sarMaxAccelerationFactor.Value;
		set => _sarMaxAccelerationFactor.Value = value;
	}

	/// <summary>
	/// Strategy parameter: RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Strategy parameter: RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Number of closed candles between position changes.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicSarRsiDivergenceStrategy()
	{
		_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
			.SetRange(0.01m, 0.25m)
			.SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicator Settings");

		_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
			.SetRange(0.1m, 0.5m)
			.SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "Indicator Settings");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level for bullish reversal detection", "Indicator Settings");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level for bearish reversal detection", "Indicator Settings");

		_cooldownBars = Param(nameof(CooldownBars), 24)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
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

		_prevRsi = 0;
		_prevPrice = 0;
		_hasPrevValues = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, rsi);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			StoreState(candle.ClosePrice, rsiValue);
			return;
		}

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullishDivergence = candle.ClosePrice < _prevPrice && rsiValue > _prevRsi;
		var bearishDivergence = candle.ClosePrice > _prevPrice && rsiValue < _prevRsi;
		var bullishReversal = _prevRsi < RsiOversold && rsiValue >= RsiOversold;
		var bearishReversal = _prevRsi > RsiOverbought && rsiValue <= RsiOverbought;
		var canTrade = _cooldownRemaining == 0;

		if (canTrade && (bullishDivergence || bullishReversal) && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (canTrade && (bearishDivergence || bearishReversal) && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}

		StoreState(candle.ClosePrice, rsiValue);
	}

	private void StoreState(decimal price, decimal rsi)
	{
		_prevPrice = price;
		_prevRsi = rsi;
		_hasPrevValues = true;
	}
}
