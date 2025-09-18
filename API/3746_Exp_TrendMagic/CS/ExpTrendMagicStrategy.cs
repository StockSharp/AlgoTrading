using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TrendMagic based strategy converted from the MetaTrader version.
/// The strategy reacts to TrendMagic color changes and mirrors the
/// original money-management configuration options.
/// </summary>
public class ExpTrendMagicStrategy : Strategy
{
	private readonly StrategyParam<decimal> _moneyManagement;
	private readonly StrategyParam<MarginModeOption> _marginMode;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _deviationPoints;
	private readonly StrategyParam<bool> _allowBuyEntry;
	private readonly StrategyParam<bool> _allowSellEntry;
	private readonly StrategyParam<bool> _allowBuyExit;
	private readonly StrategyParam<bool> _allowSellExit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<AppliedPriceMode> _cciPrice;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _signalBar;

	private CommodityChannelIndex? _cci;
	private AverageTrueRange? _atr;
	private readonly List<int> _colorHistory = new();
	private decimal? _previousTrendMagicValue;
	private decimal? _entryPrice;
	private TimeSpan _candleTimeFrame;
	private DateTimeOffset? _nextLongTradeAllowed;
	private DateTimeOffset? _nextShortTradeAllowed;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExpTrendMagicStrategy()
	{
		_moneyManagement = Param(nameof(MoneyManagement), 0.1m)
		.SetDisplay("Money Management", "Share of capital used per trade", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_marginMode = Param(nameof(MarginMode), MarginModeOption.Lot)
		.SetDisplay("Margin Mode", "Mode used to translate MM into volume", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss", "Protective stop in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(100m, 2000m, 100m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit", "Profit target in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(200m, 4000m, 200m);

		_deviationPoints = Param(nameof(DeviationPoints), 10m)
		.SetDisplay("Deviation", "Maximum price deviation in points", "Trading");

		_allowBuyEntry = Param(nameof(AllowBuyEntry), true)
		.SetDisplay("Allow Buy Entry", "Enable long entries", "Permissions");

		_allowSellEntry = Param(nameof(AllowSellEntry), true)
		.SetDisplay("Allow Sell Entry", "Enable short entries", "Permissions");

		_allowBuyExit = Param(nameof(AllowBuyExit), true)
		.SetDisplay("Allow Buy Exit", "Enable exits for short trades", "Permissions");

		_allowSellExit = Param(nameof(AllowSellExit), true)
		.SetDisplay("Allow Sell Exit", "Enable exits for long trades", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Data");

		_cciPeriod = Param(nameof(CciPeriod), 50)
		.SetDisplay("CCI Period", "Length of the CCI", "Indicator")
		.SetGreaterThanZero();

		_cciPrice = Param(nameof(CciPrice), AppliedPriceMode.Median)
		.SetDisplay("CCI Price", "Applied price for the CCI", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 5)
		.SetDisplay("ATR Period", "Length of the ATR", "Indicator")
		.SetGreaterThanZero();

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Bar shift used for signals", "Indicator")
		.SetGreaterOrEqualToZero();
	}

	/// <summary>
	/// Money management multiplier.
	/// </summary>
	public decimal MoneyManagement
	{
		get => _moneyManagement.Value;
		set => _moneyManagement.Value = value;
	}

	/// <summary>
	/// Mode used to convert the money management value into an order volume.
	/// </summary>
	public MarginModeOption MarginMode
	{
		get => _marginMode.Value;
		set => _marginMode.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allowed deviation in price points (placeholder for compatibility).
	/// </summary>
	public decimal DeviationPoints
	{
		get => _deviationPoints.Value;
		set => _deviationPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool AllowBuyEntry
	{
		get => _allowBuyEntry.Value;
		set => _allowBuyEntry.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool AllowSellEntry
	{
		get => _allowSellEntry.Value;
		set => _allowSellEntry.Value = value;
	}

	/// <summary>
	/// Enables exiting short positions.
	/// </summary>
	public bool AllowBuyExit
	{
		get => _allowBuyExit.Value;
		set => _allowBuyExit.Value = value;
	}

	/// <summary>
	/// Enables exiting long positions.
	/// </summary>
	public bool AllowSellExit
	{
		get => _allowSellExit.Value;
		set => _allowSellExit.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period used by the CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Applied price used as the CCI input.
	/// </summary>
	public AppliedPriceMode CciPrice
	{
		get => _cciPrice.Value;
		set => _cciPrice.Value = value;
	}

	/// <summary>
	/// Period used by the ATR indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Bar offset used when reading TrendMagic colors.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(0, value);
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

		_cci = null;
		_atr = null;
		_colorHistory.Clear();
		_previousTrendMagicValue = null;
		_entryPrice = null;
		_candleTimeFrame = TimeSpan.Zero;
		_nextLongTradeAllowed = null;
		_nextShortTradeAllowed = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_candleTimeFrame = CandleType.Arg is TimeSpan span ? span : TimeSpan.Zero;

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod,
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.BindEx(_atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var cci = _cci;
		var atr = _atr;
		if (cci == null || atr == null)
		return;

		if (!atrValue.IsFinal)
		return;

		var price = GetAppliedPrice(candle, CciPrice);
		var cciIndicatorValue = cci.Process(price, candle.OpenTime, true);

		if (!cci.IsFormed || !atr.IsFormed)
		return;

		var atrDecimal = atrValue.ToDecimal();
		var cciDecimal = cciIndicatorValue.ToDecimal();

		UpdateTrendMagic(candle, cciDecimal, atrDecimal);
	}

	private void UpdateTrendMagic(ICandleMessage candle, decimal cciValue, decimal atrValue)
	{
		var color = CalculateColor(candle, cciValue, atrValue);
		_colorHistory.Insert(0, color);

		var maxHistory = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > maxHistory)
		_colorHistory.RemoveRange(maxHistory, _colorHistory.Count - maxHistory);

		if (_colorHistory.Count <= SignalBar + 1)
		{
			ManageRisk(candle);
			return;
		}

		var recent = _colorHistory[SignalBar];
		var older = _colorHistory[SignalBar + 1];

		if (older == 0 && AllowSellExit && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
		}
	else if (older == 1 && AllowBuyExit && Position > 0m)
		{
			SellMarket(Position);
			_entryPrice = null;
		}

		if (older == 0 && recent == 1 && AllowBuyEntry)
		TryEnterLong(candle);
	else if (older == 1 && recent == 0 && AllowSellEntry)
		TryEnterShort(candle);

		ManageRisk(candle);
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_nextLongTradeAllowed.HasValue && candle.OpenTime < _nextLongTradeAllowed.Value)
		return;

		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
		}

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_nextLongTradeAllowed = _candleTimeFrame > TimeSpan.Zero
		? candle.OpenTime + _candleTimeFrame
		: candle.OpenTime;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_nextShortTradeAllowed.HasValue && candle.OpenTime < _nextShortTradeAllowed.Value)
		return;

		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
		return;

		if (Position > 0m)
		{
			SellMarket(Position);
			_entryPrice = null;
		}

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_nextShortTradeAllowed = _candleTimeFrame > TimeSpan.Zero
		? candle.OpenTime + _candleTimeFrame
		: candle.OpenTime;
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (Position == 0m || _entryPrice is null)
		return;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 1m;

		if (Position > 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice.Value - StopLossPoints * step;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Position);
					_entryPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = _entryPrice.Value + TakeProfitPoints * step;
				if (candle.HighPrice >= takePrice)
				{
					SellMarket(Position);
					_entryPrice = null;
				}
			}
		}
	else if (Position < 0m)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = _entryPrice.Value + StopLossPoints * step;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = _entryPrice.Value - TakeProfitPoints * step;
				if (candle.LowPrice <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					_entryPrice = null;
				}
			}
		}
	}

