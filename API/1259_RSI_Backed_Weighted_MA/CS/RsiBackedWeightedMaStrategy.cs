using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI and retro weighted moving average rate of change.
/// Enters long when RSI is above threshold and MA ROC is below level.
/// Enters short when RSI is below threshold and MA ROC is above level.
/// Uses ATR based trailing stop and fixed ratio position sizing.
/// </summary>
public class RsiBackedWeightedMaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _rsiLong;
	private readonly StrategyParam<decimal> _rsiShort;
	private readonly StrategyParam<decimal> _rocLong;
	private readonly StrategyParam<decimal> _rocShort;
	private readonly StrategyParam<decimal> _tpActivationAtr;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<decimal> _maxLossPercent;
	private readonly StrategyParam<decimal> _fixedRatio;
	private readonly StrategyParam<decimal> _increasingAmount;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<DataType> _candleType;

	private RateOfChange _rocMa = default!;
	private decimal _trailingStopActivation;
	private decimal _trailingOffset;
	private decimal _trailingStop;
	private decimal _stopLoss;
	private bool _longActive;
	private bool _shortActive;
	private bool _trailingActive;
	private decimal _cashOrder;
	private decimal _capitalRef;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaType MaType { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// RSI value to trigger long entry.
	/// </summary>
	public decimal RsiLongSignal { get => _rsiLong.Value; set => _rsiLong.Value = value; }

	/// <summary>
	/// RSI value to trigger short entry.
	/// </summary>
	public decimal RsiShortSignal { get => _rsiShort.Value; set => _rsiShort.Value = value; }

	/// <summary>
	/// MA ROC threshold for long signals.
	/// </summary>
	public decimal RocMaLongSignal { get => _rocLong.Value; set => _rocLong.Value = value; }

	/// <summary>
	/// MA ROC threshold for short signals.
	/// </summary>
	public decimal RocMaShortSignal { get => _rocShort.Value; set => _rocShort.Value = value; }

	/// <summary>
	/// ATR multiple to activate trailing stop.
	/// </summary>
	public decimal TakeProfitActivation { get => _tpActivationAtr.Value; set => _tpActivationAtr.Value = value; }

	/// <summary>
	/// Trailing stop percent from activation price.
	/// </summary>
	public decimal TrailingPercent { get => _trailingPercent.Value; set => _trailingPercent.Value = value; }

	/// <summary>
	/// Maximum loss per trade in percent.
	/// </summary>
	public decimal MaxLossPercent { get => _maxLossPercent.Value; set => _maxLossPercent.Value = value; }

	/// <summary>
	/// Fixed ratio step in currency units.
	/// </summary>
	public decimal FixedRatio { get => _fixedRatio.Value; set => _fixedRatio.Value = value; }

	/// <summary>
	/// Amount added per fixed ratio step.
	/// </summary>
	public decimal IncreasingOrderAmount { get => _increasingAmount.Value; set => _increasingAmount.Value = value; }

	/// <summary>
	/// Backtest start date.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// Backtest end date.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="RsiBackedWeightedMaStrategy"/>.
	/// </summary>
	public RsiBackedWeightedMaStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI calculation period", "RSI Settings");

		_maType = Param(nameof(MaType), MaType.RWMA)
		.SetDisplay("MA Type", "Moving average type", "MA Settings");

		_maLength = Param(nameof(MaLength), 19)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Moving average period", "MA Settings");

		_rsiLong = Param(nameof(RsiLongSignal), 60m)
		.SetRange(1m, 99m)
		.SetDisplay("RSI Long", "RSI level for long", "Strategy");

		_rsiShort = Param(nameof(RsiShortSignal), 40m)
		.SetRange(1m, 99m)
		.SetDisplay("RSI Short", "RSI level for short", "Strategy");

		_rocLong = Param(nameof(RocMaLongSignal), 0m)
		.SetDisplay("ROC MA Long", "MA ROC long threshold", "Strategy");

		_rocShort = Param(nameof(RocMaShortSignal), 0m)
		.SetDisplay("ROC MA Short", "MA ROC short threshold", "Strategy");

		_tpActivationAtr = Param(nameof(TakeProfitActivation), 5m)
		.SetGreaterThanZero()
		.SetDisplay("TP Activation ATR", "ATR multiplier for trailing", "Strategy");

		_trailingPercent = Param(nameof(TrailingPercent), 3m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing %", "Trailing stop percent", "Strategy");

		_maxLossPercent = Param(nameof(MaxLossPercent), 10m)
		.SetRange(0m, 100m)
		.SetDisplay("Max Loss %", "Maximum loss per trade", "Risk Management");

		_fixedRatio = Param(nameof(FixedRatio), 400m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed Ratio", "Equity step to change size", "Money Management");

		_increasingAmount = Param(nameof(IncreasingOrderAmount), 200m)
		.SetGreaterThanZero()
		.SetDisplay("Order Increase", "Amount added per step", "Money Management");

		var startDate = new DateTimeOffset(new DateTime(2017, 1, 1));
		var endDate = new DateTimeOffset(new DateTime(2024, 7, 1));
		_startDate = Param(nameof(StartDate), startDate)
		.SetDisplay("Start Date", "Backtest start", "Backtesting");
		_endDate = Param(nameof(EndDate), endDate)
		.SetDisplay("End Date", "Backtest end", "Backtesting");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

		_trailingStopActivation = 0m;
		_trailingOffset = 0m;
		_trailingStop = 0m;
		_stopLoss = 0m;
		_longActive = false;
		_shortActive = false;
		_trailingActive = false;
		_cashOrder = 0m;
		_capitalRef = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_capitalRef = Portfolio?.CurrentValue ?? 0m;
		_cashOrder = _capitalRef * 0.95m;

		LengthIndicator<decimal> ma = MaType switch
		{
			MaType.SMA => new SimpleMovingAverage { Length = MaLength },
			_ => new RetroWeightedMovingAverage { Length = MaLength }
		};

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = 20 };
		_rocMa = new RateOfChange { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ma, rsi, atr, OnProcess).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal maValue, decimal rsiValue, decimal atrValue)
	{
		var rocValue = _rocMa.Process(new DecimalIndicatorValue(_rocMa, maValue, candle.OpenTime)).GetValue<decimal>();
		ProcessCandle(candle, maValue, rsiValue, rocValue, atrValue);
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal rsiValue, decimal rocValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var time = candle.OpenTime;
		var inRange = time >= StartDate && time <= EndDate;

		if (Position != 0 && !inRange)
		{
			ClosePosition();
			_trailingActive = false;
			_longActive = false;
			_shortActive = false;
			_stopLoss = 0m;
			_trailingStop = 0m;
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > _capitalRef + FixedRatio)
		{
			var spread = (equity - _capitalRef) / FixedRatio;
			var nbLevel = (int)spread;
			var inc = nbLevel * IncreasingOrderAmount;
			_cashOrder += inc;
			_capitalRef += nbLevel * FixedRatio;
		}
		else if (equity < _capitalRef - FixedRatio)
		{
			var spread = (_capitalRef - equity) / FixedRatio;
			var nbLevel = (int)spread;
			var dec = nbLevel * IncreasingOrderAmount;
			_cashOrder -= dec;
			_capitalRef -= nbLevel * FixedRatio;
		}

		if (_longActive && candle.LowPrice <= _stopLoss)
		{
			_longActive = false;
			_trailingActive = false;
			SellMarket(Math.Abs(Position));
			_stopLoss = 0m;
			_trailingStop = 0m;
			return;
		}

		if (_shortActive && candle.HighPrice >= _stopLoss)
		{
			_shortActive = false;
			_trailingActive = false;
			BuyMarket(Math.Abs(Position));
			_stopLoss = 0m;
			_trailingStop = 0m;
			return;
		}

		if (_trailingActive)
		{
			if (_longActive)
			{
				var theoretical = candle.HighPrice - _trailingOffset;
				if (theoretical > _trailingStop)
				_trailingStop = theoretical;
				if (candle.LowPrice <= _trailingStop)
				{
				_longActive = false;
				_trailingActive = false;
				SellMarket(Math.Abs(Position));
				_trailingStop = 0m;
				}
			}
			else if (_shortActive)
			{
				var theoretical = candle.LowPrice + _trailingOffset;
				if (_trailingStop == 0m || theoretical < _trailingStop)
				_trailingStop = theoretical;
				if (candle.HighPrice >= _trailingStop)
				{
				_shortActive = false;
				_trailingActive = false;
				BuyMarket(Math.Abs(Position));
				_trailingStop = 0m;
				}
			}
		}
		else
		{
			if (_longActive && candle.HighPrice >= _trailingStopActivation)
			{
			_trailingActive = true;
			_trailingStop = candle.HighPrice - _trailingOffset;
			}
			else if (_shortActive && candle.LowPrice <= _trailingStopActivation)
			{
			_trailingActive = true;
			_trailingStop = candle.LowPrice + _trailingOffset;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (rsiValue >= RsiLongSignal && rocValue <= RocMaLongSignal && !_longActive && inRange)
		{
			if (_shortActive)
			{
			_shortActive = false;
			_trailingActive = false;
			BuyMarket(Math.Abs(Position));
			}

			_longActive = true;
			_trailingActive = false;
			_trailingStop = 0m;
			_trailingStopActivation = candle.ClosePrice + TakeProfitActivation * atrValue;
			_trailingOffset = _trailingStopActivation * TrailingPercent / 100m;
			_stopLoss = Math.Max(candle.ClosePrice - 3m * atrValue, candle.ClosePrice * (1m - MaxLossPercent / 100m));
			var qty = _cashOrder / candle.ClosePrice;
			BuyMarket(qty);
		}
		else if (rsiValue <= RsiShortSignal && rocValue >= RocMaShortSignal && !_shortActive && inRange)
		{
			if (_longActive)
			{
			_longActive = false;
			_trailingActive = false;
			SellMarket(Math.Abs(Position));
			}

			_shortActive = true;
			_trailingActive = false;
			_trailingStop = 0m;
			_trailingStopActivation = candle.ClosePrice - TakeProfitActivation * atrValue;
			_trailingOffset = _trailingStopActivation * TrailingPercent / 100m;
			_stopLoss = Math.Min(candle.ClosePrice + 3m * atrValue, candle.ClosePrice * (1m + MaxLossPercent / 100m));
			var qty = _cashOrder / candle.ClosePrice;
			SellMarket(qty);
		}
	}

	/// <summary>
	/// Types of moving averages.
	/// </summary>
	public enum MaType
	{
		/// <summary>Simple moving average.</summary>
		SMA,
		/// <summary>Retro weighted moving average.</summary>
		RWMA
	}

	private class RetroWeightedMovingAverage : Indicator<decimal>
	{
		public int Length { get; set; }
		private readonly Queue<decimal> _buffer = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<decimal>();
			_buffer.Enqueue(price);
			if (_buffer.Count > Length)
			_buffer.Dequeue();

			if (_buffer.Count < Length)
			return new DecimalIndicatorValue(this, default, input.Time);

			var weightX = 100m / (4m + (Length - 4m) * 1.30m);
			var weightY = 1.30m * weightX;
			var sum = 0m;
			var denom = 0m;
			var i = 0;
			foreach (var val in _buffer)
			{
			var w = i <= 3 ? weightX : weightY;
			sum += val * w;
			denom += w;
			i++;
			}

			var value = sum / denom;
			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_buffer.Clear();
		}
	}
}
