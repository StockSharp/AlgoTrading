using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Midday Reversal trading strategy.
/// Trades on price reversals that occur around midday, using MA for trend confirmation.
/// </summary>
public class MiddayReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _ma;

	private decimal _prevClose;
	private decimal _prevPrevClose;
	private int _cooldown;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Initializes a new instance of the <see cref="MiddayReversalStrategy"/>.
	/// </summary>
	public MiddayReversalStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 30)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_ma = default;
		_prevClose = 0;
		_prevPrevClose = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var hour = candle.OpenTime.Hour;

		if (_prevClose == 0)
		{
			_prevClose = close;
			return;
		}

		if (_prevPrevClose == 0)
		{
			_prevPrevClose = _prevClose;
			_prevClose = close;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevClose = _prevClose;
			_prevClose = close;
			return;
		}

		// Midday zone: hours 11-14
		var isMidday = hour >= 11 && hour <= 14;

		var isBullishCandle = close > candle.OpenPrice;
		var isBearishCandle = close < candle.OpenPrice;
		var wasPriceDecreasing = _prevClose < _prevPrevClose;
		var wasPriceIncreasing = _prevClose > _prevPrevClose;

		// Buy at midday reversal: previous decline then bullish candle
		if (isMidday && wasPriceDecreasing && isBullishCandle && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell short at midday reversal: previous increase then bearish candle
		else if (isMidday && wasPriceIncreasing && isBearishCandle && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit on MA cross
		if (Position > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrevClose = _prevClose;
		_prevClose = close;
	}
}
