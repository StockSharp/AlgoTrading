using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PSAR Trader strategy uses Parabolic SAR indicator to trade on trend reversals.
/// It opens long when price crosses above SAR and short when price crosses below.
/// Time filter restricts trading hours and optional parameter allows closing position on opposite signal.
/// Stop-loss and take-profit levels are applied via StartProtection.
/// </summary>
public class PsarTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _step;
	private readonly StrategyParam<decimal> _maximum;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevSar;
	private decimal _prevPrice;
	private bool _isFirst = true;
	public decimal Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}
	public decimal Maximum
	{
		get => _maximum.Value;
		set => _maximum.Value = value;
	}
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}
	public bool CloseOnOpposite
	{
		get => _closeOnOpposite.Value;
		set => _closeOnOpposite.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public PsarTraderStrategy()
	{
		_step = Param(nameof(Step), 0.001m)
			.SetDisplay("SAR Step", "Acceleration factor step for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);
		_maximum = Param(nameof(Maximum), 0.2m)
			.SetDisplay("SAR Maximum", "Maximum acceleration factor for Parabolic SAR", "Indicators")
			.SetCanOptimize(true);
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetDisplay("Take Profit (ticks)", "Take profit in ticks", "Risk")
			.SetCanOptimize(true);
		_stopLossTicks = Param(nameof(StopLossTicks), 50)
			.SetDisplay("Stop Loss (ticks)", "Stop loss in ticks", "Risk")
			.SetCanOptimize(true);
		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Hour of day to start trading", "General");
		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Hour of day to stop trading", "General");
		_closeOnOpposite = Param(nameof(CloseOnOpposite), true)
			.SetDisplay("Close On Opposite", "Close current position on opposite signal", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevSar = default;
		_prevPrice = default;
		_isFirst = true;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		// Create Parabolic SAR indicator
		var psar = new ParabolicSar
		{
			Acceleration = Step,
			AccelerationMax = Maximum
		};
		var subscription = SubscribeCandles(CandleType);
		// Subscribe to candles and bind indicator
		subscription
				.Bind(psar, ProcessCandle)
				.Start();
		var step = Security.PriceStep ?? 1m;
		// Enable stop-loss and take-profit protection
		StartProtection(
				takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Point),
				stopLoss: new Unit(StopLossTicks * step, UnitTypes.Point));
		var area = CreateChartArea();
		if (area != null)
		{
				DrawCandles(area, subscription);
				DrawIndicator(area, psar);
				DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal sarValue)
		// Handle PSAR crossover on finished candles
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;
		// Trade only within specified hours
		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour > EndHour)
			return;
		// Ensure strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		// Initialize previous values on first call
		if (_isFirst)
		{
			_prevSar = sarValue;
			_prevPrice = candle.ClosePrice;
			_isFirst = false;
			return;
		}
		// Determine previous and current SAR relation to price
		var prevAbove = _prevPrice > _prevSar;
		var currAbove = candle.ClosePrice > sarValue;
		// PSAR switched below price -> buy signal
		if (currAbove && !prevAbove && Position <= 0)
		{
			if (CloseOnOpposite && Position < 0)
				ClosePosition();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		// PSAR switched above price -> sell signal
		else if (!currAbove && prevAbove && Position >= 0)
		{
			if (CloseOnOpposite && Position > 0)
				ClosePosition();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		_prevSar = sarValue;
		_prevPrice = candle.ClosePrice;
	}
}
