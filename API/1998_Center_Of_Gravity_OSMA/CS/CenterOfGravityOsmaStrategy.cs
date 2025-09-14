using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Center of Gravity OSMA based reversal strategy.
/// Opens long when oscillator turns up after decline.
/// Opens short when oscillator turns down after rise.
/// Supports optional stop loss and take profit levels.
/// </summary>
public class CenterOfGravityOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _smoothPeriod1;
	private readonly StrategyParam<int> _smoothPeriod2;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private CenterOfGravityOsmaIndicator _indicator = default!;
	private decimal _prev1;
	private decimal _prev2;
	private decimal _entryPrice;

	/// <summary>
	/// Indicator calculation period.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// First smoothing period.
	/// </summary>
	public int SmoothPeriod1 { get => _smoothPeriod1.Value; set => _smoothPeriod1.Value = value; }

	/// <summary>
	/// Second smoothing period.
	/// </summary>
	public int SmoothPeriod2 { get => _smoothPeriod2.Value; set => _smoothPeriod2.Value = value; }

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions on sell signal.
	/// </summary>
	public bool BuyPosClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions on buy signal.
	/// </summary>
	public bool SellPosClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="CenterOfGravityOsmaStrategy"/>.
	/// </summary>
	public CenterOfGravityOsmaStrategy()
	{
		_period = Param(nameof(Period), 10)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Base calculation period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_smoothPeriod1 = Param(nameof(SmoothPeriod1), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smooth 1", "First smoothing period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_smoothPeriod2 = Param(nameof(SmoothPeriod2), 3)
		.SetGreaterThanZero()
		.SetDisplay("Smooth 2", "Second smoothing period", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);

		_stopLoss = Param(nameof(StopLoss), 0m)
		.SetDisplay("Stop Loss", "Stop loss distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0m, 100m, 10m);

		_takeProfit = Param(nameof(TakeProfit), 0m)
		.SetDisplay("Take Profit", "Take profit distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0m, 100m, 10m);

		_buyOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Buy Close", "Allow closing longs on sell signal", "Trading");

		_sellClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Sell Close", "Allow closing shorts on buy signal", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_prev1 = 0m;
		_prev2 = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new CenterOfGravityOsmaIndicator
		{
			Period = Period,
			SmoothPeriod1 = SmoothPeriod1,
			SmoothPeriod2 = SmoothPeriod2
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_indicator, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		CheckStops(candle);

		if (!_indicator.IsFormed)
		return;

		var prev2 = _prev2;
		var prev1 = _prev1;
		_prev2 = prev1;
		_prev1 = value;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (prev2 < prev1 && value > prev1)
		{
			if (SellPosClose && Position < 0)
			BuyMarket(Math.Abs(Position));

			if (BuyPosOpen && Position <= 0)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (prev2 > prev1 && value < prev1)
		{
			if (BuyPosClose && Position > 0)
			SellMarket(Position);

			if (SellPosOpen && Position >= 0)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
			}
		}
	}

	private void CheckStops(ICandleMessage candle)
	{
		if (Position > 0 && _entryPrice > 0m)
		{
			if (StopLoss > 0m && candle.LowPrice <= _entryPrice - StopLoss)
			SellMarket(Position);

			if (TakeProfit > 0m && candle.HighPrice >= _entryPrice + TakeProfit)
			SellMarket(Position);
		}
		else if (Position < 0 && _entryPrice > 0m)
		{
			var pos = Math.Abs(Position);
			if (StopLoss > 0m && candle.HighPrice >= _entryPrice + StopLoss)
			BuyMarket(pos);

			if (TakeProfit > 0m && candle.LowPrice <= _entryPrice - TakeProfit)
			BuyMarket(pos);
		}

		if (Position == 0)
		_entryPrice = 0m;
	}

	private sealed class CenterOfGravityOsmaIndicator : BaseIndicator
	{
		public int Period { get; set; }
		public int SmoothPeriod1 { get; set; }
		public int SmoothPeriod2 { get; set; }

		private readonly SMA _sma = new();
		private readonly WeightedMovingAverage _lwma = new();
		private readonly SMA _smooth1 = new();
		private readonly SMA _smooth2 = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var smaValue = _sma.Process(input);
			var lwmaValue = _lwma.Process(input);

			if (!smaValue.IsFinal || !lwmaValue.IsFinal)
			return new DecimalIndicatorValue(this, default, input.Time);

			var res1 = smaValue.GetValue<decimal>() * lwmaValue.GetValue<decimal>();
			var smooth1Value = _smooth1.Process(new DecimalIndicatorValue(this, res1, input.Time));
			if (!smooth1Value.IsFinal)
			return new DecimalIndicatorValue(this, default, input.Time);

			var res3 = res1 - smooth1Value.GetValue<decimal>();
			var smooth2Value = _smooth2.Process(new DecimalIndicatorValue(this, res3, input.Time));
			if (!smooth2Value.IsFinal)
			return new DecimalIndicatorValue(this, default, input.Time);

			var final = smooth2Value.GetValue<decimal>();
			return new DecimalIndicatorValue(this, final, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_sma.Length = Period;
			_lwma.Length = Period;
			_smooth1.Length = SmoothPeriod1;
			_smooth2.Length = SmoothPeriod2;
			_sma.Reset();
			_lwma.Reset();
			_smooth1.Reset();
			_smooth2.Reset();
		}
	}
}
