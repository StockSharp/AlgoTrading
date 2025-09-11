using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI, CCI and Williams %R strategy with take profit and stop loss.
/// Enters long when all indicators show oversold values.
/// Enters short when all indicators show overbought values.
/// </summary>
public class RsiCciWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<decimal> _williamsOversold;
	private readonly StrategyParam<decimal> _williamsOverbought;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private CommodityChannelIndex _cci;
	private WilliamsR _williams;

	/// <summary>
	/// RSI period parameter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// CCI period parameter.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R period parameter.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// CCI oversold level.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
	}

	/// <summary>
	/// CCI overbought level.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
	}

	/// <summary>
	/// Williams %R oversold level.
	/// </summary>
	public decimal WilliamsOversold
	{
		get => _williamsOversold.Value;
		set => _williamsOversold.Value = value;
	}

	/// <summary>
	/// Williams %R overbought level.
	/// </summary>
	public decimal WilliamsOverbought
	{
		get => _williamsOverbought.Value;
		set => _williamsOverbought.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiCciWilliamsRStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI", "Indicators")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI", "Indicators")
			.SetCanOptimize(true);

		_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
			.SetCanOptimize(true);

		_rsiOversold = Param(nameof(RsiOversold), 25m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Thresholds")
			.SetCanOptimize(true);

		_rsiOverbought = Param(nameof(RsiOverbought), 75m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Thresholds")
			.SetCanOptimize(true);

		_cciOversold = Param(nameof(CciOversold), -130m)
			.SetDisplay("CCI Oversold", "CCI oversold level", "Thresholds")
			.SetCanOptimize(true);

		_cciOverbought = Param(nameof(CciOverbought), 130m)
			.SetDisplay("CCI Overbought", "CCI overbought level", "Thresholds")
			.SetCanOptimize(true);

		_williamsOversold = Param(nameof(WilliamsOversold), -85m)
			.SetDisplay("Williams %R Oversold", "Williams %R oversold level", "Thresholds")
			.SetCanOptimize(true);

		_williamsOverbought = Param(nameof(WilliamsOverbought), -15m)
			.SetDisplay("Williams %R Overbought", "Williams %R overbought level", "Thresholds")
			.SetCanOptimize(true);

		_takeProfitPct = Param(nameof(TakeProfitPct), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true);

		_stopLossPct = Param(nameof(StopLossPct), 0.45m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(45).TimeFrame())
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

		_rsi = default;
		_cci = default;
		_williams = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_williams = new WilliamsR { Length = WilliamsPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _cci, _williams, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _williams);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(TakeProfitPct / 100m, UnitTypes.Percent), new Unit(StopLossPct / 100m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal cciValue, decimal williamsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_cci.IsFormed || !_williams.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsiValue < RsiOversold && cciValue < CciOversold && williamsValue < WilliamsOversold && Position == 0)
		{
			BuyMarket();
		}
		else if (rsiValue > RsiOverbought && cciValue > CciOverbought && williamsValue > WilliamsOverbought && Position == 0)
		{
			SellMarket();
		}
	}
}
