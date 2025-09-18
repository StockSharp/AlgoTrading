using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-timeframe stochastic oscillator strategy with ATR-based stop-loss and take-profit management.
/// Emulates the logic of the \"EA RM Stochastic Band\" MetaTrader expert advisor.
/// </summary>
public class RmStochasticBandStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSmoothing;
	private readonly StrategyParam<int> _stochasticSignalLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<decimal> _minMargin;
	private readonly StrategyParam<decimal> _maxSpreadStandard;
	private readonly StrategyParam<decimal> _maxSpreadCent;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<DataType> _baseCandleType;
	private readonly StrategyParam<DataType> _midCandleType;
	private readonly StrategyParam<DataType> _highCandleType;

	private decimal? _stochM1;
	private decimal? _stochM5;
	private decimal? _stochM15;
	private decimal? _atrValue;
	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// %K lookback for the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing period applied to %K.
	/// </summary>
	public int StochasticSmoothing
	{
		get => _stochasticSmoothing.Value;
		set => _stochasticSmoothing.Value = value;
	}

	/// <summary>
	/// %D moving average length.
	/// </summary>
	public int StochasticSignalLength
	{
		get => _stochasticSignalLength.Value;
		set => _stochasticSignalLength.Value = value;
	}

	/// <summary>
	/// ATR lookback used for volatility-based exits.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for stop-loss calculation.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for take-profit calculation.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum portfolio value required before placing trades.
	/// </summary>
	public decimal MinMargin
	{
		get => _minMargin.Value;
		set => _minMargin.Value = value;
	}

	/// <summary>
	/// Maximum spread (in price units) tolerated on standard accounts.
	/// </summary>
	public decimal MaxSpreadStandard
	{
		get => _maxSpreadStandard.Value;
		set => _maxSpreadStandard.Value = value;
	}

	/// <summary>
	/// Maximum spread (in price units) tolerated on cent accounts.
	/// </summary>
	public decimal MaxSpreadCent
	{
		get => _maxSpreadCent.Value;
		set => _maxSpreadCent.Value = value;
	}

	/// <summary>
	/// Threshold for oversold conditions.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Threshold for overbought conditions.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Primary timeframe used for signal execution.
	/// </summary>
	public DataType BaseCandleType
	{
		get => _baseCandleType.Value;
		set => _baseCandleType.Value = value;
	}

	/// <summary>
	/// Intermediate timeframe used for stochastic confirmation.
	/// </summary>
	public DataType MidCandleType
	{
		get => _midCandleType.Value;
		set => _midCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used for stochastic confirmation and ATR calculation.
	/// </summary>
	public DataType HighCandleType
	{
		get => _highCandleType.Value;
		set => _highCandleType.Value = value;
	}

	public RmStochasticBandStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay(\"Order Volume\", \"Volume of each market order\", \"Trading\");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay(\"Stochastic Length\", \"%K lookback period\", \"Indicators\")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_stochasticSmoothing = Param(nameof(StochasticSmoothing), 3)
			.SetGreaterThanZero()
			.SetDisplay(\"Stochastic Smoothing\", \"Smoothing period applied to %K\", \"Indicators\")
			.SetCanOptimize(true)
			.SetOptimize(1, 7, 1);

		_stochasticSignalLength = Param(nameof(StochasticSignalLength), 3)
			.SetGreaterThanZero()
			.SetDisplay(\"Stochastic Signal\", \"%D moving average length\", \"Indicators\")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay(\"ATR Period\", \"Lookback for ATR volatility filter\", \"Indicators\")
			.SetCanOptimize(true)
			.SetOptimize(7, 30, 1);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay(\"SL Multiplier\", \"ATR multiplier for stop-loss\", \"Risk\")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.25m);

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay(\"TP Multiplier\", \"ATR multiplier for take-profit\", \"Risk\")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 0.5m);

		_minMargin = Param(nameof(MinMargin), 100m)
			.SetGreaterThanZero()
			.SetDisplay(\"Minimum Margin\", \"Required portfolio value before trading\", \"Risk\");

		_maxSpreadStandard = Param(nameof(MaxSpreadStandard), 3m)
			.SetGreaterThanZero()
			.SetDisplay(\"Max Spread Standard\", \"Maximum spread allowed for standard accounts\", \"Filters\");

		_maxSpreadCent = Param(nameof(MaxSpreadCent), 10m)
			.SetGreaterThanZero()
			.SetDisplay(\"Max Spread Cent\", \"Maximum spread allowed for cent accounts\", \"Filters\");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay(\"Oversold Level\", \"Threshold that defines oversold conditions\", \"Signals\")
			.SetCanOptimize(true)
			.SetOptimize(5m, 40m, 5m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay(\"Overbought Level\", \"Threshold that defines overbought conditions\", \"Signals\")
			.SetCanOptimize(true)
			.SetOptimize(60m, 95m, 5m);

		_baseCandleType = Param(nameof(BaseCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay(\"Base Timeframe\", \"Primary execution timeframe\", \"General\");

		_midCandleType = Param(nameof(MidCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay(\"Mid Timeframe\", \"Secondary confirmation timeframe\", \"General\");

		_highCandleType = Param(nameof(HighCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay(\"High Timeframe\", \"Higher confirmation timeframe\", \"General\");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, BaseCandleType),
			(Security, MidCandleType),
			(Security, HighCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stochM1 = null;
		_stochM5 = null;
		_stochM15 = null;
		_atrValue = null;
		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_bestBid = null;
		_bestAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var baseStochastic = CreateStochastic();
		var midStochastic = CreateStochastic();
		var highStochastic = CreateStochastic();
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var baseSubscription = SubscribeCandles(BaseCandleType);
		baseSubscription.BindEx(baseStochastic, ProcessBaseCandle).Start();

		SubscribeCandles(MidCandleType)
			.BindEx(midStochastic, ProcessMidCandle)
			.Start();

		SubscribeCandles(HighCandleType)
			.BindEx(highStochastic, atr, ProcessHighCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private StochasticOscillator CreateStochastic()
	{
		return new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticSmoothing },
			D = { Length = StochasticSignalLength }
		};
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_bestBid = (decimal)bidValue;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_bestAsk = (decimal)askValue;
	}

	private void ProcessMidCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		var stochastic = (StochasticOscillatorValue)stochValue;

		if (stochastic.K is decimal kValue)
			_stochM5 = kValue;
	}

	private void ProcessHighCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal || !atrValue.IsFinal)
			return;

		var stochastic = (StochasticOscillatorValue)stochValue;

		if (stochastic.K is decimal kValue)
			_stochM15 = kValue;

		_atrValue = atrValue.ToDecimal();
	}

	private void ProcessBaseCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		var stochastic = (StochasticOscillatorValue)stochValue;
		if (stochastic.K is not decimal kValue)
			return;

		_stochM1 = kValue;

		ManageOpenPosition(candle);
		TryEnterPosition(candle);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakeProfit = null;
			_shortStopPrice = null;
			_shortTakeProfit = null;
			return;
		}

		if (Position > 0)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_longTakeProfit = null;
				return;
			}

			if (_longTakeProfit is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				_longStopPrice = null;
				_longTakeProfit = null;
			}
		}
		else if (Position < 0)
		{
			var shortVolume = Math.Abs(Position);
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(shortVolume);
				_shortStopPrice = null;
				_shortTakeProfit = null;
				return;
			}

			if (_shortTakeProfit is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(shortVolume);
				_shortStopPrice = null;
				_shortTakeProfit = null;
			}
		}
	}

	private void TryEnterPosition(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!HasSufficientMargin())
			return;

		if (!IsSpreadAcceptable())
			return;

		if (Position != 0)
			return;

		if (HasActiveOrders())
			return;

		if (_stochM1 is not decimal stochFast ||
			_stochM5 is not decimal stochMid ||
			_stochM15 is not decimal stochSlow ||
			_atrValue is not decimal atr)
		{
			return;
		}

		var oversold = OversoldLevel;
		var overbought = OverboughtLevel;

		if (stochFast < oversold && stochMid < oversold && stochSlow < oversold)
		{
			EnterLong(candle.ClosePrice, atr);
		}
		else if (stochFast > overbought && stochMid > overbought && stochSlow > overbought)
		{
			EnterShort(candle.ClosePrice, atr);
		}
	}

	private bool HasSufficientMargin()
	{
		var currentValue = Portfolio?.CurrentValue ?? 0m;
		return currentValue >= MinMargin;
	}

	private bool IsSpreadAcceptable()
	{
		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return false;

		var spread = ask - bid;
		if (spread <= 0m)
			return true;

		var limit = spread > MaxSpreadStandard ? MaxSpreadCent : MaxSpreadStandard;
		return spread <= limit;
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (!order.State.IsFinal())
				return true;
		}

		return false;
	}

	private void EnterLong(decimal price, decimal atr)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		_longStopPrice = price - atr * StopLossMultiplier;
		_longTakeProfit = price + atr * TakeProfitMultiplier;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private void EnterShort(decimal price, decimal atr)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		SellMarket(volume);

		_shortStopPrice = price + atr * StopLossMultiplier;
		_shortTakeProfit = price - atr * TakeProfitMultiplier;
		_longStopPrice = null;
		_longTakeProfit = null;
	}
}
