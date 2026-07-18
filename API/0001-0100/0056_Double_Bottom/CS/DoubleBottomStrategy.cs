using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Bottom reversal strategy.
/// Detects two similar bottoms and enters long on confirmation.
/// Uses SMA for exit signal.
/// </summary>
public class DoubleBottomStrategy : Strategy
{
	private readonly StrategyParam<int> _distanceParam;
	private readonly StrategyParam<decimal> _similarityPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _recentLow;
	private decimal _prevLow;
	private int _barsSinceLow;
	private int _cooldown;

	/// <summary>
	/// Distance between bottoms in bars.
	/// </summary>
	public int Distance
	{
		get => _distanceParam.Value;
		set => _distanceParam.Value = value;
	}

	/// <summary>
	/// Maximum percent difference between two bottoms.
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
	/// Initializes a new instance of <see cref="DoubleBottomStrategy"/>.
	/// </summary>
	public DoubleBottomStrategy()
	{
		_distanceParam = Param(nameof(Distance), 20)
			.SetRange(3, 100)
			.SetDisplay("Distance", "Bars between bottoms", "Pattern");

		_similarityPercent = Param(nameof(SimilarityPercent), 1.0m)
			.SetRange(0.1m, 5.0m)
			.SetDisplay("Similarity %", "Max % diff between bottoms", "Pattern");

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
		_recentLow = default;
		_prevLow = default;
		_barsSinceLow = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_recentLow = 0;
		_prevLow = 0;
		_barsSinceLow = 0;
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
			TrackLows(candle);
			return;
		}

		// Track new lows
		if (_recentLow == 0 || candle.LowPrice < _recentLow)
		{
			if (_recentLow > 0)
				_prevLow = _recentLow;

			_recentLow = candle.LowPrice;
			_barsSinceLow = 0;
		}
		else
		{
			_barsSinceLow++;
		}

		if (Position == 0 && _prevLow > 0 && _barsSinceLow >= Distance)
		{
			var priceDiff = Math.Abs((_recentLow - _prevLow) / _prevLow * 100);

			if (priceDiff <= SimilarityPercent && candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
				_recentLow = 0;
				_prevLow = 0;
			}
			else if (priceDiff <= SimilarityPercent && candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
				_recentLow = 0;
				_prevLow = 0;
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

	private void TrackLows(ICandleMessage candle)
	{
		if (_recentLow == 0 || candle.LowPrice < _recentLow)
		{
			if (_recentLow > 0)
				_prevLow = _recentLow;

			_recentLow = candle.LowPrice;
			_barsSinceLow = 0;
		}
		else
		{
			_barsSinceLow++;
		}
	}
}
