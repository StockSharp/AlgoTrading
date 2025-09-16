using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gandalf PRO strategy converted from MQL.
/// Calculates dynamic targets using LWMA/SMA smoothing filters and
/// trades when the projected level is sufficiently far from the current price.
/// </summary>
public class GandalfProStrategy : Strategy
{
	private const decimal EntryBufferSteps = 15m;

	private readonly StrategyParam<bool> _enableBuy;
	private readonly StrategyParam<int> _buyLength;
	private readonly StrategyParam<decimal> _buyPriceFactor;
	private readonly StrategyParam<decimal> _buyTrendFactor;
	private readonly StrategyParam<int> _buyStopLoss;
	private readonly StrategyParam<decimal> _buyRiskMultiplier;

	private readonly StrategyParam<bool> _enableSell;
	private readonly StrategyParam<int> _sellLength;
	private readonly StrategyParam<decimal> _sellPriceFactor;
	private readonly StrategyParam<decimal> _sellTrendFactor;
	private readonly StrategyParam<int> _sellStopLoss;
	private readonly StrategyParam<decimal> _sellRiskMultiplier;

	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _buyWeighted = null!;
	private SimpleMovingAverage _buySimple = null!;
	private WeightedMovingAverage _sellWeighted = null!;
	private SimpleMovingAverage _sellSimple = null!;

	private decimal[] _closeHistory = Array.Empty<decimal>();
	private int _availableHistory;
	private int _maxPeriod;

	private decimal _prevBuyWeighted;
	private decimal _prevBuySimple;
	private bool _hasPrevBuyValues;

	private decimal _prevSellWeighted;
	private decimal _prevSellSimple;
	private bool _hasPrevSellValues;

	private decimal? _longStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTargetPrice;

	private decimal _priceStep;

	/// <summary>
	/// Enable buy logic.
	/// </summary>
	public bool EnableBuy
	{
		get => _enableBuy.Value;
		set => _enableBuy.Value = value;
	}

	/// <summary>
	/// LWMA/SMA length for buys.
	/// </summary>
	public int BuyLength
	{
		get => _buyLength.Value;
		set => _buyLength.Value = value;
	}

	/// <summary>
	/// Price smoothing factor for buys.
	/// </summary>
	public decimal BuyPriceFactor
	{
		get => _buyPriceFactor.Value;
		set => _buyPriceFactor.Value = value;
	}

