
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color 3rd Generation XMA strategy.
/// Opens positions when the third generation moving average changes direction
/// and triggers only at the specified opening time.
/// </summary>
public class Color3rdGenXmaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _holdMinutes;
	private readonly StrategyParam<int> _volume;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useLongEntries;
	private readonly StrategyParam<bool> _useShortEntries;
	private readonly StrategyParam<bool> _closeLongBySignal;
	private readonly StrategyParam<bool> _closeShortBySignal;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ThirdGenerationXma _xma = new();

	private decimal _prevXma;
	private bool _pendingBuy;
	private bool _pendingSell;
	private DateTimeOffset? _entryTime;
	private decimal _entryPrice;

	/// <summary>Period of the third generation moving average.</summary>
	public int Length { get => _length.Value; set => _length.Value = value; }
	/// <summary>Hour when new positions may be opened.</summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	/// <summary>Minute within the hour when openings are allowed.</summary>
	public int StartMinute { get => _startMinute.Value; set => _startMinute.Value = value; }
	/// <summary>Maximum time to keep an open position.</summary>
	public int HoldMinutes { get => _holdMinutes.Value; set => _holdMinutes.Value = value; }
	/// <summary>Order volume.</summary>
	public int Volume { get => _volume.Value; set => _volume.Value = value; }
	/// <summary>Stop-loss distance in points.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Take-profit distance in points.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Enable long entries.</summary>
	public bool UseLongEntries { get => _useLongEntries.Value; set => _useLongEntries.Value = value; }
	/// <summary>Enable short entries.</summary>
	public bool UseShortEntries { get => _useShortEntries.Value; set => _useShortEntries.Value = value; }
	/// <summary>Close long positions when a sell signal appears.</summary>
	public bool CloseLongBySignal { get => _closeLongBySignal.Value; set => _closeLongBySignal.Value = value; }
	/// <summary>Close short positions when a buy signal appears.</summary>
	public bool CloseShortBySignal { get => _closeShortBySignal.Value; set => _closeShortBySignal.Value = value; }
	/// <summary>Candle type for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref=\"Color3rdGenXmaStrategy\"/>.
	/// </summary>
	public Color3rdGenXmaStrategy()
	{
		_length = Param(nameof(Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Length", "3rd Gen MA period", "General");

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start Hour", "Hour for order entry", "Trading");

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Minute for order entry", "Trading");

		_holdMinutes = Param(nameof(HoldMinutes), 720)
			.SetGreaterThanZero()
			.SetDisplay("Hold Minutes", "Maximum holding time in minutes", "Risk");

		_volume = Param(nameof(Volume), 1)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_useLongEntries = Param(nameof(UseLongEntries), true)
			.SetDisplay("Use Long Entries", "Allow long positions", "Trading");

		_useShortEntries = Param(nameof(UseShortEntries), true)
			.SetDisplay("Use Short Entries", "Allow short positions", "Trading");

		_closeLongBySignal = Param(nameof(CloseLongBySignal), false)
			.SetDisplay("Close Long By Signal", "Exit longs on sell signal", "Trading");

		_closeShortBySignal = Param(nameof(CloseShortBySignal), false)
			.SetDisplay("Close Short By Signal", "Exit shorts on buy signal", "Trading");

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
		_xma.Reset();
		_prevXma = 0m;
		_pendingBuy = false;
		_pendingSell = false;
		_entryTime = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_xma.Length = Length;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_xma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _xma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal xmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevXma != 0m)
		{
			if (xmaValue > _prevXma)
			{
				if (CloseShortBySignal && Position < 0)
					BuyMarket(-Position);

				if (UseLongEntries)
					_pendingBuy = true;
			}
			else if (xmaValue < _prevXma)
			{
				if (CloseLongBySignal && Position > 0)
					SellMarket(Position);

				if (UseShortEntries)
					_pendingSell = true;
			}
		}

		var time = candle.OpenTime.LocalDateTime;

		if (_pendingBuy && time.Hour == StartHour && time.Minute == StartMinute && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryTime = candle.OpenTime;
			_entryPrice = candle.ClosePrice;
			_pendingBuy = false;
		}

		if (_pendingSell && time.Hour == StartHour && time.Minute == StartMinute && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryTime = candle.OpenTime;
			_entryPrice = candle.ClosePrice;
			_pendingSell = false;
		}

		if (Position != 0 && _entryTime != null)
		{
			var elapsed = candle.CloseTime - _entryTime.Value;
			if (elapsed.TotalMinutes >= HoldMinutes)
			{
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(-Position);

				_entryTime = null;
				_entryPrice = 0m;
			}
		}

		if (Position > 0 && _entryPrice != 0m)
		{
			var sl = _entryPrice - StopLoss * Security.PriceStep;
			var tp = _entryPrice + TakeProfit * Security.PriceStep;
			if (StopLoss > 0m && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				_entryTime = null;
				_entryPrice = 0m;
			}
			else if (TakeProfit > 0m && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				_entryTime = null;
				_entryPrice = 0m;
			}
		}
		else if (Position < 0 && _entryPrice != 0m)
		{
			var sl = _entryPrice + StopLoss * Security.PriceStep;
			var tp = _entryPrice - TakeProfit * Security.PriceStep;
			if (StopLoss > 0m && candle.HighPrice >= sl)
			{
				BuyMarket(-Position);
				_entryTime = null;
				_entryPrice = 0m;
			}
			else if (TakeProfit > 0m && candle.LowPrice <= tp)
			{
				BuyMarket(-Position);
				_entryTime = null;
				_entryPrice = 0m;
			}
		}

		_prevXma = xmaValue;
	}

	private class ThirdGenerationXma : Indicator<decimal>
	{
		public int Length { get; set; } = 50;

		private EMA _ema1;
		private EMA _ema2;
		private decimal _alpha;

		public override void Reset()
		{
			base.Reset();
			_ema1 = null;
			_ema2 = null;
		}

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (_ema1 == null)
			{
				var sLength = Length * 2;
				var lambda = (decimal)sLength / Length;
				_alpha = lambda * (sLength - 1) / (sLength - lambda);

				_ema1 = new EMA { Length = sLength };
				_ema2 = new EMA { Length = Length };
			}

			var val1 = _ema1.Process(input).GetValue<decimal>();
			var val2 = _ema2.Process(new DecimalIndicatorValue(_ema2, val1, input.Time)).GetValue<decimal>();
			var result = (_alpha + 1m) * val1 - _alpha * val2;

			return new DecimalIndicatorValue(this, result, input.Time);
		}
	}
}
