using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the ICAi adaptive moving average indicator.
/// Opens long or short positions when the indicator changes direction.
/// </summary>
public class ICaiStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private ICaiIndicator _icai;
	private readonly List<decimal> _values = new();

	/// <summary>
	/// Smoothing length for the ICAi indicator.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit size in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss size in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ICaiStrategy()
	{
		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Indicator smoothing length", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Profit target in price units", "Protection");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Loss limit in price units", "Protection");
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

		_icai = new ICaiIndicator { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_icai, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _icai);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Price),
			stopLoss: new Unit(StopLoss, UnitTypes.Price));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue icaiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!icaiValue.IsFinal)
			return;

		var value = icaiValue.GetValue<decimal>();

		_values.Insert(0, value);
		if (_values.Count > 3)
			_values.RemoveAt(_values.Count - 1);

		if (_values.Count < 3 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var current = _values[0];
		var previous = _values[1];
		var prior = _values[2];

		if (previous < prior)
		{
			if (current >= previous && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (previous > prior)
		{
			if (current <= previous && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (Position > 0)
				SellMarket(Math.Abs(Position));
		}
	}

	private class ICaiIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 12;

		private SimpleMovingAverage _ma;
		private StandardDeviation _std;
		private decimal? _prev;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var price = candle.ClosePrice;

			_ma ??= new SimpleMovingAverage { Length = Length };
			_std ??= new StandardDeviation { Length = Length };

			var time = input.Time;

			var maVal = _ma.Process(new DecimalIndicatorValue(_ma, price, time)).GetValue<decimal>();
			var stdVal = _std.Process(new DecimalIndicatorValue(_std, price, time)).GetValue<decimal>();

			var prev = _prev ?? maVal;
			var diff = prev - maVal;
			var powDxma = diff * diff;
			var powStd = stdVal * stdVal;

			decimal koeff = 0m;
			if (powDxma >= powStd && powDxma != 0m)
				koeff = 1m - powStd / powDxma;

			var result = prev + koeff * (maVal - prev);
			_prev = result;

			IsFormed = _ma.IsFormed && _std.IsFormed;
			return new DecimalIndicatorValue(this, result, time);
		}

		public override void Reset()
		{
			base.Reset();
			_ma = null;
			_std = null;
			_prev = null;
		}
	}
}

