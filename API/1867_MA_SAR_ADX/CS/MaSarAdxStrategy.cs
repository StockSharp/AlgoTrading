using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Moving Average, Parabolic SAR and momentum confirmation.
/// </summary>
public class MaSarAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _rsiLongLevel;
	private readonly StrategyParam<decimal> _rsiShortLevel;

	private int _cooldownRemaining;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal RsiLongLevel { get => _rsiLongLevel.Value; set => _rsiLongLevel.Value = value; }
	public decimal RsiShortLevel { get => _rsiShortLevel.Value; set => _rsiShortLevel.Value = value; }

	public MaSarAdxStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum confirmation period", "Indicators");

		_sarStep = Param(nameof(SarStep), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Step", "Initial acceleration factor", "Indicators");

		_sarMax = Param(nameof(SarMax), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max", "Maximum acceleration factor", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");

		_rsiLongLevel = Param(nameof(RsiLongLevel), 52m)
			.SetDisplay("RSI Long", "Minimum RSI level for long entries", "Filters");

		_rsiShortLevel = Param(nameof(RsiShortLevel), 48m)
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
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMax
		};
		var rsi = new RelativeStrengthIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, sar, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue sarValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished || !maValue.IsFinal || !sarValue.IsFinal || !rsiValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var price = candle.ClosePrice;
		var ma = maValue.ToDecimal();
		var sar = sarValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var longSignal = price > ma && price > sar && rsi >= RsiLongLevel;
		var shortSignal = price < ma && price < sar && rsi <= RsiShortLevel;
		var longExit = price < sar || price < ma;
		var shortExit = price > sar || price > ma;

		if (Position == 0 && _cooldownRemaining == 0)
		{
			if (longSignal)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (shortSignal)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && longExit)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && shortExit)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
	}
}

