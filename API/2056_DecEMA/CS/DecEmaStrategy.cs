using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on DecEMA indicator slope.
/// Buys when the indicator turns upward and sells when it turns downward.
/// </summary>
public class DecEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev;
	private decimal _prevPrev;
	private int _count;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DecEmaStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Base EMA Period", "Length for initial EMA", "Parameters");

		_length = Param(nameof(Length), 15)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Smoothing length for DecEMA", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prev = default;
		_prevPrev = default;
		_count = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_count = 0;

		var decema = new DecemaIndicator
		{
			EmaPeriod = EmaPeriod,
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(decema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, decema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal decema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count <= 2)
		{
			_prevPrev = _prev;
			_prev = decema;
			return;
		}

		// Slope reversal detection
		if (_prev < _prevPrev && decema > _prev && Position <= 0)
		{
			// Was falling, now turning up -> buy
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prev > _prevPrev && decema < _prev && Position >= 0)
		{
			// Was rising, now turning down -> sell
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevPrev = _prev;
		_prev = decema;
	}

	private class DecemaIndicator : DecimalLengthIndicator
	{
		public int EmaPeriod { get; set; } = 3;

		private readonly ExponentialMovingAverage _baseEma = new();
		private decimal _ema1, _ema2, _ema3, _ema4, _ema5;
		private decimal _ema6, _ema7, _ema8, _ema9, _ema10;

		public override void Reset()
		{
			base.Reset();
			_baseEma.Length = EmaPeriod;
			_baseEma.Reset();
			_ema1 = _ema2 = _ema3 = _ema4 = _ema5 = 0m;
			_ema6 = _ema7 = _ema8 = _ema9 = _ema10 = 0m;
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema0 = _baseEma.Process(input).ToDecimal();
			var alpha = 2m / (1m + Length);

			_ema1 = alpha * ema0 + (1 - alpha) * _ema1;
			_ema2 = alpha * _ema1 + (1 - alpha) * _ema2;
			_ema3 = alpha * _ema2 + (1 - alpha) * _ema3;
			_ema4 = alpha * _ema3 + (1 - alpha) * _ema4;
			_ema5 = alpha * _ema4 + (1 - alpha) * _ema5;
			_ema6 = alpha * _ema5 + (1 - alpha) * _ema6;
			_ema7 = alpha * _ema6 + (1 - alpha) * _ema7;
			_ema8 = alpha * _ema7 + (1 - alpha) * _ema8;
			_ema9 = alpha * _ema8 + (1 - alpha) * _ema9;
			_ema10 = alpha * _ema9 + (1 - alpha) * _ema10;

			var value = 10m * _ema1 - 45m * _ema2 + 120m * _ema3 - 210m * _ema4 + 252m * _ema5
				- 210m * _ema6 + 120m * _ema7 - 45m * _ema8 + 10m * _ema9 - _ema10;

			IsFormed = _baseEma.IsFormed;

			return new DecimalIndicatorValue(this, value, input.Time);
		}
	}
}