	private int CalculateColor(ICandleMessage candle, decimal cciValue, decimal atrValue)
	{
		var previous = _previousTrendMagicValue;
		decimal trendMagic;
		int color;

		if (cciValue >= 0m)
		{
			trendMagic = candle.LowPrice - atrValue;
			if (previous.HasValue && trendMagic < previous.Value)
			trendMagic = previous.Value;
			color = 0;
		}
	else
		{
			trendMagic = candle.HighPrice + atrValue;
			if (previous.HasValue && trendMagic > previous.Value)
			trendMagic = previous.Value;
			color = 1;
		}

		_previousTrendMagicValue = trendMagic;
		return color;
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var mm = MoneyManagement;
		if (mm == 0m)
		return NormalizeVolume(Volume);

		if (mm < 0m)
		return NormalizeVolume(Math.Abs(mm));

		var security = Security;
		var portfolio = Portfolio;
		if (security == null || portfolio == null || price <= 0m)
		return NormalizeVolume(Volume);

		var capital = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
		if (capital <= 0m)
		return NormalizeVolume(Volume);

		decimal volume;

		switch (MarginMode)
		{
		case MarginModeOption.FreeMargin:
		case MarginModeOption.Balance:
			{
				var amount = capital * mm;
				volume = amount / price;
				break;
			}
		case MarginModeOption.LossFreeMargin:
		case MarginModeOption.LossBalance:
			{
				if (StopLossPoints <= 0m)
				return NormalizeVolume(Volume);

				var step = security.PriceStep ?? 0m;
				if (step <= 0m)
				return NormalizeVolume(Volume);

				var riskPerContract = StopLossPoints * step;
				if (riskPerContract <= 0m)
				return NormalizeVolume(Volume);

				var lossAmount = capital * mm;
				volume = lossAmount / riskPerContract;
				break;
			}
		case MarginModeOption.Lot:
		default:
			volume = mm;
			break;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		return volume;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceMode mode)
	{
		return mode switch
		{
			AppliedPriceMode.Close => candle.ClosePrice,
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			AppliedPriceMode.Average => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Available money-management modes.
	/// </summary>
	public enum MarginModeOption
	{
		/// <summary>
		/// Use the account free margin share.
		/// </summary>
		FreeMargin,

		/// <summary>
		/// Use the account balance share.
		/// </summary>
		Balance,

		/// <summary>
		/// Use a fraction of free margin as risk measured via stop loss.
		/// </summary>
		LossFreeMargin,

		/// <summary>
		/// Use a fraction of balance as risk measured via stop loss.
		/// </summary>
		LossBalance,

		/// <summary>
		/// Fixed lot size.
		/// </summary>
		Lot,
	}

	/// <summary>
	/// Applied price options for the CCI input.
	/// </summary>
	public enum AppliedPriceMode
	{
		/// <summary>
		/// Close price.
		/// </summary>
		Close,

		/// <summary>
		/// Open price.
		/// </summary>
		Open,

		/// <summary>
		/// High price.
		/// </summary>
		High,

		/// <summary>
		/// Low price.
		/// </summary>
		Low,

		/// <summary>
		/// Median price (high + low) / 2.
		/// </summary>
		Median,

		/// <summary>
		/// Typical price (high + low + close) / 3.
		/// </summary>
		Typical,

		/// <summary>
		/// Weighted price (high + low + 2 * close) / 4.
		/// </summary>
		Weighted,

		/// <summary>
		/// Average price (open + high + low + close) / 4.
		/// </summary>
		Average,
	}
}
