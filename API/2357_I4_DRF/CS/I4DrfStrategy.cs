using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the I4 DRF indicator.
/// The indicator compares changes in highs and lows and returns a value between -100 and 100.
/// Depending on <see cref="TrendMode"/> signals are interpreted with or against the trend.
/// </summary>
public class I4DrfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<Mode> _trendMode;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private I4Drf _indicator;
	private decimal _prevColor;
	private decimal _prevPrevColor;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public Mode TrendMode { get => _trendMode.Value; set => _trendMode.Value = value; }
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	public I4DrfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of candles", "General");
		_period = Param(nameof(Period), 11)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Indicator period", "Parameters");
		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Shift for signal", "Parameters");
		_trendMode = Param(nameof(TrendMode), Mode.Direct)
		.SetDisplay("Trend Mode", "Trading mode", "Parameters");
		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Open Long", "Allow opening long positions", "Switches");
		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Open Short", "Allow opening short positions", "Switches");
		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Long", "Allow closing long positions", "Switches");
		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Short", "Allow closing short positions", "Switches");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevColor = 0m;
		_prevPrevColor = 0m;
		_indicator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new I4Drf { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_indicator, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var color = value > 0m ? 1m : 0m;

		if (!_indicator.IsFormed)
		{
			_prevPrevColor = _prevColor;
			_prevColor = color;
			return;
		}

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		if (TrendMode == Mode.Direct)
		{
			if (_prevPrevColor == 1m)
			{
				if (BuyPosOpen && _prevColor < 1m)
				buyOpen = true;
				if (SellPosClose)
				sellClose = true;
			}
			if (_prevPrevColor == 0m)
			{
				if (SellPosOpen && _prevColor > 0m)
				sellOpen = true;
				if (BuyPosClose)
				buyClose = true;
			}
		}
		else
		{
			if (_prevPrevColor == 0m)
			{
				if (BuyPosOpen && _prevColor > 0m)
				buyOpen = true;
				if (SellPosClose)
				sellClose = true;
			}
			if (_prevPrevColor == 1m)
			{
				if (SellPosOpen && _prevColor < 1m)
				sellOpen = true;
				if (BuyPosClose)
				buyClose = true;
			}
		}

		if (buyClose && Position > 0)
		SellMarket(Position);
		if (sellClose && Position < 0)
		BuyMarket(-Position);
		if (buyOpen && Position <= 0)
		{
			var vol = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(vol);
		}
		if (sellOpen && Position >= 0)
		{
			var vol = Volume + (Position > 0 ? Position : 0m);
			SellMarket(vol);
		}

		_prevPrevColor = _prevColor;
		_prevColor = color;
	}

	public enum Mode
	{
		Direct,
		NotDirect
	}

	private class I4Drf : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 11;

		private readonly Queue<int> _diffs = new();
		private int _sum;
		private decimal? _prevHigh;
		private decimal? _prevLow;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (_prevHigh is null || _prevLow is null)
			{
				_prevHigh = candle.HighPrice;
				_prevLow = candle.LowPrice;
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			var diff = 0;
			if (candle.HighPrice - _prevHigh.Value > 0m)
			diff++;
			if (candle.LowPrice - _prevLow.Value < 0m)
			diff--;

			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;

			_sum += diff;
			_diffs.Enqueue(diff);
			if (_diffs.Count > Length)
			_sum -= _diffs.Dequeue();

			if (_diffs.Count < Length)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			IsFormed = true;
			var value = (decimal)_sum / Length * 100m;
			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_diffs.Clear();
			_sum = 0;
			_prevHigh = _prevLow = null;
		}
	}
}
