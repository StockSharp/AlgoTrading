namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Risk to Reward - Fixed SL Backtester strategy.
/// </summary>
public class RiskToRewardFixedSlBacktesterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _dealStartValue;
	private readonly StrategyParam<bool> _useRiskToReward;
	private readonly StrategyParam<decimal> _riskToRewardRatio;
	private readonly StrategyParam<StopMode> _stopLossType;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _pivotLookback;
	private readonly StrategyParam<decimal> _fixedTp;
	private readonly StrategyParam<decimal> _fixedSl;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenRr;
	private readonly StrategyParam<decimal> _breakEvenPercent;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private Lowest _lowest = null!;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private decimal _breakevenTarget;
	private decimal _breakevenPrice;
	private decimal _entryPrice;
	private bool _breakevenTriggered;

	public decimal DealStartValue { get => _dealStartValue.Value; set => _dealStartValue.Value = value; }
	public bool UseRiskToReward { get => _useRiskToReward.Value; set => _useRiskToReward.Value = value; }
	public decimal RiskToRewardRatio { get => _riskToRewardRatio.Value; set => _riskToRewardRatio.Value = value; }
	public StopMode StopLossType { get => _stopLossType.Value; set => _stopLossType.Value = value; }
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }
	public int PivotLookback { get => _pivotLookback.Value; set => _pivotLookback.Value = value; }
	public decimal FixedTp { get => _fixedTp.Value; set => _fixedTp.Value = value; }
	public decimal FixedSl { get => _fixedSl.Value; set => _fixedSl.Value = value; }
	public bool UseBreakEven { get => _useBreakEven.Value; set => _useBreakEven.Value = value; }
	public decimal BreakEvenRr { get => _breakEvenRr.Value; set => _breakEvenRr.Value = value; }
	public decimal BreakEvenPercent { get => _breakEvenPercent.Value; set => _breakEvenPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RiskToRewardFixedSlBacktesterStrategy()
	{
		_dealStartValue = Param(nameof(DealStartValue), 100m)
			.SetDisplay("Deal Start Value", "Value for entry trigger", "General");

		_useRiskToReward = Param(nameof(UseRiskToReward), true)
			.SetDisplay("Use Risk To Reward", "Use RR or fixed TP/SL", "Risk Management");

		_riskToRewardRatio = Param(nameof(RiskToRewardRatio), 1.5m)
			.SetDisplay("Risk To Reward Ratio", "Take profit ratio", "Risk Management");

		_stopLossType = Param(nameof(StopLossType), StopMode.Atr)
			.SetDisplay("Stop Loss Type", "ATR or Pivot", "Risk Management");

		_atrFactor = Param(nameof(AtrFactor), 1.4m)
			.SetDisplay("ATR Factor", "Multiplier for ATR stop", "Risk Management");

		_pivotLookback = Param(nameof(PivotLookback), 8)
			.SetDisplay("Pivot Lookback", "Lookback for pivot low", "Risk Management");

		_fixedTp = Param(nameof(FixedTp), 0.015m)
			.SetDisplay("Fixed TP", "Fixed take profit percent", "Risk Management");

		_fixedSl = Param(nameof(FixedSl), 0.015m)
			.SetDisplay("Fixed SL", "Fixed stop loss percent", "Risk Management");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use BreakEven", "Enable breakeven stop", "BreakEven");

		_breakEvenRr = Param(nameof(BreakEvenRr), 1m)
			.SetDisplay("BreakEven RR", "RR to move stop", "BreakEven");

		_breakEvenPercent = Param(nameof(BreakEvenPercent), 0.001m)
			.SetDisplay("BreakEven Percent", "Percent above entry", "BreakEven");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used by strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0;
		_takeProfitPrice = 0;
		_breakevenTarget = 0;
		_breakevenPrice = 0;
		_entryPrice = 0;
		_breakevenTriggered = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = 14 };
		_lowest = new Lowest { Length = PivotLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (StopLossType == StopMode.Atr && !_atr.IsFormed)
			return;
		if (StopLossType == StopMode.PivotLow && !_lowest.IsFormed)
			return;

		var dealStart = candle.ClosePrice == DealStartValue;

		var sl = UseRiskToReward
		? StopLossType switch
		{
			StopMode.Atr => candle.LowPrice - atrValue * AtrFactor,
			StopMode.PivotLow => lowestValue,
			_ => candle.LowPrice - atrValue * AtrFactor,
		}
		: candle.ClosePrice * (1m - FixedSl);

		if (dealStart && Position == 0)
		{
			_stopPrice = sl;
			_entryPrice = candle.ClosePrice;
			var diff = Math.Abs(candle.ClosePrice - _stopPrice);
			_takeProfitPrice = UseRiskToReward ? candle.ClosePrice + RiskToRewardRatio * diff : candle.ClosePrice * (1m + FixedTp);
			_breakevenTarget = candle.ClosePrice + BreakEvenRr * diff;
			_breakevenPrice = candle.ClosePrice * (1m + BreakEvenPercent);
			_breakevenTriggered = false;
			BuyMarket(Volume + Math.Abs(Position));
			return;
		}

		if (Position <= 0)
			return;

		if (UseBreakEven && !_breakevenTriggered && (candle.ClosePrice >= _breakevenTarget || candle.HighPrice >= _breakevenTarget))
		{
			_stopPrice = _breakevenPrice;
			_breakevenTriggered = true;
		}

		if (candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			return;
		}

		if (candle.HighPrice >= _takeProfitPrice)
			SellMarket(Position);
	}

	public enum StopMode
	{
		Atr,
		PivotLow
	}
}
