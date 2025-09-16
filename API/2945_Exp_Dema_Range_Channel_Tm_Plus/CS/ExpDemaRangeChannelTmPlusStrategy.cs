using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double exponential moving average range channel breakout strategy.
/// Replicates the Exp_DEMA_Range_Channel_Tm_Plus expert logic with high-level API.
/// Opens positions on channel breakouts and optionally limits holding time.
/// </summary>
public class ExpDemaRangeChannelTmPlusStrategy : Strategy
{
	private enum ChannelSignal
	{
		DownBearish = 0,
		DownBullish = 1,
		UpBearish = 2,
		UpBullish = 3,
		None = 4,
	}

	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _priceShiftPoints;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntry;
	private readonly StrategyParam<bool> _enableSellEntry;
	private readonly StrategyParam<bool> _enableBuyExit;
	private readonly StrategyParam<bool> _enableSellExit;
	private readonly StrategyParam<bool> _useHoldingLimit;
	private readonly StrategyParam<int> _holdingMinutes;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private DoubleExponentialMovingAverage _highDema;
	private DoubleExponentialMovingAverage _lowDema;
	private Queue<decimal> _upperHistory;
	private Queue<decimal> _lowerHistory;
	private Queue<ChannelSignal> _colorHistory;
	private DateTimeOffset? _positionOpenedTime;

	/// <summary>
	/// Channel calculation period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the channel lines in bars.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// Additional price offset applied to the channel lines in price points.
	/// </summary>
	public decimal PriceShiftPoints
	{
		get => _priceShiftPoints.Value;
		set => _priceShiftPoints.Value = value;
	}

