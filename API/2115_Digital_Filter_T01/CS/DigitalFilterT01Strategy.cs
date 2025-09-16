using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the DigitalF-T01 filter crossing a trigger line.
/// Buys when the digital filter crosses below the trigger.
/// Sells when the filter crosses above the trigger.
/// Includes optional stop-loss and take-profit protection.
/// </summary>
public class DigitalFilterT01Strategy : Strategy
{
	private readonly StrategyParam<decimal> _halfChannel;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDigital;
	private decimal _prevTrigger;
	private bool _hasPrev;

	/// <summary>
	/// Half channel distance added to the trigger line.
	/// </summary>
	public decimal HalfChannel
	{
		get => _halfChannel.Value;
		set => _halfChannel.Value = value;
	}

	/// <summary>
	/// Stop-loss value in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit value in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DigitalFilterT01Strategy"/>.
	/// </summary>
	public DigitalFilterT01Strategy()
	{
		_halfChannel = Param(nameof(HalfChannel), 25m)
			.SetDisplay("Half Channel", "Half channel distance added to trigger", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetDisplay("Stop Loss", "Stop-loss value in points", "Protection");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetDisplay("Take Profit", "Take-profit value in points", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(3).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for the strategy", "General");
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
		_prevDigital = 0m;
		_prevTrigger = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new DigitalFt01Indicator { HalfChannel = HalfChannel };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, indicator);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Point),
			stopLoss: new Unit(StopLoss * step, UnitTypes.Point));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var df = (DigitalFt01Value)value;
		var digital = df.Digital;
		var trigger = df.Trigger;

		if (!_hasPrev)
		{
			_prevDigital = digital;
			_prevTrigger = trigger;
			_hasPrev = true;
			return;
		}

		var prevDigital = _prevDigital;
		var prevTrigger = _prevTrigger;

		if (prevDigital > prevTrigger && digital < trigger)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (prevDigital < prevTrigger && digital > trigger)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevDigital = digital;
		_prevTrigger = trigger;
	}
}

/// <summary>
/// DigitalF-T01 indicator producing digital filter and trigger values.
/// </summary>
public class DigitalFt01Indicator : BaseIndicator<decimal>
{
	private static readonly decimal[] _coeffs = new decimal[]
	{
		0.24470985659780m, 0.23139774006970m, 0.20613796947320m, 0.17166230340640m,
		0.13146907903600m, 0.08950387549560m, 0.04960091651250m, 0.01502270569607m,
		-0.01188033734430m, -0.02989873856137m, -0.03898967104900m, -0.04014113626390m,
		-0.03511968085800m, -0.02611613850342m, -0.01539056955666m, -0.00495353651394m,
		0.00368588764825m, 0.00963614049782m, 0.01265138888314m, 0.01307496106868m,
		0.01169702291063m, 0.00974841844086m, 0.00898900012545m, -0.00649745721156m
	};

	private readonly Queue<decimal> _prices = new();

	/// <summary>
	/// Half channel distance added to the trigger line.
	/// </summary>
	public decimal HalfChannel { get; set; } = 25m;

	/// <inheritdoc />
	public override bool IsFormed => _prices.Count >= _coeffs.Length;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DigitalFt01Value(this, input, default, default);

		_prices.Enqueue(candle.ClosePrice);
		if (_prices.Count > _coeffs.Length)
			_prices.Dequeue();

		if (_prices.Count < _coeffs.Length)
			return new DigitalFt01Value(this, input, default, default);

		var arr = _prices.ToArray();
		decimal digital = 0m;
		for (var i = 0; i < _coeffs.Length; i++)
			digital += _coeffs[i] * arr[_coeffs.Length - 1 - i];

		var prevClose = arr[^2];
		var trigger = digital >= prevClose ? prevClose + HalfChannel : prevClose - HalfChannel;

		return new DigitalFt01Value(this, input, digital, trigger);
	}
}

/// <summary>
/// Indicator value for <see cref="DigitalFt01Indicator"/>.
/// </summary>
public class DigitalFt01Value : ComplexIndicatorValue
{
	public DigitalFt01Value(IIndicator indicator, IIndicatorValue input, decimal digital, decimal trigger)
		: base(indicator, input, (nameof(Digital), digital), (nameof(Trigger), trigger))
	{
	}

	/// <summary>
	/// Digital filter value.
	/// </summary>
	public decimal Digital => (decimal)GetValue(nameof(Digital));

	/// <summary>
	/// Trigger line value.
	/// </summary>
	public decimal Trigger => (decimal)GetValue(nameof(Trigger));
}
