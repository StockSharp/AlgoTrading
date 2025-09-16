using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "EA Moving Average" MetaTrader strategy.
/// Uses four configurable moving averages to define entry and exit rules for long and short trades.
/// Risk per trade is managed through a fixed percentage of account equity with optional lot reduction after consecutive losses.
/// </summary>
public class EaMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;

	private readonly StrategyParam<int> _buyOpenPeriod;
	private readonly StrategyParam<int> _buyOpenShift;
	private readonly StrategyParam<MaMethod> _buyOpenMethod;
	private readonly StrategyParam<MaPriceType> _buyOpenPrice;

	private readonly StrategyParam<int> _buyClosePeriod;
	private readonly StrategyParam<int> _buyCloseShift;
	private readonly StrategyParam<MaMethod> _buyCloseMethod;
	private readonly StrategyParam<MaPriceType> _buyClosePrice;

	private readonly StrategyParam<int> _sellOpenPeriod;
	private readonly StrategyParam<int> _sellOpenShift;
	private readonly StrategyParam<MaMethod> _sellOpenMethod;
	private readonly StrategyParam<MaPriceType> _sellOpenPrice;

	private readonly StrategyParam<int> _sellClosePeriod;
	private readonly StrategyParam<int> _sellCloseShift;
	private readonly StrategyParam<MaMethod> _sellCloseMethod;
	private readonly StrategyParam<MaPriceType> _sellClosePrice;

	private readonly StrategyParam<bool> _useBuy;
	private readonly StrategyParam<bool> _useSell;
	private readonly StrategyParam<bool> _considerPriceLastOut;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _buyOpenMa;
	private LengthIndicator<decimal>? _buyCloseMa;
	private LengthIndicator<decimal>? _sellOpenMa;
	private LengthIndicator<decimal>? _sellCloseMa;

	private readonly Queue<decimal> _buyOpenBuffer = new();
	private readonly Queue<decimal> _buyCloseBuffer = new();
	private readonly Queue<decimal> _sellOpenBuffer = new();
	private readonly Queue<decimal> _sellCloseBuffer = new();

	private decimal _lastExitPrice;
	private decimal _lastEntryPrice;
	private Sides? _lastEntrySide;
	private decimal _signedPosition;
	private int _consecutiveLosses;

	/// <summary>
	/// Initializes a new instance of the <see cref="EaMovingAverageStrategy"/> class.
	/// </summary>
	public EaMovingAverageStrategy()
	{
		_maximumRisk = Param(nameof(MaximumRisk), 0.02m)
			.SetNotNegative()
			.SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetNotNegative()
			.SetDisplay("Decrease Factor", "Lot reduction factor after losses", "Risk");

		_buyOpenPeriod = Param(nameof(BuyOpenPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Buy Open MA Period", "Moving average period for buy entries", "Buy Entry")
			.SetCanOptimize(true)
			.SetOptimize(5, 80, 5);

		_buyOpenShift = Param(nameof(BuyOpenShift), 3)
			.SetNotNegative()
			.SetDisplay("Buy Open MA Shift", "Shift in bars for the buy entry MA", "Buy Entry");

		_buyOpenMethod = Param(nameof(BuyOpenMethod), MaMethod.Exponential)
			.SetDisplay("Buy Open MA Method", "Moving average method for buy entries", "Buy Entry");

		_buyOpenPrice = Param(nameof(BuyOpenPrice), MaPriceType.Close)
			.SetDisplay("Buy Open Price", "Price type supplied to the buy entry MA", "Buy Entry");

		_buyClosePeriod = Param(nameof(BuyClosePeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Buy Close MA Period", "Moving average period for buy exits", "Buy Exit")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 5);

		_buyCloseShift = Param(nameof(BuyCloseShift), 3)
			.SetNotNegative()
			.SetDisplay("Buy Close MA Shift", "Shift in bars for the buy exit MA", "Buy Exit");

		_buyCloseMethod = Param(nameof(BuyCloseMethod), MaMethod.Exponential)
			.SetDisplay("Buy Close MA Method", "Moving average method for buy exits", "Buy Exit");

		_buyClosePrice = Param(nameof(BuyClosePrice), MaPriceType.Close)
			.SetDisplay("Buy Close Price", "Price type supplied to the buy exit MA", "Buy Exit");

		_sellOpenPeriod = Param(nameof(SellOpenPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Sell Open MA Period", "Moving average period for sell entries", "Sell Entry")
			.SetCanOptimize(true)
			.SetOptimize(5, 80, 5);

		_sellOpenShift = Param(nameof(SellOpenShift), 0)
			.SetNotNegative()
			.SetDisplay("Sell Open MA Shift", "Shift in bars for the sell entry MA", "Sell Entry");

		_sellOpenMethod = Param(nameof(SellOpenMethod), MaMethod.Exponential)
			.SetDisplay("Sell Open MA Method", "Moving average method for sell entries", "Sell Entry");

		_sellOpenPrice = Param(nameof(SellOpenPrice), MaPriceType.Close)
			.SetDisplay("Sell Open Price", "Price type supplied to the sell entry MA", "Sell Entry");

		_sellClosePeriod = Param(nameof(SellClosePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Sell Close MA Period", "Moving average period for sell exits", "Sell Exit")
			.SetCanOptimize(true)
			.SetOptimize(5, 80, 5);

		_sellCloseShift = Param(nameof(SellCloseShift), 2)
			.SetNotNegative()
			.SetDisplay("Sell Close MA Shift", "Shift in bars for the sell exit MA", "Sell Exit");

		_sellCloseMethod = Param(nameof(SellCloseMethod), MaMethod.Exponential)
			.SetDisplay("Sell Close MA Method", "Moving average method for sell exits", "Sell Exit");

		_sellClosePrice = Param(nameof(SellClosePrice), MaPriceType.Close)
			.SetDisplay("Sell Close Price", "Price type supplied to the sell exit MA", "Sell Exit");

		_useBuy = Param(nameof(UseBuy), true)
			.SetDisplay("Use Buy", "Enable long trades", "General");

		_useSell = Param(nameof(UseSell), true)
			.SetDisplay("Use Sell", "Enable short trades", "General");

		_considerPriceLastOut = Param(nameof(ConsiderPriceLastOut), true)
			.SetDisplay("Consider Last Exit Price", "Require price improvement before re-entry", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles processed by the strategy", "General");
	}

	/// <summary>
	/// Risk per trade as a fraction of the portfolio equity.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Lot reduction factor after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Moving average period for buy entries.
	/// </summary>
	public int BuyOpenPeriod
	{
		get => _buyOpenPeriod.Value;
		set => _buyOpenPeriod.Value = value;
	}

	/// <summary>
	/// Shift in bars for the buy entry moving average.
	/// </summary>
	public int BuyOpenShift
	{
		get => _buyOpenShift.Value;
		set => _buyOpenShift.Value = value;
	}

	/// <summary>
	/// Moving average method for buy entries.
	/// </summary>
	public MaMethod BuyOpenMethod
	{
		get => _buyOpenMethod.Value;
		set => _buyOpenMethod.Value = value;
	}

	/// <summary>
	/// Price type used for the buy entry moving average.
	/// </summary>
	public MaPriceType BuyOpenPrice
	{
		get => _buyOpenPrice.Value;
		set => _buyOpenPrice.Value = value;
	}

	/// <summary>
	/// Moving average period for buy exits.
	/// </summary>
	public int BuyClosePeriod
	{
		get => _buyClosePeriod.Value;
		set => _buyClosePeriod.Value = value;
	}

	/// <summary>
	/// Shift in bars for the buy exit moving average.
	/// </summary>
	public int BuyCloseShift
	{
		get => _buyCloseShift.Value;
		set => _buyCloseShift.Value = value;
	}

	/// <summary>
	/// Moving average method for buy exits.
	/// </summary>
	public MaMethod BuyCloseMethod
	{
		get => _buyCloseMethod.Value;
		set => _buyCloseMethod.Value = value;
	}

	/// <summary>
	/// Price type used for the buy exit moving average.
	/// </summary>
	public MaPriceType BuyClosePrice
	{
		get => _buyClosePrice.Value;
		set => _buyClosePrice.Value = value;
	}

	/// <summary>
	/// Moving average period for sell entries.
	/// </summary>
	public int SellOpenPeriod
	{
		get => _sellOpenPeriod.Value;
		set => _sellOpenPeriod.Value = value;
	}

	/// <summary>
	/// Shift in bars for the sell entry moving average.
	/// </summary>
	public int SellOpenShift
	{
		get => _sellOpenShift.Value;
		set => _sellOpenShift.Value = value;
	}

	/// <summary>
	/// Moving average method for sell entries.
	/// </summary>
	public MaMethod SellOpenMethod
	{
		get => _sellOpenMethod.Value;
		set => _sellOpenMethod.Value = value;
	}

	/// <summary>
	/// Price type used for the sell entry moving average.
	/// </summary>
	public MaPriceType SellOpenPrice
	{
		get => _sellOpenPrice.Value;
		set => _sellOpenPrice.Value = value;
	}

	/// <summary>
	/// Moving average period for sell exits.
	/// </summary>
	public int SellClosePeriod
	{
		get => _sellClosePeriod.Value;
		set => _sellClosePeriod.Value = value;
	}

	/// <summary>
	/// Shift in bars for the sell exit moving average.
	/// </summary>
	public int SellCloseShift
	{
		get => _sellCloseShift.Value;
		set => _sellCloseShift.Value = value;
	}

	/// <summary>
	/// Moving average method for sell exits.
	/// </summary>
	public MaMethod SellCloseMethod
	{
		get => _sellCloseMethod.Value;
		set => _sellCloseMethod.Value = value;
	}

	/// <summary>
	/// Price type used for the sell exit moving average.
	/// </summary>
	public MaPriceType SellClosePrice
	{
		get => _sellClosePrice.Value;
		set => _sellClosePrice.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool UseBuy
	{
		get => _useBuy.Value;
		set => _useBuy.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool UseSell
	{
		get => _useSell.Value;
		set => _useSell.Value = value;
	}

	/// <summary>
	/// Require price improvement relative to the last exit before re-entering.
	/// </summary>
	public bool ConsiderPriceLastOut
	{
		get => _considerPriceLastOut.Value;
		set => _considerPriceLastOut.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_buyOpenBuffer.Clear();
		_buyCloseBuffer.Clear();
		_sellOpenBuffer.Clear();
		_sellCloseBuffer.Clear();

		_lastExitPrice = 0m;
		_lastEntryPrice = 0m;
		_lastEntrySide = null;
		_signedPosition = 0m;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_buyOpenMa = CreateMovingAverage(BuyOpenMethod, BuyOpenPeriod);
		_buyCloseMa = CreateMovingAverage(BuyCloseMethod, BuyClosePeriod);
		_sellOpenMa = CreateMovingAverage(SellOpenMethod, SellOpenPeriod);
		_sellCloseMa = CreateMovingAverage(SellCloseMethod, SellClosePeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _buyOpenMa);
			DrawIndicator(area, _buyCloseMa);
			DrawIndicator(area, _sellOpenMa);
			DrawIndicator(area, _sellCloseMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var buyOpen = ProcessMovingAverage(_buyOpenMa, _buyOpenBuffer, BuyOpenShift, GetPrice(candle, BuyOpenPrice), candle);
		var buyClose = ProcessMovingAverage(_buyCloseMa, _buyCloseBuffer, BuyCloseShift, GetPrice(candle, BuyClosePrice), candle);
		var sellOpen = ProcessMovingAverage(_sellOpenMa, _sellOpenBuffer, SellOpenShift, GetPrice(candle, SellOpenPrice), candle);
		var sellClose = ProcessMovingAverage(_sellCloseMa, _sellCloseBuffer, SellCloseShift, GetPrice(candle, SellClosePrice), candle);

		if (buyOpen is not decimal buyOpenValue ||
			buyClose is not decimal buyCloseValue ||
			sellOpen is not decimal sellOpenValue ||
			sellClose is not decimal sellCloseValue)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
		{
			ProcessCloseSignal(candle, buyCloseValue, sellCloseValue);
		}
		else
		{
			ProcessOpenSignal(candle, buyOpenValue, sellOpenValue);
		}
	}

	private void ProcessOpenSignal(ICandleMessage candle, decimal buyMa, decimal sellMa)
	{
		var openPrice = candle.OpenPrice;
		var closePrice = candle.ClosePrice;

		if (UseBuy && openPrice < buyMa && closePrice > buyMa && CanReEnter(Sides.Buy, closePrice))
		{
			var volume = CalculateTradeVolume(closePrice);
			if (volume > 0)
			{
				BuyMarket(volume);
				LogInfo($"Buy signal. Close={closePrice}, MA={buyMa}, Volume={volume}");
			}
		}
		else if (UseSell && openPrice > sellMa && closePrice < sellMa && CanReEnter(Sides.Sell, closePrice))
		{
			var volume = CalculateTradeVolume(closePrice);
			if (volume > 0)
			{
				SellMarket(volume);
				LogInfo($"Sell signal. Close={closePrice}, MA={sellMa}, Volume={volume}");
			}
		}
	}

	private void ProcessCloseSignal(ICandleMessage candle, decimal buyMa, decimal sellMa)
	{
		var openPrice = candle.OpenPrice;
		var closePrice = candle.ClosePrice;

		if (Position > 0 && openPrice > buyMa && closePrice < buyMa)
		{
			ClosePosition();
			LogInfo($"Close long. Close={closePrice}, MA={buyMa}");
		}
		else if (Position < 0 && openPrice < sellMa && closePrice > sellMa)
		{
			ClosePosition();
			LogInfo($"Close short. Close={closePrice}, MA={sellMa}");
		}
	}

	private bool CanReEnter(Sides side, decimal price)
	{
		if (!ConsiderPriceLastOut)
			return true;

		if (_lastExitPrice == 0m)
			return true;

		return side == Sides.Buy
			? _lastExitPrice >= price
			: _lastExitPrice <= price;
	}

	private decimal? ProcessMovingAverage(LengthIndicator<decimal>? indicator, Queue<decimal> buffer, int shift, decimal price, ICandleMessage candle)
	{
		if (indicator == null)
			return null;

		var value = indicator.Process(price, candle.OpenTime, true);

		if (!indicator.IsFormed)
			return null;

		var maValue = value.ToDecimal();

		buffer.Enqueue(maValue);
		var maxSize = shift + 1;
		while (buffer.Count > maxSize)
			buffer.Dequeue();

		if (buffer.Count < maxSize)
			return null;

		return shift == 0 ? maValue : buffer.Peek();
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var baseVolume = Volume > 0 ? Volume : 1m;

		if (price <= 0)
			return NormalizeVolume(baseVolume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0)
			return NormalizeVolume(baseVolume);

		var volume = equity * MaximumRisk / price;

		if (DecreaseFactor > 0 && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0)
			volume = baseVolume;

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0)
				step = 1m;

			if (volume < step)
				volume = step;

			var steps = Math.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;
		}

		if (volume <= 0)
			volume = 1m;

		return volume;
	}

	private static decimal GetPrice(ICandleMessage candle, MaPriceType priceType)
	{
		return priceType switch
		{
			MaPriceType.Close => candle.ClosePrice,
			MaPriceType.Open => candle.OpenPrice,
			MaPriceType.High => candle.HighPrice,
			MaPriceType.Low => candle.LowPrice,
			MaPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			MaPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			MaPriceType.Weighted => (candle.HighPrice + candle.LowPrice + (2m * candle.ClosePrice)) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int length)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = length },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0)
			return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			_lastEntrySide = delta > 0m ? Sides.Buy : Sides.Sell;
			_lastEntryPrice = trade.Trade.Price;
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			_lastExitPrice = trade.Trade.Price;

			if (_lastEntrySide != null && _lastEntryPrice != 0m)
			{
				var profit = _lastEntrySide == Sides.Buy
					? _lastExitPrice - _lastEntryPrice
					: _lastEntryPrice - _lastExitPrice;

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = 0m;
		}
	}
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

/// <summary>
/// Price inputs supported by the moving average calculations.
/// </summary>
public enum MaPriceType
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
