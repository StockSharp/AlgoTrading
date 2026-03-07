namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Omar MMR Strategy.
/// Uses RSI, triple EMA alignment, and MACD signal crossover for entries.
/// Buys when price > EMA C, EMA A > EMA B, MACD crosses above signal, RSI in range.
/// Sells when EMA alignment reverses or MACD crosses below signal.
/// </summary>
public class OmarMmrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _emaA;
	private ExponentialMovingAverage _emaB;
	private ExponentialMovingAverage _emaC;

	private decimal _prevEmaA;
	private decimal _prevEmaB;
	private int _cooldownRemaining;

	public OmarMmrStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_emaALength = Param(nameof(EmaALength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages");

		_cooldownBars = Param(nameof(CooldownBars), 15)
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

	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
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
		_emaA = null;
		_emaB = null;
		_emaC = null;
		_prevEmaA = 0;
		_prevEmaB = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_emaA = new ExponentialMovingAverage { Length = EmaALength };
		_emaB = new ExponentialMovingAverage { Length = EmaBLength };
		_emaC = new ExponentialMovingAverage { Length = EmaCLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _emaA, _emaB, _emaC, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaA);
			DrawIndicator(area, _emaB);
			DrawIndicator(area, _emaC);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsiVal, decimal emaA, decimal emaB, decimal emaC)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_emaA.IsFormed || !_emaB.IsFormed || !_emaC.IsFormed)
		{
			_prevEmaA = emaA;
			_prevEmaB = emaB;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevEmaA = emaA;
			_prevEmaB = emaB;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevEmaA = emaA;
			_prevEmaB = emaB;
			return;
		}

		if (_prevEmaA == 0 || _prevEmaB == 0)
		{
			_prevEmaA = emaA;
			_prevEmaB = emaB;
			return;
		}

		// EMA alignment
		var bullishAlignment = emaA > emaB && candle.ClosePrice > emaC;
		var bearishAlignment = emaA < emaB && candle.ClosePrice < emaC;

		// EMA A/B crossover
		var emaCrossUp = emaA > emaB && _prevEmaA <= _prevEmaB;
		var emaCrossDown = emaA < emaB && _prevEmaA >= _prevEmaB;

		// RSI filter
		var rsiInBuyRange = rsiVal > 30 && rsiVal < 70;
		var rsiInSellRange = rsiVal > 30 && rsiVal < 70;

		// Buy: bullish EMA alignment + EMA cross up + RSI in range
		if (bullishAlignment && emaCrossUp && rsiInBuyRange && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: bearish EMA alignment + EMA cross down + RSI in range
		else if (bearishAlignment && emaCrossDown && rsiInSellRange && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: EMA A crosses below EMA B
		else if (Position > 0 && emaCrossDown)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: EMA A crosses above EMA B
		else if (Position < 0 && emaCrossUp)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevEmaA = emaA;
		_prevEmaB = emaB;
	}
}
