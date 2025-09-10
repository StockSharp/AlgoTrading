using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator with ATR-based risk management.
/// </summary>
public class AtrGodStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<DataType> _candleType;
	
	private bool _prevIsPriceAboveSupertrend;
	private decimal _prevSupertrendValue;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	
	/// <summary>
	/// ATR period for Supertrend calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}
	
	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}
	
	/// <summary>
	/// Risk to reward ratio for take profit.
	/// </summary>
	public decimal RewardRiskRatio
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public AtrGodStrategy()
	{
		_period = Param(nameof(Period), 10)
		.SetDisplay("Period", "ATR period for Supertrend", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 2);
		
		_multiplier = Param(nameof(Multiplier), 3m)
		.SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);
		
		_riskMultiplier = Param(nameof(RiskMultiplier), 4.5m)
		.SetDisplay("Risk Multiplier", "ATR multiplier for stop loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 6m, 0.5m);
		
		_rewardRisk = Param(nameof(RewardRiskRatio), 1.5m)
		.SetDisplay("RR Ratio", "Risk to reward ratio", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		
		_prevIsPriceAboveSupertrend = false;
		_prevSupertrendValue = 0m;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var atr = new AverageTrueRange { Length = Period };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
		var basicUpper = medianPrice + Multiplier * atrValue;
		var basicLower = medianPrice - Multiplier * atrValue;
		
		decimal supertrendValue;
		
		if (_prevSupertrendValue == 0m)
		{
			supertrendValue = candle.ClosePrice > medianPrice ? basicLower : basicUpper;
			_prevSupertrendValue = supertrendValue;
			_prevIsPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
			return;
		}
		
		if (_prevSupertrendValue <= candle.HighPrice)
		{
			supertrendValue = Math.Max(basicLower, _prevSupertrendValue);
		}
		else if (_prevSupertrendValue >= candle.LowPrice)
		{
			supertrendValue = Math.Min(basicUpper, _prevSupertrendValue);
		}
		else
		{
			supertrendValue = candle.ClosePrice > _prevSupertrendValue ? basicLower : basicUpper;
		}
		
		var isPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
		var crossedAbove = isPriceAboveSupertrend && !_prevIsPriceAboveSupertrend;
		var crossedBelow = !isPriceAboveSupertrend && _prevIsPriceAboveSupertrend;
		
		if (crossedAbove && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atrValue * RiskMultiplier;
			_takeProfitPrice = candle.ClosePrice + atrValue * RiskMultiplier * RewardRiskRatio;
		}
		else if (crossedBelow && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atrValue * RiskMultiplier;
			_takeProfitPrice = candle.ClosePrice - atrValue * RiskMultiplier * RewardRiskRatio;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		
		_prevSupertrendValue = supertrendValue;
		_prevIsPriceAboveSupertrend = isPriceAboveSupertrend;
	}
}

