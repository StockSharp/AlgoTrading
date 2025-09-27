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
/// Strategy that reacts to color flips of the DynamicRS_C indicator.
/// The indicator output is represented by three colors and signals trend changes when it switches to magenta or blue-violet.
/// </summary>
public class DynamicRsCStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private DynamicRsCIndicator _indicator = null!;
	private readonly List<int> _colorHistory = new();
	private decimal _entryPrice;

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback depth of the DynamicRS_C indicator.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Number of finished candles back used to evaluate signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening of long positions on a magenta flip.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Enable opening of short positions on a blue flip.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Allow closing of existing long positions when a short signal appears.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Allow closing of existing short positions when a long signal appears.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss distance from the entry price.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Absolute take-profit distance from the entry price.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with defaults inspired by the MQL expert.
	/// </summary>
	public DynamicRsCStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator processing", "General");

		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Lookback depth of DynamicRS_C", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal Bar", "Finished candles back for signals", "Indicator");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
			.SetDisplay("Allow Buy Entry", "Enable opening of long positions", "Trading");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
			.SetDisplay("Allow Sell Entry", "Enable opening of short positions", "Trading");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
			.SetDisplay("Allow Buy Exit", "Enable closing of long positions", "Trading");

		_allowSellExit = Param(nameof(AllowSellExit), true)
			.SetDisplay("Allow Sell Exit", "Enable closing of short positions", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss", "Absolute loss distance", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit", "Absolute profit distance", "Risk");
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

		_indicator = null!;
		_colorHistory.Clear();
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new DynamicRsCIndicator
		{
			Length = Length
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ManageRisk(candle))
			return;

		var value = (DynamicRsCValue)indicatorValue;

		if (!value.IsFormed)
			return;

		_colorHistory.Add(value.ColorIndex);

		var maxHistory = Math.Max(SignalBar + 3, Length + 3);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);

		var signalIndex = _colorHistory.Count - 1 - SignalBar;
		if (signalIndex < 0)
			return;

		var previousIndex = signalIndex - 1;
		if (previousIndex < 0)
			return;

		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[previousIndex];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isBuySignal = previousColor != 0 && currentColor == 0;
		var isSellSignal = previousColor != 2 && currentColor == 2;

		if (isBuySignal)
		{
			HandleBuySignal(candle);
		}
		else if (isSellSignal)
		{
			HandleSellSignal(candle);
		}
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			return false;

		if (Position > 0)
		{
			if (StopLossPoints > 0m && candle.ClosePrice <= _entryPrice - StopLossPoints)
			{
				CloseLong();
				return true;
			}

			if (TakeProfitPoints > 0m && candle.ClosePrice >= _entryPrice + TakeProfitPoints)
			{
				CloseLong();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (StopLossPoints > 0m && candle.ClosePrice >= _entryPrice + StopLossPoints)
			{
				CloseShort();
				return true;
			}

			if (TakeProfitPoints > 0m && candle.ClosePrice <= _entryPrice - TakeProfitPoints)
			{
				CloseShort();
				return true;
			}
		}

		return false;
	}

	private void HandleBuySignal(ICandleMessage candle)
	{
		if (Position < 0)
		{
			if (!AllowSellExit)
				return;

			if (AllowBuyEntry)
			{
				var volume = Volume + Math.Abs(Position);
				if (volume <= 0m)
					return;

				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				var volume = Math.Abs(Position);
				if (volume <= 0m)
					return;

				BuyMarket(volume);
				_entryPrice = 0m;
			}

			return;
		}

		if (!AllowBuyEntry || Position > 0)
			return;

		var orderVolume = Volume;
		if (orderVolume <= 0m)
			return;

		BuyMarket(orderVolume);
		_entryPrice = candle.ClosePrice;
	}

	private void HandleSellSignal(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (!AllowBuyExit)
				return;

			if (AllowSellEntry)
			{
				var volume = Volume + Position;
				if (volume <= 0m)
					return;

				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
			}
			else
			{
				var volume = Position;
				if (volume <= 0m)
					return;

				SellMarket(volume);
				_entryPrice = 0m;
			}

			return;
		}

		if (!AllowSellEntry || Position < 0)
			return;

		var orderVolume = Volume;
		if (orderVolume <= 0m)
			return;

		SellMarket(orderVolume);
		_entryPrice = candle.ClosePrice;
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		SellMarket(Position);
		_entryPrice = 0m;
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		BuyMarket(Math.Abs(Position));
		_entryPrice = 0m;
	}
}

