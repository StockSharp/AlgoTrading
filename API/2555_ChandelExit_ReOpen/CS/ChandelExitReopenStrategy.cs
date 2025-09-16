using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the ChandelExitSign expert advisor with re-entry logic.
/// </summary>
public class ChandelExitReopenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _priceStepPoints;
	private readonly StrategyParam<int> _maxAdditions;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;

	private readonly List<CandleInfo> _history = new();
	private readonly List<SignalInfo> _signals = new();

	private decimal? _previousUp;
	private decimal? _previousDown;
	private int _direction;

	private int _longAdditions;
	private int _shortAdditions;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private DateTimeOffset? _lastLongAdditionTime;
	private DateTimeOffset? _lastShortAdditionTime;

	/// <summary>
	/// Initializes a new instance of <see cref="ChandelExitReopenStrategy"/>.
	/// </summary>
	public ChandelExitReopenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for signals", "General");

		_rangePeriod = Param(nameof(RangePeriod), 15)
			.SetDisplay("Range Period", "Lookback for highest high and lowest low", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_shift = Param(nameof(Shift), 1)
			.SetDisplay("Shift", "Bars to skip from the most recent data", "Indicator")
			.SetGreaterOrEqualZero()
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR length for volatility filter", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 4m)
			.SetDisplay("ATR Multiplier", "Multiplier applied to ATR", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "How many bars back to read signals", "Trading")
			.SetGreaterOrEqualZero();

		_priceStepPoints = Param(nameof(PriceStepPoints), 300m)
			.SetDisplay("Re-entry Distance", "Minimum favorable move in price steps before adding", "Position Management")
			.SetGreaterOrEqual(0m)
			.SetCanOptimize(true);

		_maxAdditions = Param(nameof(MaxAdditions), 10)
			.SetDisplay("Max Additions", "Maximum number of re-entries after the initial position", "Position Management")
			.SetGreaterOrEqualZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss Points", "Stop-loss distance in price steps", "Risk Management")
			.SetGreaterOrEqualZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit Points", "Take-profit distance in price steps", "Risk Management")
			.SetGreaterOrEqualZero();

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions on up signals", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions on down signals", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions on down signals", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions on up signals", "Trading");
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
	/// Range length for the Chandelier exit bands.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// Number of the most recent bars skipped before measuring the range.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
	}

	/// <summary>
	/// ATR length used in the signal calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR value.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Offset of the signal bar relative to the latest finished candle.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Required move in price steps before another position add is allowed.
	/// </summary>
	public decimal PriceStepPoints
	{
		get => _priceStepPoints.Value;
		set => _priceStepPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of additional entries after the first fill.
	/// </summary>
	public int MaxAdditions
	{
		get => _maxAdditions.Value;
		set => _maxAdditions.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries generated by the up buffer.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries generated by the down buffer.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enables long exits on down signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enables short exits on up signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
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

		_history.Clear();
		_signals.Clear();

		_previousUp = null;
		_previousDown = null;
		_direction = 0;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atr = atrValue.IsFinal ? atrValue.GetValue<decimal>() : 0m;
		var info = new CandleInfo(candle.OpenTime, candle.HighPrice, candle.LowPrice, candle.ClosePrice, atr);

		_history.Add(info);

		SignalInfo signal;
		if (atrValue.IsFinal)
		{
			signal = CalculateSignal(info);
		}
		else
		{
			signal = SignalInfo.Empty(info.Time);
		}

		_signals.Add(signal);
		TrimCache();

		if (!atrValue.IsFinal)
			return;

		if (_signals.Count <= SignalBar)
			return;

		var targetIndex = _signals.Count - 1 - SignalBar;
		if (targetIndex < 0)
			return;

		var targetSignal = _signals[targetIndex];
		var targetInfo = _history[targetIndex];

		var buyOpen = targetSignal.IsUpSignal && EnableBuyEntries;
		var sellOpen = targetSignal.IsDownSignal && EnableSellEntries;
		var buyClose = targetSignal.IsDownSignal && EnableBuyExits;
		var sellClose = targetSignal.IsUpSignal && EnableSellExits;

		if (((EnableBuyEntries && EnableBuyExits) || (EnableSellEntries && EnableSellExits)) && !buyClose && !sellClose)
		{
			for (var idx = targetIndex - 1; idx >= 0; idx--)
			{
				if (!sellClose && EnableSellExits && _signals[idx].IsUpSignal)
				{
					sellClose = true;
					break;
				}

				if (!buyClose && EnableBuyExits && _signals[idx].IsDownSignal)
				{
					buyClose = true;
					break;
				}
			}
		}

		var step = Security.PriceStep ?? 1m;
		var priceStep = PriceStepPoints * step;

		var longClosed = false;
		var shortClosed = false;

		if (Position > 0m)
		{
			if (_longStopPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ResetLongState();
				longClosed = true;
				LogInfo($"Long stop triggered at {sl:0.########}");
			}
			else if (_longTakePrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ResetLongState();
				longClosed = true;
				LogInfo($"Long take profit triggered at {tp:0.########}");
			}
		}

		if (Position < 0m)
		{
			if (_shortStopPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				shortClosed = true;
				LogInfo($"Short stop triggered at {sl:0.########}");
			}
			else if (_shortTakePrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				shortClosed = true;
				LogInfo($"Short take profit triggered at {tp:0.########}");
			}
		}

		if (!longClosed && buyClose && Position > 0m)
		{
			SellMarket(Position);
			ResetLongState();
			longClosed = true;
			LogInfo($"Long exit on down signal at {candle.ClosePrice:0.########}");
		}

		if (!shortClosed && sellClose && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			shortClosed = true;
			LogInfo($"Short exit on up signal at {candle.ClosePrice:0.########}");
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!longClosed && Position > 0m && MaxAdditions > 0 && _longEntryPrice is decimal lastLongPrice && priceStep > 0m && _longAdditions < MaxAdditions)
		{
			if (candle.ClosePrice - lastLongPrice >= priceStep && _lastLongAdditionTime != candle.OpenTime)
			{
				if (Volume > 0m)
				{
					BuyMarket(Volume);
					_longAdditions++;
					_longEntryPrice = candle.ClosePrice;
					_lastLongAdditionTime = candle.OpenTime;
					UpdateLongProtection(candle.ClosePrice, step);
					LogInfo($"Added to long position at {candle.ClosePrice:0.########} (add #{_longAdditions})");
				}
			}
		}

		if (!shortClosed && Position < 0m && MaxAdditions > 0 && _shortEntryPrice is decimal lastShortPrice && priceStep > 0m && _shortAdditions < MaxAdditions)
		{
			if (lastShortPrice - candle.ClosePrice >= priceStep && _lastShortAdditionTime != candle.OpenTime)
			{
				if (Volume > 0m)
				{
					SellMarket(Volume);
					_shortAdditions++;
					_shortEntryPrice = candle.ClosePrice;
					_lastShortAdditionTime = candle.OpenTime;
					UpdateShortProtection(candle.ClosePrice, step);
					LogInfo($"Added to short position at {candle.ClosePrice:0.########} (add #{_shortAdditions})");
				}
			}
		}

		if (buyOpen && Position < 0m && !EnableSellExits)
		buyOpen = false;

		if (sellOpen && Position > 0m && !EnableBuyExits)
		sellOpen = false;

		if (buyOpen && Volume > 0m)
		{
			var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
			ResetShortState();
			_longAdditions = 0;
			_longEntryPrice = candle.ClosePrice;
			_lastLongAdditionTime = candle.OpenTime;
			UpdateLongProtection(candle.ClosePrice, step);
			LogInfo($"Opened long position at {candle.ClosePrice:0.########}");
		}

		if (sellOpen && Volume > 0m)
		{
			var volume = Volume + (Position > 0m ? Position : 0m);
			SellMarket(volume);
			ResetLongState();
			_shortAdditions = 0;
			_shortEntryPrice = candle.ClosePrice;
			_lastShortAdditionTime = candle.OpenTime;
			UpdateShortProtection(candle.ClosePrice, step);
			LogInfo($"Opened short position at {candle.ClosePrice:0.########}");
		}
	}

	private void TrimCache()
	{
		var maxItems = Math.Max(RangePeriod + Shift + 5, SignalBar + 5) + 50;
		if (_history.Count <= maxItems)
			return;

		var removeCount = _history.Count - maxItems;
		_history.RemoveRange(0, removeCount);
		_signals.RemoveRange(0, removeCount);
	}

	private SignalInfo CalculateSignal(CandleInfo current)
	{
		var currentIndex = _history.Count - 1;
		var range = RangePeriod;
		var shift = Shift;

		if (range <= 0 || currentIndex - shift < 0)
		return SignalInfo.Empty(current.Time);

		var windowEnd = currentIndex - shift;
		var windowStart = windowEnd - (range - 1);

		if (windowStart < 0)
		return SignalInfo.Empty(current.Time);

		var highestHigh = decimal.MinValue;
		var lowestLow = decimal.MaxValue;

		for (var i = windowStart; i <= windowEnd; i++)
		{
			var item = _history[i];
			if (item.High > highestHigh)
			highestHigh = item.High;
			if (item.Low < lowestLow)
			lowestLow = item.Low;
		}

		var atr = current.Atr * AtrMultiplier;
		var upperBand = highestHigh - atr;
		var lowerBand = lowestLow + atr;

		decimal up;
		decimal down;

		if (_direction >= 0)
		{
			if (current.Close < upperBand)
			{
				_direction = -1;
				up = lowerBand;
				down = upperBand;
			}
			else
			{
				up = upperBand;
				down = lowerBand;
			}
		}
		else
		{
			if (current.Close > lowerBand)
			{
				_direction = 1;
				down = lowerBand;
				up = upperBand;
			}
			else
			{
				up = lowerBand;
				down = upperBand;
			}
		}

		var isUpSignal = false;
		var isDownSignal = false;

		if (_previousDown is decimal prevDn && _previousUp is decimal prevUp)
		{
			if (prevDn <= prevUp && down > up)
			isUpSignal = true;

			if (prevDn >= prevUp && down < up)
			isDownSignal = true;
		}

		_previousUp = up;
		_previousDown = down;

		return new SignalInfo(current.Time, isUpSignal, isDownSignal, up, down);
	}

	private void UpdateLongProtection(decimal entryPrice, decimal step)
	{
		_longStopPrice = StopLossPoints > 0 ? entryPrice - StopLossPoints * step : null;
		_longTakePrice = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * step : null;
	}

	private void UpdateShortProtection(decimal entryPrice, decimal step)
	{
		_shortStopPrice = StopLossPoints > 0 ? entryPrice + StopLossPoints * step : null;
		_shortTakePrice = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * step : null;
	}

	private void ResetLongState()
	{
		_longAdditions = 0;
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
		_lastLongAdditionTime = null;
	}

	private void ResetShortState()
	{
		_shortAdditions = 0;
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_lastShortAdditionTime = null;
	}

	private sealed record CandleInfo(DateTimeOffset Time, decimal High, decimal Low, decimal Close, decimal Atr);

	private sealed record SignalInfo(DateTimeOffset Time, bool IsUpSignal, bool IsDownSignal, decimal Up, decimal Down)
	{
		public static SignalInfo Empty(DateTimeOffset time) => new(time, false, false, 0m, 0m);
	}
}
