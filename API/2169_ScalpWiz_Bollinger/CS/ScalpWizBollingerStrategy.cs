using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger-based reverse scalping strategy.
/// Places counter-trend orders when price deviates from the bands.
/// Position size scales with signal strength and risk percentage.
/// </summary>
public class ScalpWizBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<decimal> _level1Pips;
	private readonly StrategyParam<decimal> _level2Pips;
	private readonly StrategyParam<decimal> _level3Pips;
	private readonly StrategyParam<decimal> _level4Pips;
	private readonly StrategyParam<int> _multiplier1;
	private readonly StrategyParam<int> _multiplier2;
	private readonly StrategyParam<int> _multiplier3;
	private readonly StrategyParam<int> _multiplier4;
	private readonly StrategyParam<int> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	public int BandsPeriod { get => _bandsPeriod.Value; set => _bandsPeriod.Value = value; }
	public decimal BandsDeviation { get => _bandsDeviation.Value; set => _bandsDeviation.Value = value; }
	public decimal Level1Pips { get => _level1Pips.Value; set => _level1Pips.Value = value; }
	public decimal Level2Pips { get => _level2Pips.Value; set => _level2Pips.Value = value; }
	public decimal Level3Pips { get => _level3Pips.Value; set => _level3Pips.Value = value; }
	public decimal Level4Pips { get => _level4Pips.Value; set => _level4Pips.Value = value; }
	public int StrengthLevel1Multiplier { get => _multiplier1.Value; set => _multiplier1.Value = value; }
	public int StrengthLevel2Multiplier { get => _multiplier2.Value; set => _multiplier2.Value = value; }
	public int StrengthLevel3Multiplier { get => _multiplier3.Value; set => _multiplier3.Value = value; }
	public int StrengthLevel4Multiplier { get => _multiplier4.Value; set => _multiplier4.Value = value; }
	public int RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public ScalpWizBollingerStrategy()
	{
		_bandsPeriod = Param(nameof(BandsPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("Bands Period", "Number of candles for Bollinger calculation", "Bollinger")
		.SetCanOptimize(true)
		.SetOptimize(20, 40, 5);
		
		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bands Deviation", "Standard deviation multiplier", "Bollinger")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);
		
		_level1Pips = Param(nameof(Level1Pips), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Level1 Pips", "Deviation from band for weakest signal", "Levels");
		
		_level2Pips = Param(nameof(Level2Pips), 120m)
		.SetGreaterThanZero()
		.SetDisplay("Level2 Pips", "Deviation from band for level 2", "Levels");
		
		_level3Pips = Param(nameof(Level3Pips), 150m)
		.SetGreaterThanZero()
		.SetDisplay("Level3 Pips", "Deviation from band for level 3", "Levels");
		
		_level4Pips = Param(nameof(Level4Pips), 200m)
		.SetGreaterThanZero()
		.SetDisplay("Level4 Pips", "Deviation from band for strongest signal", "Levels");
		
		_multiplier1 = Param(nameof(StrengthLevel1Multiplier), 1)
		.SetGreaterThanZero()
		.SetDisplay("Strength 1 Multiplier", "Volume multiplier for level 1", "Strength");
		
		_multiplier2 = Param(nameof(StrengthLevel2Multiplier), 2)
		.SetGreaterThanZero()
		.SetDisplay("Strength 2 Multiplier", "Volume multiplier for level 2", "Strength");
		
		_multiplier3 = Param(nameof(StrengthLevel3Multiplier), 3)
		.SetGreaterThanZero()
		.SetDisplay("Strength 3 Multiplier", "Volume multiplier for level 3", "Strength");
		
		_multiplier4 = Param(nameof(StrengthLevel4Multiplier), 4)
		.SetGreaterThanZero()
		.SetDisplay("Strength 4 Multiplier", "Volume multiplier for level 4", "Strength");
		
		_riskPercent = Param(nameof(RiskPercent), 2)
		.SetGreaterThanZero()
		.SetDisplay("Risk %", "Risk percentage per trade", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		
		var bollinger = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
		.BindEx(bollinger, ProcessCandle)
		.Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var bb = (BollingerBandsValue)value;
		
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
		return;
		
		var step = Security?.PriceStep ?? 0.0001m;
		var close = candle.ClosePrice;
		
		if (close - upper > Level4Pips * step)
		{
			SellByStrength(StrengthLevel4Multiplier, close);
		}
		else if (close - upper > Level3Pips * step)
		{
			SellByStrength(StrengthLevel3Multiplier, close);
		}
		else if (close - upper > Level2Pips * step)
		{
			SellByStrength(StrengthLevel2Multiplier, close);
		}
		else if (close - upper > Level1Pips * step)
		{
			SellByStrength(StrengthLevel1Multiplier, close);
		}
		else if (lower - close > Level4Pips * step)
		{
			BuyByStrength(StrengthLevel4Multiplier, close);
		}
		else if (lower - close > Level3Pips * step)
		{
			BuyByStrength(StrengthLevel3Multiplier, close);
		}
		else if (lower - close > Level2Pips * step)
		{
			BuyByStrength(StrengthLevel2Multiplier, close);
		}
		else if (lower - close > Level1Pips * step)
		{
			BuyByStrength(StrengthLevel1Multiplier, close);
		}
	}
	
	private void BuyByStrength(int strength, decimal price)
	{
		var volume = CalculateVolume(price, strength);
		if (volume <= 0)
		return;
		
		BuyMarket(volume);
	}
	
	private void SellByStrength(int strength, decimal price)
	{
		var volume = CalculateVolume(price, strength);
		if (volume <= 0)
		return;
		
		SellMarket(volume);
	}
	
	// Calculates order volume based on account balance and risk percentage
	private decimal CalculateVolume(decimal price, int strength)
	{
		var balance = Portfolio?.CurrentValue ?? 0m;
		var percentage = balance * (RiskPercent * strength / 100m);
		var lotPrice = price * 1000m;
		var volume = percentage / lotPrice;
		if (volume < 0.01m)
		volume = 0.01m;
		return Math.Round(volume, 2);
	}
}
