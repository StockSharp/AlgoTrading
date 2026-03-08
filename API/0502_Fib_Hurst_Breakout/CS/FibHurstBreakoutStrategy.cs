using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fibonacci levels with Hurst exponent filter.
/// Uses rolling high/low for Fib levels and Hurst to detect trending.
/// Enters long when price crosses above 61.8% level and Hurst > 0.5.
/// Enters short when price crosses below 38.2% level and Hurst less than 0.5.
/// </summary>
public class FibHurstBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = [];
	private readonly List<decimal> _lows = [];
	private decimal _prevClose;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int HurstPeriod { get => _hurstPeriod.Value; set => _hurstPeriod.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public FibHurstBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Parameters");

		_hurstPeriod = Param(nameof(HurstPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Period for Hurst exponent", "Parameters");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for Fib level calculation", "Parameters");

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
		_highs.Clear();
		_lows.Clear();
		_prevClose = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var hurst = new HurstExponent { Length = HurstPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hurst, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hurstValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Track rolling highs/lows for Fib levels
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		if (_highs.Count > LookbackPeriod)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count < LookbackPeriod || _prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		// Calculate Fibonacci levels from recent range
		decimal high = decimal.MinValue, low = decimal.MaxValue;
		for (var i = 0; i < _highs.Count; i++)
		{
			if (_highs[i] > high) high = _highs[i];
			if (_lows[i] < low) low = _lows[i];
		}

		var range = high - low;
		if (range <= 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var fib382 = low + 0.382m * range;
		var fib618 = low + 0.618m * range;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevClose = candle.ClosePrice;
			return;
		}

		var crossUp = _prevClose <= fib618 && candle.ClosePrice > fib618;
		var crossDown = _prevClose >= fib382 && candle.ClosePrice < fib382;

		if (hurstValue > 0.5m && crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (hurstValue < 0.5m && crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
	}
}
