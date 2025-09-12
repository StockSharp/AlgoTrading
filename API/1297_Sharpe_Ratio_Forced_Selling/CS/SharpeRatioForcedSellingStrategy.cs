using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sharpe Ratio Forced Selling strategy.
/// Buys when the rolling Sharpe ratio is deeply negative and
/// exits when it turns highly positive or after a maximum holding period.
/// </summary>
public class SharpeRatioForcedSellingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _entrySharpeThreshold;
	private readonly StrategyParam<decimal> _exitSharpeThreshold;
	private readonly StrategyParam<int> _maxHoldingDays;
	private readonly StrategyParam<bool> _useLogReturns;
	private readonly StrategyParam<decimal> _riskFreeRateAnnual;
	private readonly StrategyParam<int> _periodsPerYear;
	private readonly StrategyParam<DataType> _candleType;
	
	private SimpleMovingAverage _avgExcessReturn = null!;
	private StandardDeviation _stdDevExcessReturn = null!;
	
	private decimal? _prevClose;
	private bool _readyToBuy;
	private int _barSinceEntry;
	private decimal _riskFreePerPeriod;
	
	/// <summary>
	/// Rolling period for Sharpe ratio.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }
	
	/// <summary>
	/// Sharpe ratio threshold for entries.
	/// </summary>
	public decimal EntrySharpeThreshold { get => _entrySharpeThreshold.Value; set => _entrySharpeThreshold.Value = value; }
	
	/// <summary>
	/// Sharpe ratio threshold for exits.
	/// </summary>
	public decimal ExitSharpeThreshold { get => _exitSharpeThreshold.Value; set => _exitSharpeThreshold.Value = value; }
	
	/// <summary>
	/// Maximum holding period in bars.
	/// </summary>
	public int MaxHoldingDays { get => _maxHoldingDays.Value; set => _maxHoldingDays.Value = value; }
	
	/// <summary>
	/// Use logarithmic returns.
	/// </summary>
	public bool UseLogReturns { get => _useLogReturns.Value; set => _useLogReturns.Value = value; }
	
	/// <summary>
	/// Annual risk-free rate.
	/// </summary>
	public decimal RiskFreeRateAnnual { get => _riskFreeRateAnnual.Value; set => _riskFreeRateAnnual.Value = value; }
	
	/// <summary>
	/// Trading periods per year.
	/// </summary>
	public int PeriodsPerYear { get => _periodsPerYear.Value; set => _periodsPerYear.Value = value; }
	
	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="SharpeRatioForcedSellingStrategy"/> class.
	/// </summary>
	public SharpeRatioForcedSellingStrategy()
	{
		_length = Param(nameof(Length), 8)
		.SetGreaterThanZero()
		.SetDisplay("Sharpe Length", "Rolling period for Sharpe ratio", "Parameters");
		
		_entrySharpeThreshold = Param(nameof(EntrySharpeThreshold), -5m)
		.SetDisplay("Entry Sharpe", "Sharpe ratio threshold for entries", "Parameters");
		
		_exitSharpeThreshold = Param(nameof(ExitSharpeThreshold), 13m)
		.SetDisplay("Exit Sharpe", "Sharpe ratio threshold for exits", "Parameters");
		
		_maxHoldingDays = Param(nameof(MaxHoldingDays), 80)
		.SetGreaterThanZero()
		.SetDisplay("Max Holding Bars", "Maximum holding period in bars", "Parameters");
		
		_useLogReturns = Param(nameof(UseLogReturns), true)
		.SetDisplay("Use Log Returns", "Use logarithmic returns", "Parameters");
		
		_riskFreeRateAnnual = Param(nameof(RiskFreeRateAnnual), 0m)
		.SetDisplay("Risk-Free Rate (Annual)", "Annual risk-free rate", "Parameters");
		
		_periodsPerYear = Param(nameof(PeriodsPerYear), 252)
		.SetGreaterThanZero()
		.SetDisplay("Periods Per Year", "Trading periods per year", "Parameters");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		
		_avgExcessReturn = null!;
		_stdDevExcessReturn = null!;
		_prevClose = null;
		_barSinceEntry = -1;
		_riskFreePerPeriod = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_avgExcessReturn = new SimpleMovingAverage { Length = Length };
		_stdDevExcessReturn = new StandardDeviation { Length = Length };
		
		_riskFreePerPeriod = (decimal)Math.Pow((double)(1m + RiskFreeRateAnnual), 1d / PeriodsPerYear) - 1m;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished) return;
		
		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}
		
		var close = candle.ClosePrice;
		var ret = UseLogReturns
		? (decimal)Math.Log((double)(close / _prevClose.Value))
		: close / _prevClose.Value - 1m;
		
		_prevClose = close;
		
		var excess = ret - _riskFreePerPeriod;
		
		var avg = _avgExcessReturn.Process(excess, candle.OpenTime, true).ToDecimal();
		var std = _stdDevExcessReturn.Process(excess, candle.OpenTime, true).ToDecimal();
		
		if (!_avgExcessReturn.IsFormed || !_stdDevExcessReturn.IsFormed || std == 0m || !IsFormedAndOnlineAndAllowTrading()) return;
		
		var sharpe = avg * PeriodsPerYear / (std * (decimal)Math.Sqrt(PeriodsPerYear));
		
		if (Position == 0)
		{
			_barSinceEntry = -1;
		}
		else
		{
			_barSinceEntry++;
		}
		
		var entryCondition = sharpe < EntrySharpeThreshold && _readyToBuy;
		var exitCondition = sharpe > ExitSharpeThreshold;
		
		if (Position == 0 && entryCondition)
		{
			BuyMarket();
			_barSinceEntry = 0;
			_readyToBuy = false;
		}
		else if (Position > 0 && (exitCondition || _barSinceEntry >= MaxHoldingDays))
		{
			SellMarket();
		}
		
		if (exitCondition) _readyToBuy = true;
	}
}
