using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of Open Drive trading strategy.
/// Trades on strong gap openings relative to previous close using ATR filter and MA trend.
/// </summary>
public class OpenDriveStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClosePrice;
	private decimal _atrValue;
	private int _cooldown;

	/// <summary>
	/// ATR multiplier for gap size.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

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
	/// Initializes a new instance of the <see cref="OpenDriveStrategy"/>.
	/// </summary>
	public OpenDriveStrategy()
	{
		_atrMultiplier = Param(nameof(AtrMultiplier), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR to define gap size", "Strategy");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Strategy");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");

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
		_prevClosePrice = 0;
		_atrValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, atr, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(3, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// indicators checked via Bind

		_atrValue = atrValue;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClosePrice = close;
			return;
		}

		// Detect strong momentum candle (body exceeds ATR * multiplier)
		if (_prevClosePrice > 0 && atrValue > 0)
		{
			var body = close - open;
			var bodySize = Math.Abs(body);

			if (bodySize > atrValue * AtrMultiplier && Position == 0)
			{
				// Bullish momentum + above MA = Buy
				if (body > 0 && close > smaValue)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
				// Bearish momentum + below MA = Sell short
				else if (body < 0 && close < smaValue)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
			}
		}

		_prevClosePrice = close;
	}
}
