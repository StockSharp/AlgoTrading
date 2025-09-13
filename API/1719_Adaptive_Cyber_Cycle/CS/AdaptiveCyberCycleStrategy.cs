using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Adaptive Cyber Cycle indicator.
/// The indicator measures a smoothed price cycle and uses the previous value as a trigger line.
/// </summary>
public class AdaptiveCyberCycleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _alpha;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Indicator ratio controlling responsiveness.
	/// </summary>
	public decimal Alpha { get => _alpha.Value; set => _alpha.Value = value; }

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="AdaptiveCyberCycleStrategy"/>.
	/// </summary>
	public AdaptiveCyberCycleStrategy()
	{
		_alpha = Param(nameof(Alpha), 0.07m)
			.SetDisplay("Alpha", "Indicator ratio", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.2m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private decimal? _prevCycle;

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var acc = new AdaptiveCyberCycleIndicator { Alpha = Alpha };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(acc, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, acc);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cycle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevCycle is null)
		{
			_prevCycle = cycle;
			return;
		}

		if (cycle > _prevCycle && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (cycle < _prevCycle && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevCycle = cycle;
	}
}

/// <summary>
/// Adaptive Cyber Cycle indicator calculating a smoothed price cycle.
/// The trigger line is the previous cycle value.
/// </summary>
public class AdaptiveCyberCycleIndicator : Indicator<decimal>
{
	/// <summary>
	/// Indicator ratio controlling responsiveness.
	/// </summary>
	public decimal Alpha { get; set; } = 0.07m;

	private decimal _k0;
	private decimal _k1;
	private decimal _k2;
	private decimal _k3;

	private decimal _price1;
	private decimal _price2;
	private decimal _price3;

	private decimal _smooth1;
	private decimal _smooth2;
	private decimal _smooth3;

	private decimal _cycle1;
	private decimal _cycle2;

	private int _count;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_k0 = (decimal)Math.Pow((double)(1m - 0.5m * Alpha), 2);
		_k1 = 2m;
		_k2 = _k1 * (1m - Alpha);
		_k3 = (decimal)Math.Pow((double)(1m - Alpha), 2);

		_price1 = _price2 = _price3 = 0m;
		_smooth1 = _smooth2 = _smooth3 = 0m;
		_cycle1 = _cycle2 = 0m;
		_count = 0;
		IsFormed = false;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.GetValue<ICandleMessage>();
		var price0 = (candle.HighPrice + candle.LowPrice) / 2m;
		var smooth0 = (price0 + 2m * _price1 + 2m * _price2 + _price3) / 6m;

		decimal cycle;

		if (_count > 0)
			cycle = _k0 * (smooth0 - _k1 * _smooth1 + _smooth2) + _k2 * _cycle1 - _k3 * _cycle2;
		else
			cycle = (price0 - 2m * _price1 + _price2) / 4m;

		_price3 = _price2;
		_price2 = _price1;
		_price1 = price0;

		_smooth3 = _smooth2;
		_smooth2 = _smooth1;
		_smooth1 = smooth0;

		_cycle2 = _cycle1;
		_cycle1 = cycle;

		_count++;
		IsFormed = _count > 5;

		return new DecimalIndicatorValue(this, cycle, input.Time);
	}
}

