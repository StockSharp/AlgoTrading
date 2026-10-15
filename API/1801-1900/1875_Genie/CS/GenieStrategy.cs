using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with momentum confirmation and trailing protection.
/// </summary>
public class GenieStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _rsiLongLevel;
	private readonly StrategyParam<decimal> _rsiShortLevel;

	private decimal _prevSar;
	private ICandleMessage _prevCandle = null!;
	private int _cooldownRemaining;

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public decimal RsiLongLevel
	{
		get => _rsiLongLevel.Value;
		set => _rsiLongLevel.Value = value;
	}

	public decimal RsiShortLevel
	{
		get => _rsiShortLevel.Value;
		set => _rsiShortLevel.Value = value;
	}

	public GenieStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetDisplay("Take Profit", "Take profit distance", "Protection");

		_trailingStop = Param(nameof(TrailingStop), 200m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Protection");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetDisplay("SAR Step", "Acceleration factor", "Indicator");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("Momentum Period", "Period for momentum confirmation", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 4)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");

		_rsiLongLevel = Param(nameof(RsiLongLevel), 55m)
			.SetDisplay("RSI Long", "Minimum RSI level for long entries", "Filters");

		_rsiShortLevel = Param(nameof(RsiShortLevel), 45m)
			.SetDisplay("RSI Short", "Maximum RSI level for short entries", "Filters");
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
		_prevSar = 0m;
		_prevCandle = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sar = new ParabolicSar { AccelerationStep = SarStep, AccelerationMax = 0.2m };
		var rsi = new RelativeStrengthIndex { Length = AdxPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription.BindEx(sar, rsi, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(TrailingStop, UnitTypes.Absolute),
			isStopTrailing: true,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished || !sarValue.IsFinal || !rsiValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var sarCurrent = sarValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		if (_prevCandle == null)
		{
			_prevSar = sarCurrent;
			_prevCandle = candle;
			return;
		}

		var sellCondition = _cooldownRemaining == 0 &&
			_prevSar < _prevCandle.ClosePrice &&
			sarCurrent > candle.ClosePrice &&
			rsi <= RsiShortLevel;

		var buyCondition = _cooldownRemaining == 0 &&
			_prevSar > _prevCandle.ClosePrice &&
			sarCurrent < candle.ClosePrice &&
			rsi >= RsiLongLevel;

		if (Position == 0)
		{
			if (sellCondition)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (buyCondition)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && _prevCandle.OpenPrice > _prevCandle.ClosePrice)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && _prevCandle.OpenPrice < _prevCandle.ClosePrice)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevSar = sarCurrent;
		_prevCandle = candle;
	}
}

