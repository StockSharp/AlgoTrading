using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Autonomous 5-Minute Robot strategy.
/// Uses SMA trend filter with RSI momentum for entries.
/// Buys when RSI crosses above 50 in uptrend, sells when RSI crosses below 50 in downtrend.
/// </summary>
public class Autonomous5MinuteRobotStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
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
	public Autonomous5MinuteRobotStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Moving average length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

		var sma = new SimpleMovingAverage { Length = MaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		var isBullish = candle.ClosePrice > maValue;
		var isBearish = candle.ClosePrice < maValue;

		// RSI crosses above 50 with uptrend
		var longSignal = _prevRsi > 0 && _prevRsi < 50 && rsiValue >= 50 && isBullish;
		// RSI crosses below 50 with downtrend
		var shortSignal = _prevRsi > 0 && _prevRsi > 50 && rsiValue <= 50 && isBearish;

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