	/// <summary>
	/// Number of bars back to evaluate the breakout color.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool EnableBuyEntry
	{
		get => _enableBuyEntry.Value;
		set => _enableBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool EnableSellEntry
	{
		get => _enableSellEntry.Value;
		set => _enableSellEntry.Value = value;
	}

	/// <summary>
	/// Enable closing of long positions on opposite signals.
	/// </summary>
	public bool EnableBuyExit
	{
		get => _enableBuyExit.Value;
		set => _enableBuyExit.Value = value;
	}

	/// <summary>
	/// Enable closing of short positions on opposite signals.
	/// </summary>
	public bool EnableSellExit
	{
		get => _enableSellExit.Value;
		set => _enableSellExit.Value = value;
	}

	/// <summary>
	/// Enable holding time limit.
	/// </summary>
	public bool UseHoldingLimit
	{
		get => _useHoldingLimit.Value;
		set => _useHoldingLimit.Value = value;
	}

	/// <summary>
	/// Holding limit in minutes before forcing an exit.
	/// </summary>
	public int HoldingMinutes
	{
		get => _holdingMinutes.Value;
		set => _holdingMinutes.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpDemaRangeChannelTmPlusStrategy"/> class.
	/// </summary>
	public ExpDemaRangeChannelTmPlusStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Double EMA base period", "Channel")
		.SetCanOptimize(true)
		.SetOptimize(7, 50, 1);

		_shift = Param(nameof(Shift), 3)
		.SetGreaterOrEqualZero()
		.SetDisplay("Channel Shift", "Forward shift of the channel lines in bars", "Channel");

		_priceShiftPoints = Param(nameof(PriceShiftPoints), 0m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Price Shift", "Offset applied to the channel in price points", "Channel");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Signal Bar", "Number of bars back used for breakout detection", "Signals");

		_enableBuyEntry = Param(nameof(EnableBuyEntry), true)
		.SetDisplay("Enable Long Entry", "Allow opening long positions", "Signals");

		_enableSellEntry = Param(nameof(EnableSellEntry), true)
		.SetDisplay("Enable Short Entry", "Allow opening short positions", "Signals");

		_enableBuyExit = Param(nameof(EnableBuyExit), true)
		.SetDisplay("Close Long", "Close long positions on opposite signals", "Signals");

		_enableSellExit = Param(nameof(EnableSellExit), true)
		.SetDisplay("Close Short", "Close short positions on opposite signals", "Signals");

		_useHoldingLimit = Param(nameof(UseHoldingLimit), true)
		.SetDisplay("Use Holding Limit", "Enable time based position closing", "Risk");

		_holdingMinutes = Param(nameof(HoldingMinutes), 1920)
		.SetGreaterOrEqualZero()
		.SetDisplay("Holding Minutes", "Maximum holding time in minutes", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in price points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 3000m, 100m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit", "Take-profit distance in price points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200m, 4000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the strategy", "General");
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

		_highDema = null;
		_lowDema = null;
		_upperHistory = null;
		_lowerHistory = null;
		_colorHistory = null;
		_positionOpenedTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highDema = new DoubleExponentialMovingAverage { Length = MaPeriod };
		_lowDema = new DoubleExponentialMovingAverage { Length = MaPeriod };
		_upperHistory = new Queue<decimal>();
		_lowerHistory = new Queue<decimal>();
		_colorHistory = new Queue<ChannelSignal>();
		_positionOpenedTime = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var step = Security?.PriceStep ?? 1m;
		Unit? stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Price) : (Unit?)null;
		Unit? takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Price) : (Unit?)null;

		if (stopLossUnit is not null && takeProfitUnit is not null)
		{
			StartProtection(
				stopLoss: stopLossUnit.Value,
				takeProfit: takeProfitUnit.Value,
				useMarketOrders: true);
		}
		else if (stopLossUnit is not null)
		{
			StartProtection(
				stopLoss: stopLossUnit.Value,
				useMarketOrders: true);
		}
		else if (takeProfitUnit is not null)
		{
			StartProtection(
				takeProfit: takeProfitUnit.Value,
				useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security?.PriceStep ?? 1m;
		var priceShift = PriceShiftPoints * step;

		var highValue = _highDema.Process(candle.HighPrice, candle.CloseTime, true).ToDecimal();
		var lowValue = _lowDema.Process(candle.LowPrice, candle.CloseTime, true).ToDecimal();

		var upperLine = highValue + priceShift;
		var lowerLine = lowValue - priceShift;

		// Store last values to emulate the indicator shift behavior.
		_upperHistory.Enqueue(upperLine);
		_lowerHistory.Enqueue(lowerLine);

		var maxCount = Shift + 1;
		while (_upperHistory.Count > maxCount)
			_upperHistory.Dequeue();
		while (_lowerHistory.Count > maxCount)
			_lowerHistory.Dequeue();

		var color = ChannelSignal.None;

		if (_upperHistory.Count == maxCount && _lowerHistory.Count == maxCount)
		{
			var shiftedUpper = _upperHistory.Peek();
			var shiftedLower = _lowerHistory.Peek();

			if (candle.ClosePrice > shiftedUpper)
			{
				color = candle.ClosePrice >= candle.OpenPrice
				? ChannelSignal.UpBullish
				: ChannelSignal.UpBearish;
			}
			else if (candle.ClosePrice < shiftedLower)
			{
				color = candle.ClosePrice <= candle.OpenPrice
				? ChannelSignal.DownBearish
				: ChannelSignal.DownBullish;
			}
		}

		_colorHistory.Enqueue(color);
		while (_colorHistory.Count > SignalBar + 2)
			_colorHistory.Dequeue();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_highDema.IsFormed || !_lowDema.IsFormed || _upperHistory.Count < maxCount)
			return;

		if (UseHoldingLimit && HoldingMinutes > 0 && Position != 0 && _positionOpenedTime is DateTimeOffset opened)
		{
			var holding = candle.CloseTime - opened;
			if (holding.TotalMinutes >= HoldingMinutes)
			{
				CloseCurrentPosition();
				_positionOpenedTime = null;
			}
		}

		var signalColor = GetSignal(SignalBar);
		var previousColor = GetSignal(SignalBar + 1);

		var buySignal = EnableBuyEntry && IsUpSignal(signalColor) && !IsUpSignal(previousColor);
		var sellSignal = EnableSellEntry && IsDownSignal(signalColor) && !IsDownSignal(previousColor);

		var closeLong = EnableBuyExit && IsDownSignal(signalColor);
		var closeShort = EnableSellExit && IsUpSignal(signalColor);

		// Close existing positions before reacting to new breakouts.
		if (closeLong && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_positionOpenedTime = null;
		}

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_positionOpenedTime = null;
		}

		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_positionOpenedTime = candle.CloseTime;
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_positionOpenedTime = candle.CloseTime;
		}

		if (Position == 0)
		{
			_positionOpenedTime = null;
		}
	}

	private ChannelSignal GetSignal(int offset)
	{
		if (offset < 0 || _colorHistory.Count == 0 || offset >= _colorHistory.Count)
		return ChannelSignal.None;

		return _colorHistory.ElementAt(_colorHistory.Count - 1 - offset);
	}

	private static bool IsUpSignal(ChannelSignal signal)
	{
		return signal == ChannelSignal.UpBearish || signal == ChannelSignal.UpBullish;
	}

	private static bool IsDownSignal(ChannelSignal signal)
	{
		return signal == ChannelSignal.DownBearish || signal == ChannelSignal.DownBullish;
	}

	private void CloseCurrentPosition()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
