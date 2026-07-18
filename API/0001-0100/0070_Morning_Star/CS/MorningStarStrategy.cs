using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Morning Star candle pattern strategy.
/// Morning Star: 1st bearish, 2nd small body (doji), 3rd bullish closing above midpoint of 1st.
/// Evening Star (reverse): 1st bullish, 2nd small body, 3rd bearish closing below midpoint of 1st.
/// Uses SMA for exit signals.
/// </summary>
public class MorningStarStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private ICandleMessage _bar1;
	private ICandleMessage _bar2;
	private int _cooldown;

	/// <summary>
	/// MA Period.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MorningStarStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_bar1 = null;
		_bar2 = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bar1 = null;
		_bar2 = null;
		_cooldown = 0;

		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			_bar1 = _bar2;
			_bar2 = candle;
			return;
		}

		if (_bar1 != null && _bar2 != null)
		{
			// Morning Star (bullish reversal)
			var firstBearish = _bar1.ClosePrice < _bar1.OpenPrice;
			var firstBody = Math.Abs(_bar1.OpenPrice - _bar1.ClosePrice);
			var secondBody = Math.Abs(_bar2.OpenPrice - _bar2.ClosePrice);
			var secondSmall = firstBody > 0 && secondBody < firstBody * 0.5m;
			var thirdBullish = candle.ClosePrice > candle.OpenPrice;
			var firstMid = (_bar1.HighPrice + _bar1.LowPrice) / 2;
			var morningStar = firstBearish && secondSmall && thirdBullish && candle.ClosePrice > firstMid;

			// Evening Star (bearish reversal)
			var firstBullish = _bar1.ClosePrice > _bar1.OpenPrice;
			var thirdBearish = candle.ClosePrice < candle.OpenPrice;
			var eveningStar = firstBullish && secondSmall && thirdBearish && candle.ClosePrice < firstMid;

			if (Position == 0 && morningStar)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (Position == 0 && eveningStar)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position > 0 && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
			else if (Position < 0 && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_bar1 = _bar2;
		_bar2 = candle;
	}
}
