using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR GOD strategy.
/// A Supertrend flip up goes long and a flip down goes short, reversing an opposite position. Each entry fixes a stop-loss
/// RiskMultiplier ATRs from the entry close and a take-profit RewardRiskRatio times that distance on the other side.
/// </summary>
public class AtrGodStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<decimal> _rewardRiskRatio;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevIsUpTrend;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// ATR period of the Supertrend and the stops.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance relative to the stop distance.
	/// </summary>
	public decimal RewardRiskRatio
	{
		get => _rewardRiskRatio.Value;
		set => _rewardRiskRatio.Value = value;
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
	public AtrGodStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "ATR period of the Supertrend and the stops", "Supertrend");

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Supertrend ATR multiplier", "Supertrend");

		_riskMultiplier = Param(nameof(RiskMultiplier), 4.5m)
			.SetNotNegative()
			.SetDisplay("Risk Multiplier", "Stop-loss distance in ATR multiples", "Risk");

		_rewardRiskRatio = Param(nameof(RewardRiskRatio), 1.5m)
			.SetNotNegative()
			.SetDisplay("Reward/Risk", "Take-profit distance relative to the stop distance", "Risk");

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
		_prevIsUpTrend = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var superTrend = new SuperTrend { Length = Period, Multiplier = Multiplier };
		var atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!superTrendValue.IsFormed || superTrendValue is not SuperTrendIndicatorValue st || !atrValue.IsFormed)
			return;

		var isUpTrend = st.IsUpTrend;
		var prevIsUpTrend = _prevIsUpTrend;
		_prevIsUpTrend = isUpTrend;

		if (prevIsUpTrend is not bool prevUp)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var risk = RiskMultiplier * atrValue.ToDecimal();
		var reward = risk * RewardRiskRatio;

		if (!prevUp && isUpTrend && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - risk;
			_takePrice = close + reward;
		}
		else if (prevUp && !isUpTrend && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + risk;
			_takePrice = close - reward;
		}
		else if (Position != 0)
		{
			var useStop = RiskMultiplier > 0m;
			var useTake = RiskMultiplier > 0m && RewardRiskRatio > 0m;

			if (Position > 0 && ((useStop && candle.LowPrice <= _stopPrice) || (useTake && candle.HighPrice >= _takePrice)))
				SellMarket(Position);
			else if (Position < 0 && ((useStop && candle.HighPrice >= _stopPrice) || (useTake && candle.LowPrice <= _takePrice)))
				BuyMarket(-Position);
		}
	}
}
