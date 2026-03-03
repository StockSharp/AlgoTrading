using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Top reversal strategy.
/// Detects two similar tops and enters short on confirmation.
/// Uses SMA for exit signal.
/// </summary>
public class DoubleTopStrategy : Strategy
{
	private readonly StrategyParam<int> _distanceParam;
	private readonly StrategyParam<decimal> _similarityPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _recentHigh;
	private decimal _prevHigh;
	private int _barsSinceHigh;
	private int _cooldown;

	/// <summary>
	/// Distance between tops in bars.
	/// </summary>
	public int Distance
	{
		get => _distanceParam.Value;
		set => _distanceParam.Value = value;
	}

	/// <summary>
	/// Maximum percent difference between two tops.
	/// </summary>
	public decimal SimilarityPercent
	{
		get => _similarityPercent.Value;
		set => _similarityPercent.Value = value;
	}

	/// <summary>
	/// MA Period for exit.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
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
	/// Initializes a new instance of <see cref="DoubleTopStrategy"/>.
	/// </summary>
	public DoubleTopStrategy()
	{
		_distanceParam = Param(nameof(Distance), 20)
			.SetRange(3, 100)
			.SetDisplay("Distance", "Bars between tops", "Pattern");

		_similarityPercent = Param(nameof(SimilarityPercent), 1.0m)
			.SetRange(0.1m, 5.0m)
			.SetDisplay("Similarity %", "Max % diff between tops", "Pattern");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for exit SMA", "Indicators");

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
		_recentHigh = default;
		_prevHigh = default;
		_barsSinceHigh = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_recentHigh = 0;
		_prevHigh = 0;
		_barsSinceHigh = 0;
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
			TrackHighs(candle);
			return;
		}

		// Track new highs
		if (_recentHigh == 0 || candle.HighPrice > _recentHigh)
		{
			if (_recentHigh > 0)
				_prevHigh = _recentHigh;

			_recentHigh = candle.HighPrice;
			_barsSinceHigh = 0;
		}
		else
		{
			_barsSinceHigh++;
		}

		if (Position == 0 && _prevHigh > 0 && _barsSinceHigh >= Distance)
		{
			var priceDiff = Math.Abs((_recentHigh - _prevHigh) / _prevHigh * 100);

			if (priceDiff <= SimilarityPercent && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_recentHigh = 0;
				_prevHigh = 0;
			}
			else if (priceDiff <= SimilarityPercent && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_recentHigh = 0;
				_prevHigh = 0;
			}
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

	private void TrackHighs(ICandleMessage candle)
	{
		if (_recentHigh == 0 || candle.HighPrice > _recentHigh)
		{
			if (_recentHigh > 0)
				_prevHigh = _recentHigh;

			_recentHigh = candle.HighPrice;
			_barsSinceHigh = 0;
		}
		else
		{
			_barsSinceHigh++;
		}
	}
}
