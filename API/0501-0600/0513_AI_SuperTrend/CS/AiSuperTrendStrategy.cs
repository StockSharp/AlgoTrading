using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AI SuperTrend Strategy - trades SuperTrend signals combined with WMA trend filter.
/// </summary>
public class AiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _wmaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private bool _prevIsUpTrend;
	private bool _isInitialized;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }
	public int WmaLength { get => _wmaLength.Value; set => _wmaLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AiSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend");

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetDisplay("ATR Factor", "ATR factor for SuperTrend", "SuperTrend");

		_wmaLength = Param(nameof(WmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("WMA Length", "WMA length for trend filter", "AI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevIsUpTrend = false;
		_isInitialized = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var superTrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrFactor };
		var wma = new WeightedMovingAverage { Length = WmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, wma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawIndicator(area, wma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stTyped = (SuperTrendIndicatorValue)stValue;
		var isUpTrend = stTyped.IsUpTrend;
		var wma = wmaValue.ToDecimal();

		if (!_isInitialized)
		{
			_prevIsUpTrend = isUpTrend;
			_isInitialized = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevIsUpTrend = isUpTrend;
			return;
		}

		// Long: SuperTrend flips to uptrend + price above WMA
		if (!_prevIsUpTrend && isUpTrend && candle.ClosePrice > wma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Short: SuperTrend flips to downtrend + price below WMA
		else if (_prevIsUpTrend && !isUpTrend && candle.ClosePrice < wma && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevIsUpTrend = isUpTrend;
	}
}
