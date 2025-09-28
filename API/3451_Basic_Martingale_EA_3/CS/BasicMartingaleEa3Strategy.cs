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
/// Martingale grid strategy converted from the MetaTrader 5 script "Basic Martingale EA 3".
/// TEMA defines the entry direction while ATR distances control the averaging grid and trailing stops.
/// </summary>
public class BasicMartingaleEa3Strategy : Strategy
{
	public enum AveragingModes
	{
		AverageDown,
		AverageUp,
		None,
	}

	public enum MartinModes
	{
		Multiply,
		Increment,
	}

	private sealed class PositionSlice
	{
		public decimal Volume { get; set; }

		public decimal Price { get; set; }
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _temaPeriod;
	private readonly StrategyParam<int> _barsCalculated;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _gridMultiplier;
	private readonly StrategyParam<int> _maxAverageOrders;
	private readonly StrategyParam<AveragingModes> _averagingMode;
	private readonly StrategyParam<MartinModes> _martinMode;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _lotIncrement;
	private readonly StrategyParam<bool> _tradeAtNewBar;
	private readonly StrategyParam<int> _trailingStart;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStep;

	private TripleExponentialMovingAverage _tema = null!;
	private AverageTrueRange _atr = null!;

	private readonly List<PositionSlice> _longEntries = new();
	private readonly List<PositionSlice> _shortEntries = new();

	private decimal _lastAtr;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private decimal _longStopPrice;
	private decimal _longTakePrice;
	private decimal _shortStopPrice;
	private decimal _shortTakePrice;
	private decimal _longTrailing;
	private decimal _shortTrailing;
	private bool _longTrailingActive;
	private bool _shortTrailingActive;
	private DateTimeOffset? _lastEntryBarTime;
	private int _processedBars;

