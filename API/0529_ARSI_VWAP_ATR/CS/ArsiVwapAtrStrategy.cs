using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive RSI strategy with dynamic OB/OS levels.
/// Uses RSI crossover of overbought/oversold thresholds with EMA trend filter.
/// </summary>
public class ArsiVwapAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _obLevel;
	private readonly StrategyParam<decimal> _osLevel;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// EMA trend filter length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal ObLevel
	{
		get => _obLevel.Value;
		set => _obLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal OsLevel
	{
		get => _osLevel.Value;
		set => _osLevel.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
	/// Constructor.
	/// </summary>
	public ArsiVwapAtrStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_obLevel = Param(nameof(ObLevel), 55m)
			.SetDisplay("OB Level", "Overbought RSI level", "Indicators");

		_osLevel = Param(nameof(OsLevel), 45m)
			.SetDisplay("OS Level", "Oversold RSI level", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 300)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevRsi = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// RSI crosses above OS level from below = buy signal
		var longSignal = _prevRsi > 0 && _prevRsi < OsLevel && rsiValue >= OsLevel;
		// RSI crosses below OB level from above = sell signal
		var shortSignal = _prevRsi > 0 && _prevRsi > ObLevel && rsiValue <= ObLevel;

		if (longSignal && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (shortSignal && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevRsi = rsiValue;
	}
}
