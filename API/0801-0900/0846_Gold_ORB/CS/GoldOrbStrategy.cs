using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold opening range breakout strategy.
/// The high and low of the candles opening between AsiaStart and AsiaEnd (UTC) form the Asia range of the day. Between TradeStart and
/// TradeEnd a close above the range high goes long and a close below the range low goes short. The stop is one range size away from the
/// entry and the target RewardMultiplier range sizes away.
/// </summary>
public class GoldOrbStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _asiaStart;
	private readonly StrategyParam<TimeSpan> _asiaEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<decimal> _rewardMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _rangeDay;
	private decimal? _asiaHigh;
	private decimal? _asiaLow;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Start of the Asia session (UTC).
	/// </summary>
	public TimeSpan AsiaStart
	{
		get => _asiaStart.Value;
		set => _asiaStart.Value = value;
	}

	/// <summary>
	/// End of the Asia session (UTC).
	/// </summary>
	public TimeSpan AsiaEnd
	{
		get => _asiaEnd.Value;
		set => _asiaEnd.Value = value;
	}

	/// <summary>
	/// Start of the trade window (UTC).
	/// </summary>
	public TimeSpan TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// End of the trade window (UTC).
	/// </summary>
	public TimeSpan TradeEnd
	{
		get => _tradeEnd.Value;
		set => _tradeEnd.Value = value;
	}

	/// <summary>
	/// Target distance in range sizes.
	/// </summary>
	public decimal RewardMultiplier
	{
		get => _rewardMultiplier.Value;
		set => _rewardMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public GoldOrbStrategy()
	{
		_asiaStart = Param(nameof(AsiaStart), TimeSpan.Zero)
			.SetDisplay("Asia Start", "Start of the Asia session (UTC)", "Session");

		_asiaEnd = Param(nameof(AsiaEnd), TimeSpan.FromHours(6))
			.SetDisplay("Asia End", "End of the Asia session (UTC)", "Session");

		_tradeStart = Param(nameof(TradeStart), TimeSpan.FromHours(6))
			.SetDisplay("Trade Start", "Start of the trade window (UTC)", "Session");

		_tradeEnd = Param(nameof(TradeEnd), TimeSpan.FromHours(10))
			.SetDisplay("Trade End", "End of the trade window (UTC)", "Session");

		_rewardMultiplier = Param(nameof(RewardMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Multiplier", "Target distance in range sizes", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_rangeDay = null;
		_asiaHigh = null;
		_asiaLow = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		var day = candle.OpenTime.Date;
		var timeOfDay = candle.OpenTime.TimeOfDay;

		if (_rangeDay != day)
		{
			_rangeDay = day;
			_asiaHigh = null;
			_asiaLow = null;
		}

		if (timeOfDay >= AsiaStart && timeOfDay < AsiaEnd)
		{
			_asiaHigh = _asiaHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_asiaLow = _asiaLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
		}

		if (ManagePosition(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 || timeOfDay < TradeStart || timeOfDay >= TradeEnd)
			return;

		if (_asiaHigh is not decimal high || _asiaLow is not decimal low)
			return;

		var range = high - low;
		if (range <= 0)
			return;

		var close = candle.ClosePrice;

		if (close > high)
		{
			BuyMarket(Volume);
			_stopPrice = close - range;
			_takePrice = close + range * RewardMultiplier;
		}
		else if (close < low)
		{
			SellMarket(Volume);
			_stopPrice = close + range;
			_takePrice = close - range * RewardMultiplier;
		}
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
