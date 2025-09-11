using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold opening range breakout based on Asia session high/low.
/// Trades breakouts during specified trade window with risk/reward targets.
/// </summary>
public class GoldOrbStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _asiaStart;
	private readonly StrategyParam<TimeSpan> _asiaEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<decimal> _rewardMultiplier;

	private decimal? _asiaHigh;
	private decimal? _asiaLow;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private DateTime _currentDay;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start of Asia range (UTC).
	/// </summary>
	public TimeSpan AsiaStart
	{
		get => _asiaStart.Value;
		set => _asiaStart.Value = value;
	}

	/// <summary>
	/// End of Asia range (UTC).
	/// </summary>
	public TimeSpan AsiaEnd
	{
		get => _asiaEnd.Value;
		set => _asiaEnd.Value = value;
	}

	/// <summary>
	/// Start of trading window (UTC).
	/// </summary>
	public TimeSpan TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// End of trading window (UTC).
	/// </summary>
	public TimeSpan TradeEnd
	{
		get => _tradeEnd.Value;
		set => _tradeEnd.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio.
	/// </summary>
	public decimal RewardMultiplier
	{
		get => _rewardMultiplier.Value;
		set => _rewardMultiplier.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GoldOrbStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_asiaStart = Param(nameof(AsiaStart), TimeSpan.Zero)
			.SetDisplay("Asia Start", "Start of Asia range (UTC)", "Time");

		_asiaEnd = Param(nameof(AsiaEnd), TimeSpan.FromHours(6))
			.SetDisplay("Asia End", "End of Asia range (UTC)", "Time");

		_tradeStart = Param(nameof(TradeStart), TimeSpan.FromHours(6))
			.SetDisplay("Trade Start", "Start of trading window (UTC)", "Time");

		_tradeEnd = Param(nameof(TradeEnd), TimeSpan.FromHours(10))
			.SetDisplay("Trade End", "End of trading window (UTC)", "Time");

		_rewardMultiplier = Param(nameof(RewardMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Multiplier", "Reward to risk ratio", "Risk");
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
		_asiaHigh = null;
		_asiaLow = null;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		var openTime = candle.OpenTime;
		if (openTime.Date != _currentDay)
		{
			_currentDay = openTime.Date;
			_asiaHigh = null;
			_asiaLow = null;
		}

		var time = openTime.TimeOfDay;
		var inAsia = time >= AsiaStart && time < AsiaEnd;
		var inTrade = time >= TradeStart && time < TradeEnd;

		if (inAsia)
		{
			_asiaHigh = _asiaHigh.HasValue ? Math.Max(_asiaHigh.Value, candle.HighPrice) : candle.HighPrice;
			_asiaLow = _asiaLow.HasValue ? Math.Min(_asiaLow.Value, candle.LowPrice) : candle.LowPrice;
		}

		if (inTrade && _asiaHigh.HasValue && _asiaLow.HasValue)
		{
			var rangeSize = _asiaHigh.Value - _asiaLow.Value;
			var risk = rangeSize;
			var reward = rangeSize * RewardMultiplier;

			if (Position <= 0 && candle.ClosePrice > _asiaHigh.Value)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice - risk;
				_targetPrice = candle.ClosePrice + reward;
			}
			else if (Position >= 0 && candle.ClosePrice < _asiaLow.Value)
			{
				SellMarket(Volume + Math.Abs(Position));
				_stopPrice = candle.ClosePrice + risk;
				_targetPrice = candle.ClosePrice - reward;
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}

