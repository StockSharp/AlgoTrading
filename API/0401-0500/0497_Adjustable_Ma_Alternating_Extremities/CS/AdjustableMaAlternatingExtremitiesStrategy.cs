using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adjustable MA and Alternating Extremities Strategy.
/// Buys when price crosses above the upper Bollinger Band and sells when it crosses below the lower band.
/// </summary>
public class AdjustableMaAlternatingExtremitiesStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool? _isUpper;
	private int _cooldownRemaining;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdjustableMaAlternatingExtremitiesStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Periods for Bollinger Bands", "General")
			.SetOptimize(20, 100, 10);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Bollinger band width", "General")
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

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
		_isUpper = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bands = new BollingerBands { Length = Length, Width = Multiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bands, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bbValue;

		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (candle.HighPrice > upper && _isUpper != true)
		{
			_isUpper = true;

			if (Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (candle.LowPrice < lower && _isUpper != false)
		{
			_isUpper = false;

			if (Position >= 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
