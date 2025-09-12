using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// New York session breakout strategy.
/// Collects the 13:00-13:30 UTC range and trades the breakout on the next bar.
/// </summary>
public class NyBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _hi;
	private decimal? _lo;
	private bool _wasSession;
	private decimal _tickSize;

	/// <summary>
	/// Take profit to stop ratio.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NyBreakoutStrategy"/> class.
	/// </summary>
	public NyBreakoutStrategy()
	{
		_rewardRisk = Param(nameof(RewardRisk), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Stop Ratio", "Take profit vs stop ratio", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_hi = null;
		_lo = null;
		_wasSession = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var t = candle.OpenTime;
		var inSession = t.Hour == 13 && t.Minute < 30;

		if (inSession)
		{
			_hi = _hi.HasValue ? Math.Max(_hi.Value, candle.HighPrice) : candle.HighPrice;
			_lo = _lo.HasValue ? Math.Min(_lo.Value, candle.LowPrice) : candle.LowPrice;
		}
		else if (_wasSession && _hi is decimal hi && _lo is decimal lo)
		{
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var volume = Volume + Math.Abs(Position);

			if (candle.ClosePrice > hi && Position <= 0)
			{
				BuyMarket(volume);
				SellLimit(candle.ClosePrice + (candle.ClosePrice - lo) * RewardRisk, volume);
				SellStop(lo - _tickSize, volume);
			}
			else if (candle.ClosePrice < lo && Position >= 0)
			{
				SellMarket(volume);
				BuyLimit(candle.ClosePrice - (hi - candle.ClosePrice) * RewardRisk, volume);
				BuyStop(hi + _tickSize, volume);
			}

			_hi = null;
			_lo = null;
		}

		_wasSession = inSession;
	}
}
