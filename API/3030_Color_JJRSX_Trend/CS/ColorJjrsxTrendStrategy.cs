using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by the MetaTrader ColorJJRSX expert advisor.
/// Generates entries from the slope of a Jurik-smoothed RSI approximation.
/// </summary>
public class ColorJjrsxTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _jurxPeriod;
	private readonly StrategyParam<int> _jmaPeriod;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private JurikMovingAverage _jma;
	private readonly List<decimal> _jmaHistory = new();

	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;
	private decimal _priceStep = 1m;

	/// <summary>
	/// JurX calculation period (approximated with RSI length).
	/// </summary>
	public int JurxPeriod
	{
		get => _jurxPeriod.Value;
		set => _jurxPeriod.Value = value;
	}

	/// <summary>
	/// Jurik moving average smoothing length.
	/// </summary>
	public int JmaPeriod
	{
		get => _jmaPeriod.Value;
		set => _jmaPeriod.Value = value;
	}

	/// <summary>
	/// Index of the bar used for signal evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on opposite slope.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on opposite slope.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Volume used for new entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorJjrsxTrendStrategy"/> class.
	/// </summary>
	public ColorJjrsxTrendStrategy()
	{
		_jurxPeriod = Param(nameof(JurxPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("JurX Period", "Length of the RSI approximation", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(4, 30, 2);

		_jmaPeriod = Param(nameof(JmaPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("JMA Period", "Length of Jurik smoothing", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Bar index used for signals", "Indicator");

		_enableBuy = Param(nameof(EnableBuy), true)
		.SetDisplay("Enable Longs", "Allow opening long positions", "Trading Rules");

		_enableSell = Param(nameof(EnableSell), true)
		.SetDisplay("Enable Shorts", "Allow opening short positions", "Trading Rules");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
		.SetDisplay("Close Longs", "Allow slope based long exits", "Trading Rules");

		_allowSellClose = Param(nameof(AllowSellClose), true)
		.SetDisplay("Close Shorts", "Allow slope based short exits", "Trading Rules");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume for new entries", "Trading Rules");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signals", "General");
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
		_jmaHistory.Clear();
		ResetLongTargets();
		ResetShortTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = JurxPeriod };
		_jma = new JurikMovingAverage { Length = JmaPeriod };
		_priceStep = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();

		Volume = OrderVolume;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var jmaValue = _jma.Process(new DecimalIndicatorValue(_jma, rsiValue, candle.OpenTime));
		if (!jmaValue.IsFinal)
		return;

		var smoothed = jmaValue.GetValue<decimal>();

		_jmaHistory.Insert(0, smoothed);
		var maxHistory = SignalBar + 3;
		while (_jmaHistory.Count > maxHistory)
		_jmaHistory.RemoveAt(_jmaHistory.Count - 1);

		if (_jmaHistory.Count <= SignalBar + 2)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var value0 = _jmaHistory[SignalBar];
		var value1 = _jmaHistory[SignalBar + 1];
		var value2 = _jmaHistory[SignalBar + 2];

		var slopeUp = value1 < value2;
		var slopeDown = value1 > value2;

		var closeLong = AllowBuyClose && slopeDown;
		var closeShort = AllowSellClose && slopeUp;

		if (Position > 0)
		{
		if (StopLossPoints > 0 && _longStop > 0m && candle.LowPrice <= _longStop)
		{
		SellMarket(Position);
		ResetLongTargets();
		return;
		}

		if (TakeProfitPoints > 0 && _longTake > 0m && candle.HighPrice >= _longTake)
		{
		SellMarket(Position);
		ResetLongTargets();
		return;
		}
		}
		else if (Position < 0)
		{
		if (StopLossPoints > 0 && _shortStop > 0m && candle.HighPrice >= _shortStop)
		{
		BuyMarket(Math.Abs(Position));
		ResetShortTargets();
		return;
		}

		if (TakeProfitPoints > 0 && _shortTake > 0m && candle.LowPrice <= _shortTake)
		{
		BuyMarket(Math.Abs(Position));
		ResetShortTargets();
		return;
		}
		}

		if (Position > 0 && closeLong)
		{
		SellMarket(Position);
		ResetLongTargets();
		}
		else if (Position < 0 && closeShort)
		{
		BuyMarket(Math.Abs(Position));
		ResetShortTargets();
		}

		var buySignal = EnableBuy && slopeUp && value0 > value1;
		var sellSignal = EnableSell && slopeDown && value0 < value1;

		if (buySignal && Position <= 0)
		{
		var volume = OrderVolume + (Position < 0 ? Math.Abs(Position) : 0m);
		if (volume > 0m)
		{
		BuyMarket(volume);
		var entryPrice = candle.ClosePrice;
		_longStop = StopLossPoints > 0 ? entryPrice - StopLossPoints * _priceStep : 0m;
		_longTake = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * _priceStep : 0m;
		ResetShortTargets();
		}
		}
		else if (sellSignal && Position >= 0)
		{
		var volume = OrderVolume + (Position > 0 ? Position : 0m);
		if (volume > 0m)
		{
		SellMarket(volume);
		var entryPrice = candle.ClosePrice;
		_shortStop = StopLossPoints > 0 ? entryPrice + StopLossPoints * _priceStep : 0m;
		_shortTake = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * _priceStep : 0m;
		ResetLongTargets();
		}
		}
	}

	private void ResetLongTargets()
	{
		_longStop = 0m;
		_longTake = 0m;
	}

	private void ResetShortTargets()
	{
		_shortStop = 0m;
		_shortTake = 0m;
	}
}

