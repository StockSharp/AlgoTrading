namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// EMA/SMA + RSI Strategy.
/// Uses three EMAs for trend and crossover, with RSI for exit signals.
/// Buy on fast EMA crossing above medium EMA when both above slow EMA.
/// Sell on fast EMA crossing below medium EMA when both below slow EMA.
/// </summary>
public class EmaSmaRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _emaA;
	private ExponentialMovingAverage _emaB;
	private ExponentialMovingAverage _emaC;
	private RelativeStrengthIndex _rsi;

	private decimal _prevEmaA;
	private decimal _prevEmaB;
	private int _cooldownRemaining;

	public EmaSmaRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_emaALength = Param(nameof(EmaALength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
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

		_emaA = null;
		_emaB = null;
		_emaC = null;
		_rsi = null;
		_prevEmaA = 0;
		_prevEmaB = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaA = new ExponentialMovingAverage { Length = EmaALength };
		_emaB = new ExponentialMovingAverage { Length = EmaBLength };
		_emaC = new ExponentialMovingAverage { Length = EmaCLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaA, _emaB, _emaC, _rsi, OnProcess)
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

	private void OnProcess(ICandleMessage candle, decimal emaA, decimal emaB, decimal emaC, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaA.IsFormed || !_emaB.IsFormed || !_emaC.IsFormed || !_rsi.IsFormed)
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

		// Crossover detection
		var bullishCross = emaA > emaB && _prevEmaA <= _prevEmaB && _prevEmaA > 0;
		var bearishCross = emaA < emaB && _prevEmaA >= _prevEmaB && _prevEmaA > 0;

		// Exit long on RSI overbought
		if (Position > 0 && rsi > 70)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short on RSI oversold
		else if (Position < 0 && rsi < 30)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Buy: fast crosses above medium, both above slow
		else if (bullishCross && emaA > emaC && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: fast crosses below medium, both below slow
		else if (bearishCross && emaA < emaC && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevEmaA = emaA;
		_prevEmaB = emaB;
	}
}
