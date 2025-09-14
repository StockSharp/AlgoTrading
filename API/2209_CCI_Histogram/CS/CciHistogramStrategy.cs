using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Histogram reversal strategy.
/// Buys when CCI leaves the upper extreme and sells when it leaves the lower extreme.
/// Optional stop loss and take profit protection in points.
/// </summary>
public class CciHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevCci;

	/// <summary>
	/// CCI period length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI level that defines overbought zone.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower CCI level that defines oversold zone.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Use stop loss protection.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Use take profit protection.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CciHistogramStrategy"/> class.
	/// </summary>
	public CciHistogramStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("CCI Period", "Period length for the CCI indicator", "General")
						 .SetCanOptimize(true)
						 .SetOptimize(7, 28, 7);

		_upperLevel = Param(nameof(UpperLevel), 100m)
						  .SetDisplay("Upper Level", "Upper CCI level", "General")
						  .SetCanOptimize(true)
						  .SetOptimize(80m, 120m, 10m);

		_lowerLevel = Param(nameof(LowerLevel), -100m)
						  .SetDisplay("Lower Level", "Lower CCI level", "General")
						  .SetCanOptimize(true)
						  .SetOptimize(-120m, -80m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
							  .SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
							  .SetCanOptimize(true)
							  .SetOptimize(50m, 200m, 25m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
								.SetDisplay("Take Profit", "Take profit in points", "Risk Management")
								.SetCanOptimize(true)
								.SetOptimize(50m, 300m, 25m);

		_useStopLoss = Param(nameof(UseStopLoss), false)
						   .SetDisplay("Enable Stop Loss", "Use stop loss protection", "Risk Management");

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
							 .SetDisplay("Enable Take Profit", "Use take profit protection", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevCci = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(takeProfit: UseTakeProfit ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null,
						stopLoss: UseStopLoss ? new Unit(StopLossPoints, UnitTypes.Absolute) : null,
						isStopTrailing: false, useMarketOrders: true);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var subscription = SubscribeCandles(CandleType);

		var isInitialized = false;

		subscription
			.Bind(cci,
				  (candle, cciValue) =>
				  {
					  if (candle.State != CandleStates.Finished)
						  return;

					  if (!IsFormedAndOnlineAndAllowTrading())
						  return;

					  if (!isInitialized)
					  {
						  _prevCci = cciValue;
						  isInitialized = true;
						  return;
					  }

					  if (_prevCci > UpperLevel && cciValue <= UpperLevel && Position <= 0)
					  {
						  BuyMarket(Volume + Math.Abs(Position));
					  }
					  else if (_prevCci < LowerLevel && cciValue >= LowerLevel && Position >= 0)
					  {
						  SellMarket(Volume + Math.Abs(Position));
					  }

					  _prevCci = cciValue;
				  })
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}
}
