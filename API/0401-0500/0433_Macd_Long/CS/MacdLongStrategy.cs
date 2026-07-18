namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD Long Strategy.
/// Uses MACD crossover with RSI oversold/overbought lookback for timing.
/// Buys when RSI was recently oversold and MACD turns positive.
/// Sells when RSI was recently overbought and MACD turns negative.
/// </summary>
public class MacdLongStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private MovingAverageConvergenceDivergence _macd;

	private int _barsSinceOversold;
	private int _barsSinceOverbought;
	private decimal _prevMacd;
	private int _cooldownRemaining;

	public MacdLongStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiOversold = Param(nameof(RsiOversold), 40)
			.SetDisplay("RSI Oversold", "Oversold level", "RSI");

		_rsiOverbought = Param(nameof(RsiOverbought), 60)
			.SetDisplay("RSI Overbought", "Overbought level", "RSI");

		_lookbackBars = Param(nameof(LookbackBars), 20)
			.SetDisplay("Lookback Bars", "Bars to look back for RSI conditions", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public int RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
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

		_rsi = null;
		_macd = null;
		_barsSinceOversold = int.MaxValue;
		_barsSinceOverbought = int.MaxValue;
		_prevMacd = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _macd, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsi, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_macd.IsFormed)
		{
			_prevMacd = macdVal;
			return;
		}

		// Track RSI oversold/overbought
		if (rsi <= RsiOversold)
			_barsSinceOversold = 0;
		else
			_barsSinceOversold = Math.Min(_barsSinceOversold + 1, int.MaxValue - 1);

		if (rsi >= RsiOverbought)
			_barsSinceOverbought = 0;
		else
			_barsSinceOverbought = Math.Min(_barsSinceOverbought + 1, int.MaxValue - 1);

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

		var wasOversold = _barsSinceOversold <= LookbackBars;
		var wasOverbought = _barsSinceOverbought <= LookbackBars;

		// MACD zero cross
		var macdCrossUp = macdVal > 0 && _prevMacd <= 0 && _prevMacd != 0;
		var macdCrossDown = macdVal < 0 && _prevMacd >= 0 && _prevMacd != 0;

		// Buy: RSI was recently oversold + MACD crosses above zero
		if (wasOversold && macdCrossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: RSI was recently overbought + MACD crosses below zero
		else if (wasOverbought && macdCrossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long on MACD cross down
		else if (Position > 0 && macdCrossDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short on MACD cross up
		else if (Position < 0 && macdCrossUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevMacd = macdVal;
	}
}
