using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji Reversal strategy.
/// Looks for doji candlestick patterns after a trend and takes a reversal position.
/// Doji after downtrend = buy, doji after uptrend = sell.
/// Uses SMA for exit signals.
/// </summary>
public class DojiReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _dojiThreshold;
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
	/// Doji threshold as fraction of candle range.
	/// </summary>
	public decimal DojiThreshold
	{
		get => _dojiThreshold.Value;
		set => _dojiThreshold.Value = value;
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
	public DojiReversalStrategy()
	{
		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for SMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_dojiThreshold = Param(nameof(DojiThreshold), 0.1m)
			.SetNotNegative()
			.SetDisplay("Doji Threshold", "Max body/range ratio for doji", "Indicators");

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
			var isDoji = IsDoji(candle);

			if (isDoji)
			{
				var isDowntrend = _bar2.ClosePrice < _bar1.ClosePrice;
				var isUptrend = _bar2.ClosePrice > _bar1.ClosePrice;

				if (Position == 0 && isDowntrend)
				{
					BuyMarket();
					_cooldown = CooldownBars;
				}
				else if (Position == 0 && isUptrend)
				{
					SellMarket();
					_cooldown = CooldownBars;
				}
			}

			// Exit on SMA cross
			if (Position > 0 && candle.ClosePrice < smaValue)
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

	private bool IsDoji(ICandleMessage candle)
	{
		var bodySize = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		var totalRange = candle.HighPrice - candle.LowPrice;

		if (totalRange == 0)
			return false;

		return bodySize / totalRange < DojiThreshold;
	}
}
