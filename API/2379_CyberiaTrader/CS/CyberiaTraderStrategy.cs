using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified conversion of the CyberiaTrader MQL5 strategy.
/// Combines MACD, MA, CCI, and ADX filters to determine market direction.
/// </summary>
public class CyberiaTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<bool> _enableMacd;
	private readonly StrategyParam<bool> _enableMa;
	private readonly StrategyParam<bool> _enableCci;
	private readonly StrategyParam<bool> _enableAdx;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergenceSignal _macd;
	private SimpleMovingAverage _ma;
	private CommodityChannelIndex _cci;
	private AverageDirectionalIndex _adx;
	
	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	
	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	
	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	
	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	
	/// <summary>
	/// Commodity Channel Index length.
	/// </summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	
	/// <summary>
	/// ADX calculation length.
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	
	/// <summary>
	/// Enable MACD filter.
	/// </summary>
	public bool EnableMacd { get => _enableMacd.Value; set => _enableMacd.Value = value; }
	
	/// <summary>
	/// Enable moving average filter.
	/// </summary>
	public bool EnableMa { get => _enableMa.Value; set => _enableMa.Value = value; }
	
	/// <summary>
	/// Enable CCI filter.
	/// </summary>
	public bool EnableCci { get => _enableCci.Value; set => _enableCci.Value = value; }
	
	/// <summary>
	/// Enable ADX filter.
	/// </summary>
	public bool EnableAdx { get => _enableAdx.Value; set => _enableAdx.Value = value; }
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of <see cref="CyberiaTraderStrategy"/>.
	/// </summary>
	public CyberiaTraderStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators");
		
		_macdSlow = Param(nameof(MacdSlow), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators");
		
		_macdSignal = Param(nameof(MacdSignal), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal Period", "Signal MA period for MACD", "Indicators");
		
		_maPeriod = Param(nameof(MaPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of moving average", "Indicators");
		
		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Length of CCI", "Indicators");
		
		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Length of ADX", "Indicators");
		
		_enableMacd = Param(nameof(EnableMacd), true)
		.SetDisplay("Enable MACD", "Use MACD direction filter", "Logic");
		
		_enableMa = Param(nameof(EnableMa), true)
		.SetDisplay("Enable MA", "Use moving average trend filter", "Logic");
		
		_enableCci = Param(nameof(EnableCci), true)
		.SetDisplay("Enable CCI", "Use CCI overbought/oversold filter", "Logic");
		
		_enableAdx = Param(nameof(EnableAdx), true)
		.SetDisplay("Enable ADX", "Use ADX directional filter", "Logic");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");
		
		Volume = 1;
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
		
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		
		_ma = new SimpleMovingAverage { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _ma, _cci, _adx, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
			
			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _macd);
				DrawIndicator(indicatorArea, _cci);
				DrawIndicator(indicatorArea, _adx);
			}
		}
		
		StartProtection(new Unit(2m, UnitTypes.Percent), new Unit(1m, UnitTypes.Percent));
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdVal, IIndicatorValue maVal, IIndicatorValue cciVal, IIndicatorValue adxVal)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		
		var disableBuy = false;
		var disableSell = false;
		
		if (EnableMacd)
		{
			var macd = macdTyped.Macd;
			var signal = macdTyped.Signal;
			if (macd > signal)
			disableSell = true;
			else if (macd < signal)
			disableBuy = true;
		}
		
		if (EnableMa)
		{
			var ma = maVal.ToDecimal();
			if (candle.ClosePrice > ma)
			disableSell = true;
			else if (candle.ClosePrice < ma)
			disableBuy = true;
		}
		
		if (EnableCci)
		{
			var cci = cciVal.ToDecimal();
			if (cci > 100m)
			disableBuy = true;
			else if (cci < -100m)
			disableSell = true;
		}
		
		if (EnableAdx)
		{
			var plus = adxTyped.Dx.Plus;
			var minus = adxTyped.Dx.Minus;
			if (plus > minus)
			disableSell = true;
			else if (minus > plus)
			disableBuy = true;
		}
		
		if (!disableBuy && disableSell && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!disableSell && disableBuy && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
