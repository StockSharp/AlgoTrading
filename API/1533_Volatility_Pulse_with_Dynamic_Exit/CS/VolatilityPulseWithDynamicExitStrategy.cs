namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy using ATR volatility expansion with momentum confirmation and delayed exits.
/// </summary>
public class VolatilityPulseWithDynamicExitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _volThreshold;
	private readonly StrategyParam<decimal> _minVolatility;
	private readonly StrategyParam<int> _exitBars;
	private readonly StrategyParam<decimal> _riskReward;
	
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrAverage;
	private Momentum _momentum;
	
	private int _barIndex;
	private int _entryBarIndex;
	private decimal _entryPrice;
	private bool _exitOrdersPlaced;
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}
	
	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}
	
	/// <summary>
	/// Volatility expansion multiplier.
	/// </summary>
	public decimal VolThreshold
	{
		get => _volThreshold.Value;
		set => _volThreshold.Value = value;
	}
	
	/// <summary>
	/// Minimum ATR threshold.
	/// </summary>
	public decimal MinVolatility
	{
		get => _minVolatility.Value;
		set => _minVolatility.Value = value;
	}
	
	/// <summary>
	/// Maximum holding bars before placing exits.
	/// </summary>
	public int ExitBars
	{
		get => _exitBars.Value;
		set => _exitBars.Value = value;
	}
	
	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public VolatilityPulseWithDynamicExitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetRange(1, 100)
		.SetDisplay("ATR Length", "ATR calculation length", "Parameters");
		
		_momentumLength = Param(nameof(MomentumLength), 20)
		.SetRange(1, 100)
		.SetDisplay("Momentum Length", "Momentum lookback length", "Parameters");
		
		_volThreshold = Param(nameof(VolThreshold), 0.5m)
		.SetRange(0.1m, 5m)
		.SetDisplay("Volatility Threshold", "ATR expansion multiplier", "Parameters");
		
		_minVolatility = Param(nameof(MinVolatility), 1m)
		.SetRange(0.1m, 5m)
		.SetDisplay("Min Volatility", "Minimum ATR threshold", "Parameters");
		
		_exitBars = Param(nameof(ExitBars), 42)
		.SetGreaterThanZero()
		.SetDisplay("Exit Bars", "Bars before placing exits", "Risk");
		
		_riskReward = Param(nameof(RiskReward), 2m)
		.SetRange(0.5m, 5m)
		.SetDisplay("Risk Reward", "Take-profit to stop-loss ratio", "Risk");
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
		
		_barIndex = 0;
		_entryBarIndex = -1;
		_entryPrice = 0m;
		_exitOrdersPlaced = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_atr = new AverageTrueRange { Length = AtrLength };
		_atrAverage = new SimpleMovingAverage { Length = 20 };
		_momentum = new Momentum { Length = MomentumLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _momentum, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawIndicator(area, _atrAverage);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var atrBase = _atrAverage.Process(new DecimalIndicatorValue(_atrAverage, atrValue, candle.ServerTime)).ToDecimal();
		
		var volExpansion = atrValue > atrBase * VolThreshold;
		var lowVolatility = atrValue < atrBase * MinVolatility;
		var momentumUp = momentumValue > 0m;
		var momentumDown = momentumValue < 0m;
		
		if (volExpansion && momentumUp && !lowVolatility && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_entryBarIndex = _barIndex;
			_exitOrdersPlaced = false;
		}
		else if (volExpansion && momentumDown && !lowVolatility && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_entryBarIndex = _barIndex;
			_exitOrdersPlaced = false;
		}
		
		if (Position > 0 && !_exitOrdersPlaced && _entryBarIndex >= 0 && _barIndex - _entryBarIndex >= ExitBars)
		{
			var longSl = _entryPrice - atrValue;
			var longTp = _entryPrice + atrValue * RiskReward;
			var volume = Math.Abs(Position);
			
			SellStop(longSl, volume);
			SellLimit(longTp, volume);
			_exitOrdersPlaced = true;
		}
		else if (Position < 0 && !_exitOrdersPlaced && _entryBarIndex >= 0 && _barIndex - _entryBarIndex >= ExitBars)
		{
			var shortSl = _entryPrice + atrValue;
			var shortTp = _entryPrice - atrValue * RiskReward;
			var volume = Math.Abs(Position);
			
			BuyStop(shortSl, volume);
			BuyLimit(shortTp, volume);
			_exitOrdersPlaced = true;
		}
		
		if (Position == 0)
		{
			_entryBarIndex = -1;
			_exitOrdersPlaced = false;
		}
		
		_barIndex++;
	}
}