/// <summary>
/// Indicator that mimics the DynamicRS_C buffer behaviour from MetaTrader.
/// It outputs both the line value and a discrete color index (0, 1, 2).
/// </summary>
public class DynamicRsCIndicator : BaseIndicator<decimal>
{
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _previousValue;
	private int? _previousColor;
	private int? _beforePreviousColor;

	/// <summary>
	/// Number of bars used for the high/low comparison.
	/// </summary>
	public int Length { get; set; } = 5;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_highs.Clear();
		_lows.Clear();
		_previousValue = null;
		_previousColor = null;
		_beforePreviousColor = null;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DynamicRsCValue(this, input, 0m, 1, false);

		if (Length < 1)
			Length = 1;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxStored = Length + 2;
		if (_highs.Count > maxStored)
		{
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_highs.Count <= Length)
		{
			var baseValue = candle.ClosePrice;
			_beforePreviousColor = _previousColor;
			_previousColor = 1;
			_previousValue = baseValue;
			return new DynamicRsCValue(this, input, baseValue, 1, false);
		}

		var previousValue = _previousValue ?? candle.ClosePrice;
		var previousColor = _previousColor ?? 1;
		var beforePreviousColor = _beforePreviousColor ?? 1;

		var currentHigh = candle.HighPrice;
		var currentLow = candle.LowPrice;

		var previousHigh = _highs[_highs.Count - 2];
		var previousLow = _lows[_lows.Count - 2];

		var indexLength = _highs.Count - 1 - Length;
		if (indexLength < 0)
			indexLength = 0;

		var distantHigh = _highs[indexLength];
		var distantLow = _lows[indexLength];

		var color = 1;
		var value = previousValue;

		if (currentHigh < previousHigh && currentHigh < distantHigh && currentHigh < previousValue)
		{
			value = currentHigh;
			color = previousColor == 2 ? 1 : 0;
		}
		else if (currentLow > previousLow && currentLow > distantLow && currentLow > previousValue)
		{
			value = currentLow;
			color = previousColor == 0 ? 1 : 2;
		}
		else
		{
			color = previousColor;

			if (color == 1)
			{
				if (beforePreviousColor == 0)
					color = 2;
				else if (beforePreviousColor == 2)
					color = 0;
			}
		}

		_beforePreviousColor = _previousColor;
		_previousColor = color;
		_previousValue = value;

		return new DynamicRsCValue(this, input, value, color, true);
	}
}

/// <summary>
/// Indicator value exposing the DynamicRS_C price level and color state.
/// </summary>
public class DynamicRsCValue : ComplexIndicatorValue
{
	/// <summary>
	/// Initializes the indicator value container.
	/// </summary>
	public DynamicRsCValue(IIndicator indicator, IIndicatorValue input, decimal level, int colorIndex, bool isFormed)
	: base(indicator, input, (nameof(Level), level), (nameof(ColorIndex), colorIndex))
	{
	Level = level;
	ColorIndex = colorIndex;
	IsFormed = isFormed;
	}

	/// <summary>
	/// Indicator price level corresponding to the coloured line.
	/// </summary>
	public decimal Level { get; }

	/// <summary>
	/// Color index reproduced from the MetaTrader buffer (0, 1, or 2).
	/// </summary>
	public int ColorIndex { get; }

	/// <summary>
	/// Indicates whether the indicator has enough data to provide reliable values.
	/// </summary>
	public bool IsFormed { get; }
}