	/// <summary>
	/// Initializes a new instance of the <see cref="BasicMartingaleEa3Strategy"/> class.
	/// </summary>
	public BasicMartingaleEa3Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_startVolume = Param(nameof(StartVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Start Volume", "Baseline order size", "Money Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetDisplay("Stop Loss (points)", "Protective stop distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 20m)
			.SetDisplay("Take Profit (points)", "Target distance in price steps", "Risk");

		_startHour = Param(nameof(StartHour), 3)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour when new baskets may start", "Schedule");

		_endHour = Param(nameof(EndHour), 18)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Hour when new baskets are blocked", "Schedule");

		_temaPeriod = Param(nameof(TemaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("TEMA Period", "Length of the triple exponential moving average", "Indicators");

		_barsCalculated = Param(nameof(BarsCalculated), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bars Calculated", "Minimum completed candles before trading", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Length of the ATR used for grid spacing", "Indicators");

		_gridMultiplier = Param(nameof(GridMultiplier), 0.75m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Multiplier", "ATR multiple between averaging orders", "Money Management");

		_maxAverageOrders = Param(nameof(MaxAverageOrders), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Average Orders", "Maximum additional entries per side", "Money Management");

		_averagingMode = Param(nameof(Averaging), AveragingModes.AverageDown)
			.SetDisplay("Averaging Mode", "Choose averaging direction", "Money Management");

		_martinMode = Param(nameof(Martin), MartinModes.Multiply)
			.SetDisplay("Martingale Mode", "Volume growth scheme", "Money Management");

		_lotMultiplier = Param(nameof(LotMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Factor applied in multiply mode", "Money Management");

		_lotIncrement = Param(nameof(LotIncrement), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Increment", "Additional size in increment mode", "Money Management");

		_tradeAtNewBar = Param(nameof(TradeAtNewBar), false)
			.SetDisplay("Trade At New Bar", "Restrict new baskets to one per candle", "General");

		_trailingStart = Param(nameof(TrailingStart), 100)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start", "Points before activating trailing", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 30)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Step to move the trailing stop", "Risk");
	}

	/// <summary>
	/// Candle type driving the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Baseline order volume for the first entry of a basket.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Start hour of the trading window (exchange time).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour of the trading window (exchange time).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// TEMA length.
	/// </summary>
	public int TemaPeriod
	{
		get => _temaPeriod.Value;
		set => _temaPeriod.Value = value;
	}

	/// <summary>
	/// Minimum number of completed candles before signals are processed.
	/// </summary>
	public int BarsCalculated
	{
		get => _barsCalculated.Value;
		set => _barsCalculated.Value = value;
	}

	/// <summary>
	/// ATR length used for grid spacing.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR when checking averaging distances.
	/// </summary>
	public decimal GridMultiplier
	{
		get => _gridMultiplier.Value;
		set => _gridMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum number of additional averaging trades per direction.
	/// </summary>
	public int MaxAverageOrders
	{
		get => _maxAverageOrders.Value;
		set => _maxAverageOrders.Value = value;
	}

	/// <summary>
	/// Averaging mode defining when additional trades are allowed.
	/// </summary>
	public AveragingModes Averaging
	{
		get => _averagingMode.Value;
		set => _averagingMode.Value = value;
	}

	/// <summary>
	/// Martingale mode controlling the next order size.
	/// </summary>
	public MartinModes Martin
	{
		get => _martinMode.Value;
		set => _martinMode.Value = value;
	}

	/// <summary>
	/// Multiplication factor when martingale mode is <see cref="MartinModes.Multiply"/>.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Increment added when martingale mode is <see cref="MartinModes.Increment"/>.
	/// </summary>
	public decimal LotIncrement
	{
		get => _lotIncrement.Value;
		set => _lotIncrement.Value = value;
	}

	/// <summary>
	/// Restrict initial entries to at most one per candle.
	/// </summary>
	public bool TradeAtNewBar
	{
		get => _tradeAtNewBar.Value;
		set => _tradeAtNewBar.Value = value;
	}

	/// <summary>
	/// Distance before activating trailing in favor of the position (points).
	/// </summary>
	public int TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}

	/// <summary>
	/// Trailing stop offset in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Step used to tighten the trailing stop (points).
	/// </summary>
	public int TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
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
		_longEntries.Clear();
		_shortEntries.Clear();
		_lastAtr = 0m;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
		_longStopPrice = 0m;
		_longTakePrice = 0m;
		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
		_longTrailing = 0m;
		_shortTrailing = 0m;
		_longTrailingActive = false;
		_shortTrailingActive = false;
		_lastEntryBarTime = null;
		_processedBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		_tema = new TripleExponentialMovingAverage { Length = TemaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_tema, _atr, ProcessCandle).Start();

		StartProtection();
		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal temaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_processedBars++;
		_lastAtr = atrValue;

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading() || !_tema.IsFormed || !_atr.IsFormed || _processedBars < BarsCalculated)
			return;

		if (!IsWithinTradingHours(candle.CloseTime))
			return;

		var closePrice = candle.ClosePrice;
		var priceStep = GetPriceStep();

		if (Position == 0m)
		{
			_longEntries.Clear();
			_shortEntries.Clear();
			_lastBuyVolume = 0m;
			_lastSellVolume = 0m;
			_longTrailingActive = false;
			_shortTrailingActive = false;

			if (TradeAtNewBar && _lastEntryBarTime == candle.CloseTime)
				return;

			if (closePrice > temaValue)
			{
				OpenLong(closePrice, priceStep, candle.CloseTime);
			}
			else if (closePrice < temaValue)
			{
				OpenShort(closePrice, priceStep, candle.CloseTime);
			}
		}
		else if (Position > 0m)
		{
			ManageLongPositions(closePrice, priceStep);
		}
		else if (Position < 0m)
		{
			ManageShortPositions(closePrice, priceStep);
		}
	}

	private void OpenLong(decimal price, decimal priceStep, DateTimeOffset candleTime)
	{
		var volume = AdjustVolume(StartVolume);
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		BuyMarket(volume);

		_longEntries.Add(new PositionSlice { Volume = volume, Price = price });
		_lastBuyVolume = volume;
		_longStopPrice = StopLossPoints > 0m ? price - StopLossPoints * priceStep : 0m;
		_longTakePrice = TakeProfitPoints > 0m ? price + TakeProfitPoints * priceStep : 0m;
		_longTrailingActive = false;
		_longTrailing = 0m;
		_lastEntryBarTime = candleTime;
	}

	private void OpenShort(decimal price, decimal priceStep, DateTimeOffset candleTime)
	{
		var volume = AdjustVolume(StartVolume);
		if (volume <= 0m)
			return;

		CancelActiveOrders();
		SellMarket(volume);

		_shortEntries.Add(new PositionSlice { Volume = volume, Price = price });
		_lastSellVolume = volume;
		_shortStopPrice = StopLossPoints > 0m ? price + StopLossPoints * priceStep : 0m;
		_shortTakePrice = TakeProfitPoints > 0m ? price - TakeProfitPoints * priceStep : 0m;
		_shortTrailingActive = false;
		_shortTrailing = 0m;
		_lastEntryBarTime = candleTime;
	}

	private void ManageLongPositions(decimal price, decimal priceStep)
	{
		if (_longEntries.Count == 0)
			return;

		var avgPrice = GetAveragePrice(_longEntries);
		var lowestPrice = GetExtremePrice(_longEntries, true);
		var highestPrice = GetExtremePrice(_longEntries, false);

		if (_longStopPrice > 0m && price <= _longStopPrice)
		{
			FlattenLong();
			return;
		}

		if (_longTakePrice > 0m && price >= _longTakePrice)
		{
			FlattenLong();
			return;
		}

		if (_longTrailingActive && _longTrailing > 0m && price <= _longTrailing)
		{
			FlattenLong();
			return;
		}

		TryActivateLongTrailing(price, avgPrice, priceStep);

		if (_lastAtr <= 0m || _longEntries.Count >= MaxAverageOrders)
			return;

		var nextVolume = CalculateNextVolume(true);
		if (nextVolume <= 0m)
			return;

		var atrDistance = GridMultiplier * _lastAtr;

		if (Averaging == AveragingModes.AverageDown)
		{
			if (price <= lowestPrice - atrDistance)
			{
				BuyMarket(nextVolume);
				_longEntries.Add(new PositionSlice { Volume = nextVolume, Price = price });
				_lastBuyVolume = nextVolume;
			}
		}
		else if (Averaging == AveragingModes.AverageUp)
		{
			if (price >= highestPrice + atrDistance)
			{
				BuyMarket(nextVolume);
				_longEntries.Add(new PositionSlice { Volume = nextVolume, Price = price });
				_lastBuyVolume = nextVolume;
			}
		}
	}

	private void ManageShortPositions(decimal price, decimal priceStep)
	{
		if (_shortEntries.Count == 0)
			return;

		var avgPrice = GetAveragePrice(_shortEntries);
		var highestPrice = GetExtremePrice(_shortEntries, true);
		var lowestPrice = GetExtremePrice(_shortEntries, false);

		if (_shortStopPrice > 0m && price >= _shortStopPrice)
		{
			FlattenShort();
			return;
		}

		if (_shortTakePrice > 0m && price <= _shortTakePrice)
		{
			FlattenShort();
			return;
		}

		if (_shortTrailingActive && _shortTrailing > 0m && price >= _shortTrailing)
		{
			FlattenShort();
			return;
		}

		TryActivateShortTrailing(price, avgPrice, priceStep);

		if (_lastAtr <= 0m || _shortEntries.Count >= MaxAverageOrders)
			return;

		var nextVolume = CalculateNextVolume(false);
		if (nextVolume <= 0m)
			return;

		var atrDistance = GridMultiplier * _lastAtr;

		if (Averaging == AveragingModes.AverageDown)
		{
			if (price >= highestPrice + atrDistance)
			{
				SellMarket(nextVolume);
				_shortEntries.Add(new PositionSlice { Volume = nextVolume, Price = price });
				_lastSellVolume = nextVolume;
			}
		}
		else if (Averaging == AveragingModes.AverageUp)
		{
			if (price <= lowestPrice - atrDistance)
			{
				SellMarket(nextVolume);
				_shortEntries.Add(new PositionSlice { Volume = nextVolume, Price = price });
				_lastSellVolume = nextVolume;
			}
		}
	}

	private void TryActivateLongTrailing(decimal price, decimal avgPrice, decimal priceStep)
	{
		var startDistance = TrailingStart * priceStep;
		var stopDistance = TrailingStop * priceStep;
		var stepDistance = TrailingStep * priceStep;

		if (startDistance <= 0m || stopDistance <= 0m || stepDistance <= 0m)
			return;

		if (!_longTrailingActive)
		{
			if (price - avgPrice >= startDistance)
			{
				_longTrailing = price - stopDistance;
				_longTrailingActive = _longTrailing > 0m;
			}
		}
		else if (price - _longTrailing >= stepDistance)
		{
			_longTrailing = price - stopDistance;
		}
	}

	private void TryActivateShortTrailing(decimal price, decimal avgPrice, decimal priceStep)
	{
		var startDistance = TrailingStart * priceStep;
		var stopDistance = TrailingStop * priceStep;
		var stepDistance = TrailingStep * priceStep;

		if (startDistance <= 0m || stopDistance <= 0m || stepDistance <= 0m)
			return;

		if (!_shortTrailingActive)
		{
			if (avgPrice - price >= startDistance)
			{
				_shortTrailing = price + stopDistance;
				_shortTrailingActive = _shortTrailing > 0m;
			}
		}
		else if (_shortTrailing - price >= stepDistance)
		{
			_shortTrailing = price + stopDistance;
		}
	}

	private void FlattenLong()
	{
		if (Position > 0m)
			SellMarket(Position);

		_longEntries.Clear();
		_longTrailingActive = false;
		_longStopPrice = 0m;
		_longTakePrice = 0m;
	}

	private void FlattenShort()
	{
		if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		_shortEntries.Clear();
		_shortTrailingActive = false;
		_shortStopPrice = 0m;
		_shortTakePrice = 0m;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (Position > 0m && _longTrailingActive && _longTrailing > 0m && candle.LowPrice <= _longTrailing)
		{
			FlattenLong();
			return;
		}

		if (Position < 0m && _shortTrailingActive && _shortTrailing > 0m && candle.HighPrice >= _shortTrailing)
		{
			FlattenShort();
		}
	}

	private decimal CalculateNextVolume(bool isBuy)
	{
		var referenceVolume = isBuy ? (_lastBuyVolume > 0m ? _lastBuyVolume : StartVolume) : (_lastSellVolume > 0m ? _lastSellVolume : StartVolume);

		decimal nextVolume = referenceVolume;
		switch (Martin)
		{
			case MartinModes.Multiply:
				nextVolume = referenceVolume * LotMultiplier;
				break;
			case MartinModes.Increment:
				nextVolume = referenceVolume + LotIncrement;
				break;
		}

		if (nextVolume < StartVolume)
			nextVolume = StartVolume;

		return AdjustVolume(nextVolume);
	}

	private decimal GetAveragePrice(List<PositionSlice> slices)
	{
		decimal totalVolume = 0m;
		decimal weightedPrice = 0m;
		foreach (var slice in slices)
		{
			totalVolume += slice.Volume;
			weightedPrice += slice.Price * slice.Volume;
		}

		if (totalVolume <= 0m)
			return 0m;

		return weightedPrice / totalVolume;
	}

	private decimal GetExtremePrice(List<PositionSlice> slices, bool highest)
	{
		decimal result = 0m;
		var first = true;
		foreach (var slice in slices)
		{
			if (first)
			{
				result = slice.Price;
				first = false;
				continue;
			}

			if (highest)
			{
				if (slice.Price > result)
					result = slice.Price;
			}
			else if (slice.Price < result)
			{
				result = slice.Price;
			}
		}

		return result;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var start = StartHour;
		var end = EndHour;
		var hour = time.Hour;

		if (start == end)
			return true;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step);
			volume = steps * step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}
}

