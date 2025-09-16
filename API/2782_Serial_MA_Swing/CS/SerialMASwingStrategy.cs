using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Serial moving average swing strategy converted from the MQL SerialMA EA.
/// It opens trades when the custom serial moving average flips across price.
/// </summary>
public class SerialMASwingStrategy : Strategy
{
	private readonly StrategyParam<SerialMaOpenedMode> _openedMode;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SerialMovingAverageIndicator _serialMa;
	private bool _previousBarHadCross;
	private decimal? _previousMovingAverage;
	private decimal? _previousClose;
	private bool _previousValuesReady;

	/// <summary>
	/// Defines how many concurrent swing trades are allowed.
	/// </summary>
	public SerialMaOpenedMode OpenedMode
	{
		get => _openedMode.Value;
		set => _openedMode.Value = value;
	}

	/// <summary>
	/// Enables long trades.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Enables short trades.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Reverses every generated signal when set to <c>true</c>.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Default order volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points (price steps).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points (price steps).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Initializes a new instance of <see cref="SerialMASwingStrategy"/>.
	/// </summary>
	public SerialMASwingStrategy()
	{
		_openedMode = Param(nameof(OpenedMode), SerialMaOpenedMode.AllSwing)
			.SetDisplay("Opened Mode", "How many swing positions may coexist", "Trading");

		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow opening long positions", "Trading");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow opening short positions", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert the generated direction", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default order volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Target distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Data series used for calculations", "General");
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

		_previousBarHadCross = false;
		_previousMovingAverage = null;
		_previousClose = null;
		_previousValuesReady = false;
		_serialMa?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_serialMa = new SerialMovingAverageIndicator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_serialMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var serialValue = (SerialMaValue)indicatorValue;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateState(serialValue, candle);
			return;
		}

		if (!_previousValuesReady)
		{
			UpdateState(serialValue, candle);
			_previousValuesReady = _serialMa.IsFormed;
			return;
		}

		HandleProtectiveLevels(candle);

		var signal = GetPendingSignal();
		if (signal != 0)
		{
			var openLong = signal > 0;
			var openShort = signal < 0;

			if (ReverseSignals)
			{
				(openLong, openShort) = (openShort, openLong);
			}

			if (!EnableBuy)
				openLong = false;

			if (!EnableSell)
				openShort = false;

			if (openLong)
				ExecuteLongEntry();

			if (openShort)
				ExecuteShortEntry();
		}

		UpdateState(serialValue, candle);
		_previousValuesReady = _serialMa.IsFormed;
	}

	private void ExecuteLongEntry()
	{
		if (TradeVolume <= 0m)
			return;

		// Close short exposure before building a long swing.
		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		// Add a new long swing if allowed by the opening mode.
		if (OpenedMode == SerialMaOpenedMode.AllSwing || Position <= 0m)
		{
			BuyMarket(TradeVolume);
		}
	}

	private void ExecuteShortEntry()
	{
		if (TradeVolume <= 0m)
			return;

		// Close long exposure before building a short swing.
		if (Position > 0m)
		{
			SellMarket(Position);
		}

		// Add a new short swing if allowed by the opening mode.
		if (OpenedMode == SerialMaOpenedMode.AllSwing || Position >= 0m)
		{
			SellMarket(TradeVolume);
		}
	}

	private void HandleProtectiveLevels(ICandleMessage candle)
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			return;

		if (Position > 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice - StopLossPoints * step;
				// Exit on stop loss for a long position.
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var targetPrice = PositionPrice + TakeProfitPoints * step;
				// Lock in profit once the target is reached.
				if (candle.HighPrice >= targetPrice)
				{
					SellMarket(Position);
				}
			}
		}
		else if (Position < 0m)
		{
			var absPosition = Math.Abs(Position);

			if (StopLossPoints > 0m)
			{
				var stopPrice = PositionPrice + StopLossPoints * step;
				// Exit on stop loss for a short position.
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(absPosition);
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var targetPrice = PositionPrice - TakeProfitPoints * step;
				// Capture profit when the downside target is achieved.
				if (candle.LowPrice <= targetPrice)
				{
					BuyMarket(absPosition);
				}
			}
		}
	}

	private int GetPendingSignal()
	{
		if (!_previousBarHadCross || _previousMovingAverage == null || _previousClose == null)
			return 0;

		if (_previousClose > _previousMovingAverage)
			return 1;

		if (_previousClose < _previousMovingAverage)
			return -1;

		return 0;
	}

	private void UpdateState(SerialMaValue value, ICandleMessage candle)
	{
		_previousBarHadCross = value.IsCross;
		_previousMovingAverage = value.MovingAverage;
		_previousClose = candle.ClosePrice;
	}
}

/// <summary>
/// Mode describing how the strategy manages swing positions.
/// </summary>
public enum SerialMaOpenedMode
{
	/// <summary>
	/// Open a new position on every signal, even if a same-direction position exists.
	/// </summary>
	AllSwing,

	/// <summary>
	/// Allow only a single swing position per direction.
	/// </summary>
	SingleSwing,
}

/// <summary>
/// Serial moving average indicator replicating the original MQL logic.
/// </summary>
public class SerialMovingAverageIndicator : Indicator<ICandleMessage>
{
	private decimal _sum;
	private int _count;
	private decimal? _previousDiff;
	private int _history;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new SerialMaValue(this, input, 0m, false);

		var close = candle.ClosePrice;
		_history++;

		if (_count == 0)
		{
			_sum = close;
			_count = 1;
			_previousDiff = 0m;
			IsFormed = _history > 1;
			return new SerialMaValue(this, input, close, false);
		}

		_sum += close;
		_count++;

		var movingAverage = _sum / _count;
		var diff = movingAverage - close;
		var isCross = false;

		if (_previousDiff.HasValue && diff * _previousDiff < 0m)
		{
			isCross = true;
			movingAverage = close;
			diff = 0m;
			_sum = close;
			_count = 1;
		}

		_previousDiff = diff;
		IsFormed = _history > 1;

		return new SerialMaValue(this, input, movingAverage, isCross);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_sum = 0m;
		_count = 0;
		_previousDiff = null;
		_history = 0;
	}
}

/// <summary>
/// Indicator value containing the moving average and cross flag.
/// </summary>
public class SerialMaValue : ComplexIndicatorValue
{
	public SerialMaValue(IIndicator indicator, IIndicatorValue input, decimal movingAverage, bool isCross)
		: base(indicator, input, (nameof(MovingAverage), movingAverage), (nameof(IsCross), isCross))
	{
	}

	/// <summary>
	/// Latest serial moving average value.
	/// </summary>
	public decimal MovingAverage => (decimal)GetValue(nameof(MovingAverage));

	/// <summary>
	/// Indicates that the price crossed the moving average on the previous bar.
	/// </summary>
	public bool IsCross => (bool)GetValue(nameof(IsCross));
}
