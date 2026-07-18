namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Four Bar Momentum Reversal Strategy.
/// Enters long after consecutive closes below the close from N bars ago.
/// Exits on breakout above previous high.
/// Uses EMA as trend filter.
/// </summary>
public class FourBarMomentumReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _buyThreshold;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;
	private readonly List<decimal> _closes = new();
	private int _belowCount;
	private decimal _prevHigh;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BuyThreshold { get => _buyThreshold.Value; set => _buyThreshold.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public FourBarMomentumReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_buyThreshold = Param(nameof(BuyThreshold), 3)
			.SetGreaterThanZero()
			.SetDisplay("Buy Threshold", "Consecutive closes below reference to trigger buy", "Strategy");

		_lookback = Param(nameof(Lookback), 4)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars to compare", "Strategy");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_closes.Clear();
		_belowCount = 0;
		_prevHigh = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
		{
			_closes.Add(candle.ClosePrice);
			_prevHigh = candle.HighPrice;
			return;
		}

		var close = candle.ClosePrice;

		// Track past closes for lookback comparison
		if (_closes.Count >= Lookback)
		{
			var pastClose = _closes[_closes.Count - Lookback];

			if (close < pastClose)
				_belowCount++;
			else
				_belowCount = 0;
		}

		_closes.Add(close);

		// Keep list from growing too large
		if (_closes.Count > Lookback + 10)
			_closes.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevHigh = candle.HighPrice;
			return;
		}

		// Buy: consecutive closes below reference + price below EMA (reversal from weakness)
		if (_belowCount >= BuyThreshold && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: breakout above previous high
		else if (Position > 0 && close > _prevHigh)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Short: consecutive closes above reference (overbought reversal)
		else if (_belowCount == 0 && _closes.Count > Lookback)
		{
			var pastClose = _closes[_closes.Count - 1 - Lookback];
			var aboveCount = 0;
			for (int i = _closes.Count - 1; i >= Math.Max(0, _closes.Count - BuyThreshold); i--)
			{
				if (_closes[i] > pastClose)
					aboveCount++;
				else
					break;
			}

			if (aboveCount >= BuyThreshold && Position >= 0 && close > emaVal)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}

		_prevHigh = candle.HighPrice;
	}
}