	/// <summary>
	/// Trend smoothing factor for buys.
	/// </summary>
	public decimal BuyTrendFactor
	{
		get => _buyTrendFactor.Value;
		set => _buyTrendFactor.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in price steps.
	/// </summary>
	public int BuyStopLoss
	{
		get => _buyStopLoss.Value;
		set => _buyStopLoss.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the strategy volume for longs.
	/// </summary>
	public decimal BuyRiskMultiplier
	{
		get => _buyRiskMultiplier.Value;
		set => _buyRiskMultiplier.Value = value;
	}

	/// <summary>
	/// Enable sell logic.
	/// </summary>
	public bool EnableSell
	{
		get => _enableSell.Value;
		set => _enableSell.Value = value;
	}

	/// <summary>
	/// LWMA/SMA length for sells.
	/// </summary>
	public int SellLength
	{
		get => _sellLength.Value;
		set => _sellLength.Value = value;
	}

	/// <summary>
	/// Price smoothing factor for sells.
	/// </summary>
	public decimal SellPriceFactor
	{
		get => _sellPriceFactor.Value;
		set => _sellPriceFactor.Value = value;
	}

	/// <summary>
	/// Trend smoothing factor for sells.
	/// </summary>
	public decimal SellTrendFactor
	{
		get => _sellTrendFactor.Value;
		set => _sellTrendFactor.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in price steps.
	/// </summary>
	public int SellStopLoss
	{
		get => _sellStopLoss.Value;
		set => _sellStopLoss.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the strategy volume for shorts.
	/// </summary>
	public decimal SellRiskMultiplier
	{
		get => _sellRiskMultiplier.Value;
		set => _sellRiskMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public GandalfProStrategy()
	{
		_enableBuy = Param(nameof(EnableBuy), true)
			.SetDisplay("Enable Buy", "Allow long trades", "General");

		_buyLength = Param(nameof(BuyLength), 24)
			.SetGreaterThan(1)
			.SetDisplay("Buy Length", "LWMA/SMA length for longs", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_buyPriceFactor = Param(nameof(BuyPriceFactor), 0.18m)
			.SetRange(0m, 1m)
			.SetDisplay("Buy Price Factor", "Recursive smoothing weight for price", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.01m);

		_buyTrendFactor = Param(nameof(BuyTrendFactor), 0.18m)
			.SetRange(0m, 1m)
			.SetDisplay("Buy Trend Factor", "Recursive smoothing weight for trend", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.01m);

		_buyStopLoss = Param(nameof(BuyStopLoss), 62)
			.SetGreaterOrEqualZero()
			.SetDisplay("Buy Stop Loss", "Stop distance for longs in price steps", "Risk");

		_buyRiskMultiplier = Param(nameof(BuyRiskMultiplier), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Buy Risk Multiplier", "Volume multiplier for longs (0 = use base volume)", "Risk");

		_enableSell = Param(nameof(EnableSell), true)
			.SetDisplay("Enable Sell", "Allow short trades", "General");

		_sellLength = Param(nameof(SellLength), 24)
			.SetGreaterThan(1)
			.SetDisplay("Sell Length", "LWMA/SMA length for shorts", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_sellPriceFactor = Param(nameof(SellPriceFactor), 0.18m)
			.SetRange(0m, 1m)
			.SetDisplay("Sell Price Factor", "Recursive smoothing weight for price", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.01m);

		_sellTrendFactor = Param(nameof(SellTrendFactor), 0.18m)
			.SetRange(0m, 1m)
			.SetDisplay("Sell Trend Factor", "Recursive smoothing weight for trend", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.01m);

		_sellStopLoss = Param(nameof(SellStopLoss), 62)
			.SetGreaterOrEqualZero()
			.SetDisplay("Sell Stop Loss", "Stop distance for shorts in price steps", "Risk");

		_sellRiskMultiplier = Param(nameof(SellRiskMultiplier), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Sell Risk Multiplier", "Volume multiplier for shorts (0 = use base volume)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Data type used for calculations", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyWeighted?.Reset();
		_buySimple?.Reset();
		_sellWeighted?.Reset();
		_sellSimple?.Reset();

		_closeHistory = Array.Empty<decimal>();
		_availableHistory = 0;
		_maxPeriod = 0;

		_prevBuyWeighted = 0m;
		_prevBuySimple = 0m;
		_hasPrevBuyValues = false;

		_prevSellWeighted = 0m;
		_prevSellSimple = 0m;
		_hasPrevSellValues = false;

		_longStopPrice = null;
		_longTargetPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;

		_priceStep = 1m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		_maxPeriod = Math.Max(BuyLength, SellLength);
		_closeHistory = new decimal[_maxPeriod + 2];
		_availableHistory = 0;

		_buyWeighted = new WeightedMovingAverage
		{
			Length = BuyLength,
			CandlePrice = CandlePrice.Close
		};

		_buySimple = new SimpleMovingAverage
		{
			Length = BuyLength,
			CandlePrice = CandlePrice.Close
		};

		_sellWeighted = new WeightedMovingAverage
		{
			Length = SellLength,
			CandlePrice = CandlePrice.Close
		};

		_sellSimple = new SimpleMovingAverage
		{
			Length = SellLength,
			CandlePrice = CandlePrice.Close
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_buyWeighted, _buySimple, _sellWeighted, _sellSimple, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal buyWeightedValue, decimal buySimpleValue, decimal sellWeightedValue, decimal sellSimpleValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPositions(candle);

		var buyReady = _hasPrevBuyValues && _availableHistory >= BuyLength;
		var sellReady = _hasPrevSellValues && _availableHistory >= SellLength;

		if (EnableBuy && buyReady && IsFormedAndOnlineAndAllowTrading())
		{
			var target = CalculateTarget(BuyLength, BuyPriceFactor, BuyTrendFactor, _prevBuyWeighted, _prevBuySimple);
			var entryPrice = candle.ClosePrice;

			if (target > entryPrice + EntryBufferSteps * _priceStep)
			{
				var volume = GetOrderVolume(BuyRiskMultiplier);

				if (volume > 0)
				{
					BuyMarket(volume);
					_longTargetPrice = target;
					_longStopPrice = BuyStopLoss > 0 ? entryPrice - BuyStopLoss * _priceStep : null;
				}
			}
		}

		if (EnableSell && sellReady && IsFormedAndOnlineAndAllowTrading())
		{
			var target = CalculateTarget(SellLength, SellPriceFactor, SellTrendFactor, _prevSellWeighted, _prevSellSimple);
			var entryPrice = candle.ClosePrice;

			if (target < entryPrice - EntryBufferSteps * _priceStep)
			{
				var volume = GetOrderVolume(SellRiskMultiplier);

				if (volume > 0)
				{
					SellMarket(volume);
					_shortTargetPrice = target;
					_shortStopPrice = SellStopLoss > 0 ? entryPrice + SellStopLoss * _priceStep : null;
				}
			}
		}

		if (_buyWeighted.IsFormed && _buySimple.IsFormed)
		{
			_prevBuyWeighted = buyWeightedValue;
			_prevBuySimple = buySimpleValue;
			_hasPrevBuyValues = true;
		}

		if (_sellWeighted.IsFormed && _sellSimple.IsFormed)
		{
			_prevSellWeighted = sellWeightedValue;
			_prevSellSimple = sellSimpleValue;
			_hasPrevSellValues = true;
		}

		UpdateCloseHistory(candle.ClosePrice);
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				ClosePosition();
				_longStopPrice = null;
				_longTargetPrice = null;
			}
			else if (_longTargetPrice.HasValue && candle.HighPrice >= _longTargetPrice.Value)
			{
				ClosePosition();
				_longStopPrice = null;
				_longTargetPrice = null;
			}
		}
		else
		{
			_longStopPrice = null;
			_longTargetPrice = null;
		}

		if (Position < 0)
		{
			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				ClosePosition();
				_shortStopPrice = null;
				_shortTargetPrice = null;
			}
			else if (_shortTargetPrice.HasValue && candle.LowPrice <= _shortTargetPrice.Value)
			{
				ClosePosition();
				_shortStopPrice = null;
				_shortTargetPrice = null;
			}
		}
		else
		{
			_shortStopPrice = null;
			_shortTargetPrice = null;
		}
	}

	private void UpdateCloseHistory(decimal close)
	{
		if (_closeHistory.Length <= 2)
			return;

		for (var i = _closeHistory.Length - 1; i > 1; i--)
			_closeHistory[i] = _closeHistory[i - 1];

		_closeHistory[1] = close;

		if (_availableHistory < _closeHistory.Length - 1)
			_availableHistory++;
	}

	private decimal CalculateTarget(int length, decimal priceFactor, decimal trendFactor, decimal weightedPrev, decimal simplePrev)
	{
		if (length <= 1)
			return 0m;

		var t = new decimal[length + 2];
		var s = new decimal[length + 2];

		var lengthMinusOne = length - 1m;

		var trendComponent = (6m * weightedPrev - 6m * simplePrev) / lengthMinusOne;
		t[length] = trendComponent;
		s[length] = 4m * simplePrev - 3m * weightedPrev - trendComponent;

		for (var k = length - 1; k > 0; k--)
		{
			var close = _closeHistory[k];
			s[k] = priceFactor * close + (1m - priceFactor) * (s[k + 1] + t[k + 1]);
			t[k] = trendFactor * (s[k] - s[k + 1]) + (1m - trendFactor) * t[k + 1];
		}

		return s[1] + t[1];
	}

	private decimal GetOrderVolume(decimal riskMultiplier)
	{
		var baseVolume = Volume;
		if (riskMultiplier <= 0m)
			return baseVolume;

		return baseVolume * riskMultiplier;
	}
}
