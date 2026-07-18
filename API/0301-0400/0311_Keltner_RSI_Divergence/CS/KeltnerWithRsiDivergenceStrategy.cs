using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion strategy that trades Keltner band extremes only when RSI diverges from price.
/// </summary>
public class KeltnerWithRsiDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private decimal _prevPrice;
	private bool _isInitialized;
	private int _cooldown;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner bands.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Bars to wait after each order.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public KeltnerWithRsiDivergenceStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetRange(2, 100)
			.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.15m)
			.SetRange(0.1m, 10m)
			.SetDisplay("ATR Multiplier", "Multiplier for the Keltner band width", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(2, 100)
			.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 72)
			.SetRange(1, 500)
			.SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetRange(0.5m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_atr = null;
		_rsi = null;
		_prevRsi = 50m;
		_prevPrice = 0m;
		_isInitialized = false;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_isInitialized = false;
		_cooldown = 0;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_ema, _atr, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent), false);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed || !_rsi.IsFormed)
			return;

		if (ProcessState != ProcessStates.Started)
			return;

		if (!_isInitialized)
		{
			_prevPrice = candle.ClosePrice;
			_prevRsi = rsiValue;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrice = candle.ClosePrice;
			_prevRsi = rsiValue;
			return;
		}

		var upperBand = emaValue + AtrMultiplier * atrValue;
		var lowerBand = emaValue - AtrMultiplier * atrValue;
		var bullishDivergence = (rsiValue >= _prevRsi && candle.ClosePrice < _prevPrice) || rsiValue <= 30m;
		var bearishDivergence = (rsiValue <= _prevRsi && candle.ClosePrice > _prevPrice) || rsiValue >= 70m;
		var price = candle.ClosePrice;

		if (Position == 0)
		{
			if (price <= lowerBand + atrValue * 0.1m && bullishDivergence)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (price >= upperBand - atrValue * 0.1m && bearishDivergence)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (price >= emaValue || rsiValue >= 50m)
			{
				SellMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (price <= emaValue || rsiValue <= 50m)
			{
				BuyMarket(Math.Abs(Position));
				_cooldown = CooldownBars;
			}
		}

		_prevPrice = price;
		_prevRsi = rsiValue;
	}
}
