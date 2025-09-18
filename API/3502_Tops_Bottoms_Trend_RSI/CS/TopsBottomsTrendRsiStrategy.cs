using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the "Tops bottoms trend and RSI" expert advisor.
/// Buys on new bottoms confirmed by RSI oversold readings and sells on new tops confirmed by RSI overbought readings.
/// Protective stop-loss and take-profit levels are managed using pip-based distances.
/// </summary>
public class TopsBottomsTrendRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _buyStopLossPips;
	private readonly StrategyParam<decimal> _buyTakeProfitPips;
	private readonly StrategyParam<decimal> _buyTakeProfitPercentOfStop;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _sellStopLossPips;
	private readonly StrategyParam<decimal> _sellTakeProfitPercentOfStop;
	private readonly StrategyParam<int> _sellTrendCandles;
	private readonly StrategyParam<decimal> _sellTrendPips;
	private readonly StrategyParam<decimal> _sellTrendQuality;
	private readonly StrategyParam<int> _buyTrendCandles;
	private readonly StrategyParam<decimal> _buyTrendPips;
	private readonly StrategyParam<decimal> _buyTrendQuality;
	private readonly StrategyParam<int> _buyRsiPeriod;
	private readonly StrategyParam<decimal> _buyRsiThreshold;
	private readonly StrategyParam<int> _sellRsiPeriod;
	private readonly StrategyParam<decimal> _sellRsiThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _buyRsi;
	private RelativeStrengthIndex _sellRsi;
	private decimal? _previousBuyRsi;
	private decimal? _previousSellRsi;
	private readonly List<CandleInfo> _candles = new();
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Trade volume for long entries.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions in pips.
	/// </summary>
	public decimal BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Optional fixed take-profit distance for long positions in pips.
	/// Used when percentage based take-profit is disabled.
	/// </summary>
	public decimal BuyTakeProfitPips
	{
		get => _buyTakeProfitPips.Value;
		set => _buyTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long positions expressed as percentage of the stop-loss.
	/// </summary>
	public decimal BuyTakeProfitPercentOfStop
	{
		get => _buyTakeProfitPercentOfStop.Value;
		set => _buyTakeProfitPercentOfStop.Value = value;
	}

	/// <summary>
	/// Trade volume for short entries.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions in pips.
	/// </summary>
	public decimal SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short positions expressed as percentage of the stop-loss.
	/// </summary>
	public decimal SellTakeProfitPercentOfStop
	{
		get => _sellTakeProfitPercentOfStop.Value;
		set => _sellTakeProfitPercentOfStop.Value = value;
	}

	/// <summary>
	/// Number of historical candles analysed when detecting new tops.
	/// </summary>
	public int SellTrendCandles
	{
		get => _sellTrendCandles.Value;
		set => _sellTrendCandles.Value = value;
	}

	/// <summary>
	/// Minimum distance in pips between the current close and the reference low when detecting a new top.
	/// </summary>
	public decimal SellTrendPips
	{
		get => _sellTrendPips.Value;
		set => _sellTrendPips.Value = value;
	}

	/// <summary>
	/// Trend quality factor applied while validating new tops.
	/// </summary>
	public decimal SellTrendQuality
	{
		get => _sellTrendQuality.Value;
		set => _sellTrendQuality.Value = value;
	}

	/// <summary>
	/// Number of historical candles analysed when detecting new bottoms.
	/// </summary>
	public int BuyTrendCandles
	{
		get => _buyTrendCandles.Value;
		set => _buyTrendCandles.Value = value;
	}

	/// <summary>
	/// Minimum distance in pips between the current close and the reference high when detecting a new bottom.
	/// </summary>
	public decimal BuyTrendPips
	{
		get => _buyTrendPips.Value;
		set => _buyTrendPips.Value = value;
	}

	/// <summary>
	/// Trend quality factor applied while validating new bottoms.
	/// </summary>
	public decimal BuyTrendQuality
	{
		get => _buyTrendQuality.Value;
		set => _buyTrendQuality.Value = value;
	}

	/// <summary>
	/// RSI period used to confirm long entries.
	/// </summary>
	public int BuyRsiPeriod
	{
		get => _buyRsiPeriod.Value;
		set => _buyRsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that defines oversold conditions for long entries.
	/// </summary>
	public decimal BuyRsiThreshold
	{
		get => _buyRsiThreshold.Value;
		set => _buyRsiThreshold.Value = value;
	}

	/// <summary>
	/// RSI period used to confirm short entries.
	/// </summary>
	public int SellRsiPeriod
	{
		get => _sellRsiPeriod.Value;
		set => _sellRsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold that defines overbought conditions for short entries.
	/// </summary>
	public decimal SellRsiThreshold
	{
		get => _sellRsiThreshold.Value;
		set => _sellRsiThreshold.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TopsBottomsTrendRsiStrategy"/>.
	/// </summary>
	public TopsBottomsTrendRsiStrategy()
	{
		_buyVolume = Param(nameof(BuyVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Volume", "Volume used when opening long trades", "Risk Management");

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Stop Loss (pips)", "Stop-loss distance for long positions", "Risk Management");

		_buyTakeProfitPips = Param(nameof(BuyTakeProfitPips), 5m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Buy Take Profit (pips)", "Optional fixed take-profit for long positions", "Risk Management");

		_buyTakeProfitPercentOfStop = Param(nameof(BuyTakeProfitPercentOfStop), 100m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Buy Take Profit (%)", "Take-profit as percentage of the stop-loss", "Risk Management");

		_sellVolume = Param(nameof(SellVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Volume", "Volume used when opening short trades", "Risk Management");

		_sellStopLossPips = Param(nameof(SellStopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Stop Loss (pips)", "Stop-loss distance for short positions", "Risk Management");

		_sellTakeProfitPercentOfStop = Param(nameof(SellTakeProfitPercentOfStop), 100m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Sell Take Profit (%)", "Take-profit as percentage of the stop-loss", "Risk Management");

		_sellTrendCandles = Param(nameof(SellTrendCandles), 10)
			.SetGreaterThanZero()
			.SetDisplay("Top Lookback", "Candles inspected when searching for new tops", "Trend Detection");

		_sellTrendPips = Param(nameof(SellTrendPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Top Distance (pips)", "Minimum advance above the reference low", "Trend Detection");

		_sellTrendQuality = Param(nameof(SellTrendQuality), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Top Trend Quality", "Trend quality threshold for short entries", "Trend Detection");

		_buyTrendCandles = Param(nameof(BuyTrendCandles), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bottom Lookback", "Candles inspected when searching for new bottoms", "Trend Detection");

		_buyTrendPips = Param(nameof(BuyTrendPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Bottom Distance (pips)", "Minimum decline below the reference high", "Trend Detection");

		_buyTrendQuality = Param(nameof(BuyTrendQuality), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Bottom Trend Quality", "Trend quality threshold for long entries", "Trend Detection");

		_buyRsiPeriod = Param(nameof(BuyRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Buy RSI Period", "RSI length used to confirm long trades", "Indicators");

		_buyRsiThreshold = Param(nameof(BuyRsiThreshold), 40m)
			.SetDisplay("Buy RSI Threshold", "RSI oversold level for long trades", "Indicators");

		_sellRsiPeriod = Param(nameof(SellRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Sell RSI Period", "RSI length used to confirm short trades", "Indicators");

		_sellRsiThreshold = Param(nameof(SellRsiThreshold), 60m)
			.SetDisplay("Sell RSI Threshold", "RSI overbought level for short trades", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "Data");
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
		_candles.Clear();
		_previousBuyRsi = null;
		_previousSellRsi = null;
		ResetLongProtection();
		ResetShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_buyRsi = new RelativeStrengthIndex { Length = BuyRsiPeriod };
		_sellRsi = new RelativeStrengthIndex { Length = SellRsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_buyRsi, _sellRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _buyRsi);
			DrawIndicator(area, _sellRsi);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		var price = trade.Trade?.Price ?? trade.Order.Price;
		if (price == null)
			return;

		var pip = GetPipSize();
		if (pip <= 0m)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			if (Position > 0m)
			{
				SetLongProtection(price.Value, pip);
			}
			else if (Position >= 0m)
			{
				ResetShortProtection();
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (Position < 0m)
			{
				SetShortProtection(price.Value, pip);
			}
			else if (Position <= 0m)
			{
				ResetLongProtection();
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal buyRsiValue, decimal sellRsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(new CandleInfo(candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		TrimHistory();

		if (TryHandleProtection(candle))
		{
			_previousBuyRsi = buyRsiValue;
			_previousSellRsi = sellRsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousBuyRsi = buyRsiValue;
			_previousSellRsi = sellRsiValue;
			return;
		}

		if (!_buyRsi.IsFormed || !_sellRsi.IsFormed || _previousBuyRsi is null || _previousSellRsi is null)
		{
			_previousBuyRsi = buyRsiValue;
			_previousSellRsi = sellRsiValue;
			return;
		}

		var pip = GetPipSize();
		if (pip <= 0m)
		{
			_previousBuyRsi = buyRsiValue;
			_previousSellRsi = sellRsiValue;
			return;
		}

		if (Position == 0m && !HasActiveOrders())
		{
			var newBottom = IsNewBottomInTrend(BuyTrendCandles, BuyTrendPips, BuyTrendQuality, pip);
			var newTop = IsNewTopInTrend(SellTrendCandles, SellTrendPips, SellTrendQuality, pip);

			if (newBottom && _previousBuyRsi.Value < BuyRsiThreshold)
			{
				BuyMarket(BuyVolume);
			}
			else if (newTop && _previousSellRsi.Value > SellRsiThreshold)
			{
				SellMarket(SellVolume);
			}
		}

		_previousBuyRsi = buyRsiValue;
		_previousSellRsi = sellRsiValue;
	}

	private bool TryHandleProtection(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				ResetLongProtection();
				SellMarket(Position);
				return true;
			}

			if (_longTakePrice is decimal take && candle.HighPrice >= take)
			{
				ResetLongProtection();
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				ResetShortProtection();
				BuyMarket(volume);
				return true;
			}

			if (_shortTakePrice is decimal take && candle.LowPrice <= take)
			{
				ResetShortProtection();
				BuyMarket(volume);
				return true;
			}
		}

		return false;
	}

	private void SetLongProtection(decimal entryPrice, decimal pip)
	{
		var stopDistance = BuyStopLossPips * pip;
		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;

		var takeDistance = 0m;
		if (stopDistance > 0m && BuyTakeProfitPercentOfStop > 0m)
			takeDistance = stopDistance * BuyTakeProfitPercentOfStop / 100m;

		if (takeDistance <= 0m && BuyTakeProfitPips > 0m)
			takeDistance = BuyTakeProfitPips * pip;

		_longTakePrice = takeDistance > 0m ? entryPrice + takeDistance : null;
	}

	private void SetShortProtection(decimal entryPrice, decimal pip)
	{
		var stopDistance = SellStopLossPips * pip;
		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;

		var takeDistance = 0m;
		if (stopDistance > 0m && SellTakeProfitPercentOfStop > 0m)
			takeDistance = stopDistance * SellTakeProfitPercentOfStop / 100m;

		_shortTakePrice = takeDistance > 0m ? entryPrice - takeDistance : null;
	}

	private bool IsNewTopInTrend(int pastCandles, decimal minimumPips, decimal trendQuality, decimal pip)
	{
		if (pastCandles <= 0 || _candles.Count <= pastCandles)
			return false;

		var current = _candles[^1];
		var oldest = _candles[^ (pastCandles + 1)];

		for (var i = 0; i < pastCandles; i++)
		{
			var candle = _candles[^ (i + 1)];
			if (candle.High > current.Close)
				return false;
		}

		var quality = Math.Max(1m, Math.Min(9m, trendQuality));
		var allowedDiff = minimumPips * pip * (100m - quality * 10m) / 100m;

		for (var i = 0; i < pastCandles; i++)
		{
			var candle = _candles[^ (i + 1)];
			if ((oldest.Low - candle.Low) >= allowedDiff)
				return false;
		}

		var requiredAdvance = minimumPips * pip;
		return current.Close >= oldest.Low + requiredAdvance;
	}

	private bool IsNewBottomInTrend(int pastCandles, decimal minimumPips, decimal trendQuality, decimal pip)
	{
		if (pastCandles <= 0 || _candles.Count <= pastCandles)
			return false;

		var current = _candles[^1];
		var oldest = _candles[^ (pastCandles + 1)];

		for (var i = 0; i < pastCandles; i++)
		{
			var candle = _candles[^ (i + 1)];
			if (candle.Low < current.Close)
				return false;
		}

		var quality = Math.Max(1m, Math.Min(9m, trendQuality));
		var allowedDiff = minimumPips * pip * (100m - quality * 10m) / 100m;

		for (var i = 0; i < pastCandles; i++)
		{
			var candle = _candles[^ (i + 1)];
			if ((candle.High - oldest.High) >= allowedDiff)
				return false;
		}

		var requiredDecline = minimumPips * pip;
		return current.Close <= oldest.High - requiredDecline;
	}

	private void TrimHistory()
	{
		var maxCount = Math.Max(BuyTrendCandles, SellTrendCandles) + 2;
		while (_candles.Count > maxCount)
			_candles.RemoveAt(0);
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? Security?.MinStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		if (priceStep == 0.00001m || priceStep == 0.000001m)
			return 0.0001m;

		if (priceStep == 0.001m)
			return 0.01m;

		return priceStep;
	}

	private bool HasActiveOrders()
	{
		return Orders.Any(o => o.State.IsActive());
	}

	private void ResetLongProtection()
	{
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void ResetShortProtection()
	{
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private readonly struct CandleInfo
	{
		public CandleInfo(decimal high, decimal low, decimal close)
		{
			High = high;
			Low = low;
			Close = close;
		}

		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}
