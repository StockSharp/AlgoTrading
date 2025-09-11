using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doji candle strategy with risk-reward based protection.
/// </summary>
public class EnhancedDojiCandleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevBullishConfirm;
	private bool _prevBearishConfirm;

	/// <summary>
	/// Risk-reward multiplier for take-profit.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
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
	/// Constructor.
	/// </summary>
	public EnhancedDojiCandleStrategy()
	{
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk-Reward Ratio", "Reward to risk multiplier", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss in pips", "Risk");

		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period of SMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 1m;
		var stopDiff = StopLossPips * step;
		var takeDiff = stopDiff * RiskRewardRatio;
		StartProtection(new Unit(takeDiff, UnitTypes.Absolute), new Unit(stopDiff, UnitTypes.Absolute));

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

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

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;
		var doji = range > 0m && body <= range * 0.3m;

		var bullishConfirm = candle.ClosePrice > candle.OpenPrice && candle.LowPrice >= candle.OpenPrice * 0.99m;
		var bearishConfirm = candle.ClosePrice < candle.OpenPrice && candle.HighPrice <= candle.OpenPrice * 1.01m;

		if (doji)
		{
			if (Position > 0)
			{
				SellMarket(Position);
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
			}
			else if (bullishConfirm || _prevBullishConfirm)
			{
				BuyMarket();
			}
			else if (bearishConfirm || _prevBearishConfirm)
			{
				SellMarket();
			}
		}

		_prevBullishConfirm = bullishConfirm;
		_prevBearishConfirm = bearishConfirm;
	}
}
