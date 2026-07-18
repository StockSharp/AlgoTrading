namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Bollinger Breakout Strategy - trades Bollinger Band breakouts with trend and RSI filters.
/// Buys when close crosses above lower band with uptrend.
/// Sells when close crosses below upper band with downtrend.
/// </summary>
public class BollingerBreakout2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _isInitial;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public BollingerBreakout2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Multiplier", "Standard deviation multiplier", "Bollinger Bands");

		_trendLength = Param(nameof(TrendLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Length for trend moving average", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "Filters");

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

		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_isInitial = true;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var trendSma = new SimpleMovingAverage { Length = TrendLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, trendSma, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue trendValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (bollingerValue is not BollingerBandsValue bb)
			return;

		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var trend = trendValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var close = candle.ClosePrice;

		if (_isInitial)
		{
			_prevClose = close;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_isInitial = false;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = close;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = close;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			return;
		}

		var trendConditionLong = close > trend;
		var trendConditionShort = close < trend;

		// Long entry: close breaks above upper band + uptrend
		if (close > upperBand && _prevClose <= _prevUpper && Position <= 0 && trendConditionLong)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Short entry: close breaks below lower band + downtrend
		else if (close < lowerBand && _prevClose >= _prevLower && Position >= 0 && trendConditionShort)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: close drops below middle band
		else if (Position > 0 && close < middleBand)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: close rises above middle band
		else if (Position < 0 && close > middleBand)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = close;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
	}
}
