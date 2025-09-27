using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Commodity Channel Index breakout strategy converted from the FT_CCI MetaTrader expert.
/// Opens long positions when CCI drops below the lower band and shorts when it rises above the upper band.
/// Optional stop-loss and take-profit distances are expressed in pips and converted into price offsets automatically.
/// </summary>
public class FtCciStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private decimal _pipSize;

	/// <summary>
	/// Trade volume expressed in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Averaging period for the CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI level that triggers short entries.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Lower CCI level that triggers long entries.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Use zero to disable protection.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Use zero to disable protection.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FtCciStrategy"/> class.
	/// </summary>
	public FtCciStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Lot size used for entries", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetDisplay("CCI Period", "Averaging period for the CCI indicator", "Indicator")
			.SetRange(5, 100)
			.SetCanOptimize(true);

		_upperThreshold = Param(nameof(UpperThreshold), 210m)
			.SetDisplay("CCI Upper Threshold", "CCI level that triggers short entries", "Indicator")
			.SetRange(100m, 350m)
			.SetCanOptimize(true);

		_lowerThreshold = Param(nameof(LowerThreshold), -210m)
			.SetDisplay("CCI Lower Threshold", "CCI level that triggers long entries", "Indicator")
			.SetRange(-350m, -100m)
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk")
			.SetRange(0m, 500m)
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
			.SetRange(0m, 500m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
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

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Synchronize the Strategy volume with the configured parameter.
		Volume = TradeVolume;

		_pipSize = CalculatePipSize();

		Unit stopLossUnit = null;
		if (StopLossPips > 0m && _pipSize > 0m)
			stopLossUnit = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);

		Unit takeProfitUnit = null;
		if (TakeProfitPips > 0m && _pipSize > 0m)
			takeProfitUnit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);

		// Enable built-in protection once to emulate MetaTrader stop-loss and take-profit behaviour.
		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(
				takeProfit: takeProfitUnit,
				stopLoss: stopLossUnit,
				isStopTrailing: false,
				useMarketOrders: true);
		}

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cci == null || !_cci.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Enter or reverse to a long position when the CCI pierces the lower band.
		if (cciValue <= LowerThreshold && Position <= 0)
		{
			BuyMarket(TradeVolume + Math.Abs(Position));
			return;
		}

		// Enter or reverse to a short position when the CCI pierces the upper band.
		if (cciValue >= UpperThreshold && Position >= 0)
			SellMarket(TradeVolume + Math.Abs(Position));
	}

	private decimal CalculatePipSize()
	{
		if (Security == null)
			return 0m;

		var step = Security.PriceStep ?? Security.Step ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
