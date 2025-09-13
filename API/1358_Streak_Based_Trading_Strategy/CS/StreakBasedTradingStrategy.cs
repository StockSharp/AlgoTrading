using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on candle win/loss streaks.
/// Enters a long position after consecutive losses or a short position after consecutive wins.
/// Holds the position for a fixed number of candles and ignores doji candles.
/// </summary>
public class StreakBasedTradingStrategy : Strategy
{
	private readonly StrategyParam<Sides?> _tradeDirection;
	private readonly StrategyParam<int> _streakThreshold;
	private readonly StrategyParam<int> _holdDuration;
	private readonly StrategyParam<decimal> _dojiThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private int _winStreak;
	private int _lossStreak;
	private int _holdCounter;

	public Sides? TradeDirection
{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
}

	public int StreakThreshold
	{
		get => _streakThreshold.Value;
		set => _streakThreshold.Value = value;
	}

	public int HoldDuration
	{
		get => _holdDuration.Value;
		set => _holdDuration.Value = value;
	}

	public decimal DojiThreshold
	{
		get => _dojiThreshold.Value;
		set => _dojiThreshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

public StreakBasedTradingStrategy()
{
		_tradeDirection = Param(nameof(TradeDirection), Sides.Buy)
			.SetDisplay("Trade Direction", "Choose Long or Short", "General");

		_streakThreshold = Param(nameof(StreakThreshold), 8)
			.SetDisplay("Streak Threshold", "Number of streaks before trade", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_holdDuration = Param(nameof(HoldDuration), 7)
			.SetDisplay("Hold Duration", "Holding period in candles", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_dojiThreshold = Param(nameof(DojiThreshold), 0.01m)
			.SetDisplay("Doji Threshold (%)", "Doji sensitivity in percent", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.001m, 0.1m, 0.001m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = default;
		_winStreak = 0;
		_lossStreak = 0;
		_holdCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position != 0)
		{
			_holdCounter--;
			if (_holdCounter <= 0)
			{
				ClosePosition();
				_winStreak = 0;
				_lossStreak = 0;
			}
		}
		else
		{
			if (_prevClose != default && candle.HighPrice > candle.LowPrice)
			{
				var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				var range = candle.HighPrice - candle.LowPrice;
				var isDoji = body / range < DojiThreshold / 100m;

				if (isDoji)
				{
					_winStreak = 0;
					_lossStreak = 0;
				}
				else if (candle.ClosePrice > _prevClose)
				{
					_winStreak++;
					_lossStreak = 0;
				}
				else if (candle.ClosePrice < _prevClose)
				{
					_lossStreak++;
					_winStreak = 0;
				}
				else
				{
					_winStreak = 0;
					_lossStreak = 0;
				}
			}

		if (TradeDirection != Sides.Sell && _lossStreak >= StreakThreshold)
			{
				BuyMarket();
				_holdCounter = HoldDuration;
			}
		else if (TradeDirection != Sides.Buy && _winStreak >= StreakThreshold)
			{
				SellMarket();
				_holdCounter = HoldDuration;
			}
		}

		_prevClose = candle.ClosePrice;
	}
}