namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades when DMI's +DI crosses above -DI and vice versa.
/// </summary>
public class RobotiAdxProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _trailingStopPercent;

	private DirectionalIndex _dmi = null!;

	/// <summary>
	/// DMI calculation period.
	/// </summary>
	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trailing stop size in percent.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RobotiAdxProfitStrategy"/>.
	/// </summary>
	public RobotiAdxProfitStrategy()
	{
		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetDisplay("DMI Period", "Period for Directional Movement Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type and timeframe of candles", "General");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1m)
			.SetDisplay("Trailing Stop %", "Trailing stop as percent", "Risk Management")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
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

		_dmi = new DirectionalIndex { Length = DmiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _dmi);
			DrawOwnTrades(area);
		}

		StartProtection(stopLoss: new Unit(TrailingStopPercent, UnitTypes.Percent), isStopTrailing: true);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dmi = (DirectionalIndexValue)dmiValue;

		if (dmi.Plus is not decimal plus || dmi.Minus is not decimal minus)
			return;

		if (plus > minus && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (minus > plus && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
