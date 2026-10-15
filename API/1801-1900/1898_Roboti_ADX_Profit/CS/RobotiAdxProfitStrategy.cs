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
/// Strategy that trades when DMI's +DI crosses above -DI and vice versa.
/// </summary>
public class RobotiAdxProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<int> _cooldownBars;

	private DirectionalIndex _dmi = null!;
	private decimal _prevPlus;
	private decimal _prevMinus;
	private int _barsSinceTrade;

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
	/// Minimum number of bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RobotiAdxProfitStrategy"/>.
	/// </summary>
	public RobotiAdxProfitStrategy()
	{
		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetDisplay("DMI Period", "Period for Directional Movement Index", "Indicators")
			
			.SetOptimize(10, 30, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type and timeframe of candles", "General");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 1m)
			.SetDisplay("Trailing Stop %", "Trailing stop as percent", "Risk Management")
			.SetGreaterThanZero()
			
			.SetOptimize(0.5m, 5m, 0.5m);

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Risk Management");
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
		_prevPlus = 0m;
		_prevMinus = 0m;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		StartProtection(
			stopLoss: new Unit(TrailingStopPercent, UnitTypes.Percent),
			takeProfit: null,
			isStopTrailing: true
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dmi = (DirectionalIndexValue)dmiValue;

		if (dmi.Plus is not decimal plus || dmi.Minus is not decimal minus)
			return;

		_barsSinceTrade++;

		var buySignal = _prevPlus <= _prevMinus && plus > minus;
		var sellSignal = _prevPlus >= _prevMinus && minus > plus;

		if (buySignal && Position <= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_barsSinceTrade = 0;
		}
		else if (sellSignal && Position >= 0 && _barsSinceTrade >= CooldownBars)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_barsSinceTrade = 0;
		}

		_prevPlus = plus;
		_prevMinus = minus;
	}
}
