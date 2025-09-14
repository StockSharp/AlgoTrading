namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on DecEMA indicator slope.
/// Buys when the indicator turns upward and sells when it turns downward.
/// </summary>
public class DecEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prev;
	private decimal _prevPrev;
	private bool _hasPrev;
	private bool _hasPrevPrev;

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DecEmaStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 3)
			.SetDisplay("Base EMA Period", "Length for initial EMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_length = Param(nameof(Length), 15)
			.SetDisplay("Smoothing Length", "Smoothing length for DecEMA", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Open Long", "Allow long entries", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Open Short", "Allow short entries", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevPrev)
		{
			if (_hasPrev)
			{
				_prevPrev = _prev;
				_hasPrevPrev = true;
			}

			_prev = decema;
			_hasPrev = true;
			return;
		}

		var current = decema;
		var prev = _prev;
		var prevPrev = _prevPrev;

		if (prev < prevPrev)
		{
			if (SellPosClose && Position < 0)
				ClosePosition();

			if (BuyPosOpen && current > prev && Position <= 0)
				BuyMarket();
		}
		else if (prev > prevPrev)
		{
			if (BuyPosClose && Position > 0)
				ClosePosition();

			if (SellPosOpen && current < prev && Position >= 0)
				SellMarket();
		}

		_prevPrev = prev;
		_prev = current;
	}

	private class DecemaIndicator : LengthIndicator<decimal>
	{
		public int EmaPeriod { get; set; } = 3;

		private readonly ExponentialMovingAverage _baseEma = new();
		private decimal _ema1;
		private decimal _ema2;
		private decimal _ema3;
		private decimal _ema4;
		private decimal _ema5;
		private decimal _ema6;
		private decimal _ema7;
		private decimal _ema8;
		private decimal _ema9;
		private decimal _ema10;

		public override void Reset()
		{
			base.Reset();
			_baseEma.Length = EmaPeriod;
			_baseEma.Reset();
			_ema1 = _ema2 = _ema3 = _ema4 = _ema5 = _ema6 = _ema7 = _ema8 = _ema9 = _ema10 = 0m;
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema0 = _baseEma.Process(input).GetValue<decimal>();
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

			var value = 10m * _ema1 - 45m * _ema2 + 120m * _ema3 - 210m * _ema4 + 252m * _ema5 - 210m * _ema6 + 120m * _ema7 - 45m * _ema8 + 10m * _ema9 - _ema10;
			return new DecimalIndicatorValue(this, value, input.Time);
		}
	}
}
