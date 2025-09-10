using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Exponential Profit strategy based on EMA crossover with risk-based position sizing and trailing stop.
/// </summary>
public class BitcoinExponentialProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _rewardMultiplier;
	private readonly StrategyParam<decimal> _trailOffsetPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _trailOffset;
	private decimal _highestPrice;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }

	/// <summary>
	/// Risk percent of equity.
	/// </summary>
	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }

	/// <summary>
	/// Reward to risk multiplier.
	/// </summary>
	public decimal RewardMultiplier { get => _rewardMultiplier.Value; set => _rewardMultiplier.Value = value; }

	/// <summary>
	/// Trailing stop offset percent.
	/// </summary>
	public decimal TrailOffsetPercent { get => _trailOffsetPercent.Value; set => _trailOffsetPercent.Value = value; }

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="BitcoinExponentialProfitStrategy"/>.
	/// </summary>
	public BitcoinExponentialProfitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Risk percent of equity", "Risk");

		_rewardMultiplier = Param(nameof(RewardMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Mult", "Reward-risk multiplier", "Risk");

		_trailOffsetPercent = Param(nameof(TrailOffsetPercent), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Offset %", "Trailing stop offset percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevFast = default;
		_prevSlow = default;
		_initialized = default;
		ResetTrade();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new() { Length = FastLength };
		_slowEma = new() { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized && _fastEma.IsFormed && _slowEma.IsFormed)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		if (!_initialized)
			return;

		var crossover = _prevFast <= _prevSlow && fast > slow;
		var crossunder = _prevFast >= _prevSlow && fast < slow;

		if (crossover && Position <= 0)
		{
			var entryPrice = candle.ClosePrice;
			var stopPercent = RiskPercent / 100m;
			_stopLoss = entryPrice * (1m - stopPercent);
			_takeProfit = entryPrice * (1m + RewardMultiplier * stopPercent);
			_trailOffset = entryPrice * TrailOffsetPercent / 100m;
			_highestPrice = entryPrice;
			var volume = CalculateQty(entryPrice, _stopLoss);
			if (volume > 0)
			{
				BuyMarket(volume);
				_entryPrice = entryPrice;
			}
		}
		else if (crossunder && Position > 0)
		{
			ClosePosition();
			ResetTrade();
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var trailing = _highestPrice - _trailOffset;

			if (candle.LowPrice <= _stopLoss || candle.LowPrice <= trailing || candle.HighPrice >= _takeProfit)
			{
				ClosePosition();
				ResetTrade();
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private decimal CalculateQty(decimal price, decimal stopLevel)
	{
		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskValue = equity * RiskPercent / 100m;
		var stopDist = price - stopLevel;
		return stopDist > 0m ? riskValue / stopDist : 0m;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
	}

	private void ResetTrade()
	{
		_entryPrice = default;
		_stopLoss = default;
		_takeProfit = default;
		_trailOffset = default;
		_highestPrice = default;
	}
}
