using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar-cost averaging strategy using Bollinger Bands mean reversion.
/// Buys a fixed amount when price crosses below the lower band or on the first day of each month.
/// All positions are closed on the specified close date.
/// </summary>
public class DcaMeanReversionBollingerBandStrategy : Strategy
{
	private readonly StrategyParam<decimal> _investmentAmount;
	private readonly StrategyParam<DateTime> _openDate;
	private readonly StrategyParam<DateTime> _closeDate;
	private readonly StrategyParam<StrategyModes> _strategyMode;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _prevClose;
	private decimal _prevLower;
	private int _prevMonth;
	private bool _initialized;
	
	public decimal InvestmentAmount { get => _investmentAmount.Value; set => _investmentAmount.Value = value; }
	public DateTime OpenDate { get => _openDate.Value; set => _openDate.Value = value; }
	public DateTime CloseDate { get => _closeDate.Value; set => _closeDate.Value = value; }
	public StrategyModes StrategyMode { get => _strategyMode.Value; set => _strategyMode.Value = value; }
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public enum StrategyModes
	{
		BbMeanReversion,
		MonthlyDca,
		Combined
	}
	
	public DcaMeanReversionBollingerBandStrategy()
	{
		_investmentAmount = Param(nameof(InvestmentAmount), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Investment Amount", "Amount invested each buy", "General");
		
		_openDate = Param(nameof(OpenDate), new DateTime(2018, 1, 1))
		.SetDisplay("Open Date", "Start date for DCA", "General");
		
		_closeDate = Param(nameof(CloseDate), new DateTime(2025, 1, 2))
		.SetDisplay("Close Date", "Date to close all positions", "General");
		
		_strategyMode = Param(nameof(StrategyMode), StrategyModes.BbMeanReversion)
		.SetDisplay("Strategy Mode", "BB mean reversion, Monthly DCA or Combined", "General");
		
		_bollingerPeriod = Param(nameof(BollingerPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger");
		
		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("BB Multiplier", "Standard deviation multiplier", "Bollinger");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for Bollinger", "General");
	}
	
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
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(bollinger, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevLower = lower;
			_prevMonth = candle.OpenTime.Month;
			_initialized = true;
			return;
		}
		
		bool buyConditionBb = _prevClose >= _prevLower && candle.ClosePrice < lower;
		bool isFirstDay = candle.OpenTime.Month != _prevMonth && candle.OpenTime.Day == 1;
		
		bool buyCondition = StrategyMode switch
		{
			StrategyModes.BbMeanReversion => buyConditionBb,
			StrategyModes.MonthlyDca => isFirstDay,
			StrategyModes.Combined => buyConditionBb || isFirstDay,
			_ => false
		};
		
		var candleTime = candle.OpenTime.DateTime;
		
		if (buyCondition && candleTime >= OpenDate && candleTime <= CloseDate)
		{
			var quantity = InvestmentAmount / candle.ClosePrice;
			BuyMarket(quantity);
		}
		
		if (candleTime >= CloseDate && Position != 0)
		ClosePosition();
		
		_prevClose = candle.ClosePrice;
		_prevLower = lower;
		_prevMonth = candle.OpenTime.Month;
	}
}
