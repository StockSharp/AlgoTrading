namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD + Bollinger Bands + RSI Strategy.
/// Uses MACD for momentum, BB for volatility levels, RSI for confirmation.
/// Buys when MACD bullish + price near lower BB + RSI oversold.
/// Sells when MACD bearish + price near upper BB + RSI overbought.
/// </summary>
public class MacdBbRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private MovingAverageConvergenceDivergence _macd;
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private decimal _prevMacd;
	private int _cooldownRemaining;

	public MacdBbRsiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands");

		_bbWidth = Param(nameof(BBWidth), 1.5m)
			.SetDisplay("BB Width", "BB standard deviation multiplier", "Bollinger Bands");

		_rsiLength = Param(nameof(RSILength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_cooldownBars = Param(nameof(CooldownBars), 50)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BBWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public int RSILength
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

		_macd = null;
		_bollinger = null;
		_rsi = null;
		_prevMacd = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_macd = new MovingAverageConvergenceDivergence();
		_bollinger = new BollingerBands { Length = BBLength, Width = BBWidth };
		_rsi = new RelativeStrengthIndex { Length = RSILength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, OnProcess)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process MACD and BB manually
		var macdResult = _macd.Process(candle);
		var bbResult = _bollinger.Process(candle);

		if (!_macd.IsFormed || !_bollinger.IsFormed)
			return;

		var macdVal = macdResult.ToDecimal();
		var bb = (BollingerBandsValue)bbResult;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMacd = macdVal;
			return;
		}

		var close = candle.ClosePrice;

		// Buy: price below lower BB + RSI oversold + MACD positive
		if (close <= lower && rsi < 30 && Position == 0)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}
		// Sell: price above upper BB + RSI overbought + MACD negative
		else if (close >= upper && rsi > 70 && Position == 0)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevMacd = macdVal;
	}
}
