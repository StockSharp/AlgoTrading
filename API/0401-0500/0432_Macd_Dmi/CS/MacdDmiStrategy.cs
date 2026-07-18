namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD + DMI Strategy.
/// Uses MACD for momentum and DMI for directional confirmation.
/// Buys when MACD crosses above zero and DI+ > DI-.
/// Sells when MACD crosses below zero and DI- > DI+.
/// </summary>
public class MacdDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private MovingAverageConvergenceDivergence _macd;
	private DirectionalIndex _dmi;
	private decimal _prevMacd;
	private int _cooldownRemaining;

	public MacdDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_dmiLength = Param(nameof(DmiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DMI Length", "DMI period", "DMI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_dmi = null;
		_prevMacd = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();
		_dmi = new DirectionalIndex { Length = DmiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _dmi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed || !_dmi.IsFormed)
			return;

		if (macdValue.IsEmpty)
			return;

		var macdVal = macdValue.ToDecimal();

		var dmiTyped = (DirectionalIndexValue)dmiValue;
		if (dmiTyped.Plus is not decimal diPlus || dmiTyped.Minus is not decimal diMinus)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macdVal;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMacd = macdVal;
			return;
		}

		// MACD zero crossover + DMI direction
		var macdCrossUp = macdVal > 0 && _prevMacd <= 0 && _prevMacd != 0;
		var macdCrossDown = macdVal < 0 && _prevMacd >= 0 && _prevMacd != 0;

		// Buy: MACD crosses above zero + DI+ > DI-
		if (macdCrossUp && diPlus > diMinus && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: MACD crosses below zero + DI- > DI+
		else if (macdCrossDown && diMinus > diPlus && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: DI- crosses above DI+
		else if (Position > 0 && diMinus > diPlus && macdVal < 0)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: DI+ crosses above DI-
		else if (Position < 0 && diPlus > diMinus && macdVal > 0)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevMacd = macdVal;
	}
}
