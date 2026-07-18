namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Tendency EMA + RSI Strategy.
/// Uses EMA crossover with RSI and trend filter.
/// Buys when fast EMA crosses above medium EMA while above slow EMA.
/// Exits when RSI becomes overbought/oversold.
/// </summary>
public class TendencyEmaRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
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

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	public TendencyEmaRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI");

		_emaALength = Param(nameof(EmaALength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Fast EMA length", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Medium EMA length", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Slow/Trend EMA length", "Moving Averages");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
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

		// EMA crossovers
		var emaCrossUp = emaA > emaB && _prevEmaA <= _prevEmaB;
		var emaCrossDown = emaA < emaB && _prevEmaA >= _prevEmaB;

		// Buy: EMA A crosses above EMA B + EMA A > EMA C (uptrend) + bullish candle
		if (emaCrossUp && emaA > emaC && candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: EMA A crosses below EMA B + EMA A < EMA C (downtrend) + bearish candle
		else if (emaCrossDown && emaA < emaC && candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: RSI overbought
		else if (Position > 0 && rsiVal > 70)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: RSI oversold
		else if (Position < 0 && rsiVal < 30)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevEmaA = emaA;
		_prevEmaB = emaB;
	}
}
