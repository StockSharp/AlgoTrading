using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zonal Trading strategy based on Awesome and Accelerator oscillators.
/// </summary>
public class ZonalTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Unit> _takeProfit;

	private AwesomeOscillator _ao;
	private SimpleMovingAverage _aoMa;

	private decimal _aoPrev1;
	private decimal _aoPrev2;
	private decimal _acPrev1;
	private decimal _acPrev2;
	private int _historyCount;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit value.
	/// </summary>
	public Unit TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ZonalTradingStrategy"/>.
	/// </summary>
	public ZonalTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_takeProfit = Param(nameof(TakeProfit), new Unit(5000, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Fixed take profit in price units", "Protection");
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

		_aoPrev1 = _aoPrev2 = _acPrev1 = _acPrev2 = default;
		_historyCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ao = new AwesomeOscillator();
		_aoMa = new SimpleMovingAverage { Length = 5, Input = _ao };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ao, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ao);
			DrawOwnTrades(area);
		}

		StartProtection(TakeProfit, new Unit());
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var maValue = _aoMa.Process(new DecimalIndicatorValue(_aoMa, aoValue));
		if (!maValue.IsFinal)
			return;

		var acValue = aoValue - maValue.GetValue<decimal>();

		if (_historyCount < 2)
		{
			_aoPrev2 = _aoPrev1;
			_aoPrev1 = aoValue;
			_acPrev2 = _acPrev1;
			_acPrev1 = acValue;
			_historyCount++;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var buySignal = aoValue > _aoPrev1 && acValue > _acPrev1 && (_acPrev1 < _acPrev2 || _aoPrev1 < _aoPrev2) && aoValue > 0 && acValue > 0;
		var sellSignal = aoValue < _aoPrev1 && acValue < _acPrev1 && (_acPrev1 > _acPrev2 || _aoPrev1 > _aoPrev2) && aoValue < 0 && acValue < 0;

		if (buySignal && Position <= 0)
			BuyMarket();

		if (sellSignal && Position >= 0)
			SellMarket();

		if (Position > 0 && aoValue < _aoPrev1 && acValue < _acPrev1)
			ClosePosition();

		if (Position < 0 && aoValue > _aoPrev1 && acValue > _acPrev1)
			ClosePosition();

		_aoPrev2 = _aoPrev1;
		_aoPrev1 = aoValue;
		_acPrev2 = _acPrev1;
		_acPrev1 = acValue;
	}
}
