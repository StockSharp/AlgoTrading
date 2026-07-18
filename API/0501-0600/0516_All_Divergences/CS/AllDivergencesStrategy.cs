using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// All Divergences strategy - trades RSI divergences filtered by moving average.
/// Bullish divergence: price makes lower low but RSI makes higher low.
/// Bearish divergence: price makes higher high but RSI makes lower high.
/// </summary>
public class AllDivergencesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevLowPrice;
	private decimal _prevLowRsi;
	private decimal _prevHighPrice;
	private decimal _prevHighRsi;
	private decimal _curLowPrice;
	private decimal _curLowRsi;
	private decimal _curHighPrice;
	private decimal _curHighRsi;
	private int _barsSinceExtreme;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int LookbackBars { get => _lookbackBars.Value; set => _lookbackBars.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AllDivergencesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of moving average", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_lookbackBars = Param(nameof(LookbackBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Bars", "Bars to look back for divergence", "Indicators");

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
		_prevLowPrice = 0;
		_prevLowRsi = 0;
		_prevHighPrice = 0;
		_prevHighRsi = 0;
		_curLowPrice = decimal.MaxValue;
		_curLowRsi = 100;
		_curHighPrice = 0;
		_curHighRsi = 0;
		_barsSinceExtreme = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track current lows and highs
		if (candle.LowPrice < _curLowPrice)
		{
			_curLowPrice = candle.LowPrice;
			_curLowRsi = rsiValue;
		}
		if (candle.HighPrice > _curHighPrice)
		{
			_curHighPrice = candle.HighPrice;
			_curHighRsi = rsiValue;
		}

		_barsSinceExtreme++;

		// Reset extremes periodically
		if (_barsSinceExtreme >= LookbackBars)
		{
			_prevLowPrice = _curLowPrice;
			_prevLowRsi = _curLowRsi;
			_prevHighPrice = _curHighPrice;
			_prevHighRsi = _curHighRsi;
			_curLowPrice = decimal.MaxValue;
			_curLowRsi = 100;
			_curHighPrice = 0;
			_curHighRsi = 0;
			_barsSinceExtreme = 0;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (_prevLowPrice == 0 || _prevHighPrice == 0)
			return;

		// Bullish divergence: lower low in price, higher low in RSI + price above MA
		var bullishDiv = candle.LowPrice < _prevLowPrice && rsiValue > _prevLowRsi && candle.ClosePrice > maValue;

		// Bearish divergence: higher high in price, lower high in RSI + price below MA
		var bearishDiv = candle.HighPrice > _prevHighPrice && rsiValue < _prevHighRsi && candle.ClosePrice < maValue;

		if (bullishDiv && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (bearishDiv && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
	}
}
