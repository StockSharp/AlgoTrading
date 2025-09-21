using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto KDJ strategy converted from the MetaTrader expert advisor.
/// Uses the KDJ oscillator (smoothed stochastic) to detect momentum reversals
/// and replicates the original money management options including risk-based sizing.
/// </summary>
public class AutoKdjStrategy : Strategy
{
	private readonly StrategyParam<int> _kdjPeriod;
	private readonly StrategyParam<int> _smoothK;
	private readonly StrategyParam<int> _smoothD;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _leverage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousK;
	private decimal? _previousDiff;
	private decimal _pipSize;
	private int _lossStreak;
	private decimal? _entryPrice;
	private Sides? _entrySide;

	/// <summary>
	/// Lookback length used to compute the RSV component of the KDJ oscillator.
	/// </summary>
	public int KdjPeriod
	{
		get => _kdjPeriod.Value;
		set => _kdjPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the %K line.
	/// </summary>
	public int SmoothK
	{
		get => _smoothK.Value;
		set => _smoothK.Value = value;
	}

	/// <summary>
	/// Smoothing length applied to the %D line.
	/// </summary>
	public int SmoothD
	{
		get => _smoothD.Value;
		set => _smoothD.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables submission of stop loss protection orders.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Enables submission of take profit protection orders.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Base order volume used before applying risk adjustments.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of equity allocated as risk when calculating dynamic volume.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor used to reduce volume after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Account leverage used in the risk-based sizing formula.
	/// </summary>
	public int Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>
	/// Type of candles supplied to the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="AutoKdjStrategy"/> with default parameters.
	/// </summary>
	public AutoKdjStrategy()
	{
		_kdjPeriod = Param(nameof(KdjPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("KDJ Length", "Lookback for RSV calculation", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_smoothK = Param(nameof(SmoothK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %K", "Smoothing applied to %K", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_smoothD = Param(nameof(SmoothD), 6)
			.SetGreaterThanZero()
			.SetDisplay("Smooth %D", "Smoothing applied to %D", "KDJ")
			.SetCanOptimize(true)
			.SetOptimize(1, 15, 1);

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 300, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 200)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Protective target distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 400, 10);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Enable Stop Loss", "Toggle protective stop", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Enable Take Profit", "Toggle protective take", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Minimum order volume", "Trading");

		_maximumRisk = Param(nameof(MaximumRisk), 0.4m)
			.SetNotNegative()
			.SetDisplay("Maximum Risk", "Fraction of equity allocated per trade", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1m, 0.1m);

		_decreaseFactor = Param(nameof(DecreaseFactor), 0.3m)
			.SetNotNegative()
			.SetDisplay("Decrease Factor", "Volume reduction after losses", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 5m, 0.5m);

		_leverage = Param(nameof(Leverage), 100)
			.SetGreaterThanZero()
			.SetDisplay("Leverage", "Account leverage for sizing", "Money Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 500, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");
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

		_previousK = null;
		_previousDiff = null;
		_lossStreak = 0;
		_entryPrice = null;
		_entrySide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		Unit? stopLossUnit = null;
		Unit? takeProfitUnit = null;

		if (UseStopLoss && StopLossPips > 0)
		{
			stopLossUnit = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);
		}

		if (UseTakeProfit && TakeProfitPips > 0)
		{
			takeProfitUnit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);
		}

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var kdj = new Stochastic
		{
			Length = KdjPeriod,
			KPeriod = SmoothK,
			DPeriod = SmoothD
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(kdj, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, kdj);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		// Process signals only on completed candles to match MetaTrader's behavior.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochastic = (StochasticValue)indicatorValue;
		if (stochastic.K is not decimal k || stochastic.D is not decimal d)
			return;

		var diff = k - d;

		var diffCrossUp = _previousDiff.HasValue && _previousDiff.Value < 0m && diff > 0m;
		var diffCrossDown = _previousDiff.HasValue && _previousDiff.Value > 0m && diff < 0m;
		var kRising = _previousK.HasValue && k > _previousK.Value;
		var kFalling = _previousK.HasValue && k < _previousK.Value;

		if (Position > 0m && (diffCrossDown || kFalling) && !HasActiveOrders())
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
		else if (Position < 0m && (diffCrossUp || kRising) && !HasActiveOrders())
		{
			var volume = Math.Abs(Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (Position == 0m && !HasActiveOrders())
		{
			var openLong = diffCrossUp || (diff > 0m && kRising);
			var openShort = diffCrossDown || (diff < 0m && kFalling);

			if (openLong && !openShort)
			{
				EnterPosition(Sides.Buy, candle.ClosePrice);
			}
			else if (openShort && !openLong)
			{
				EnterPosition(Sides.Sell, candle.ClosePrice);
			}
		}

		_previousK = k;
		_previousDiff = diff;
	}

	private void EnterPosition(Sides side, decimal price)
	{
		var volume = CalculateOrderVolume(price);
		if (volume <= 0m)
			return;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_entryPrice = price;
		_entrySide = side;
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var volume = BaseVolume;

		if (MaximumRisk > 0m && Leverage > 0 && price > 0m)
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (equity is decimal eq && eq > 0m)
			{
				var contractSize = Security?.ContractSize ?? 100000m;
				var marginPerLot = price * contractSize / Leverage;
				if (marginPerLot > 0m)
				{
					var riskLots = eq * MaximumRisk / marginPerLot;
					if (riskLots > volume)
						volume = riskLots;
				}
			}
		}

		if (DecreaseFactor > 0m && _lossStreak > 1)
		{
			var reduction = volume * _lossStreak / DecreaseFactor;
			volume -= reduction;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 0m;
			if (step <= 0m)
				step = 1m;

			var minVolume = security.MinVolume ?? step;
			var maxVolume = security.MaxVolume;

			var steps = decimal.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;

			if (volume < minVolume)
				volume = minVolume;

			if (maxVolume is decimal max && max > 0m && volume > max)
				volume = max;
		}

		if (volume <= 0m)
			volume = 1m;

		return volume;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
			return;

		var price = trade.Trade?.Price ?? order.Price;
		if (price is decimal fillPrice && fillPrice > 0m && order.Direction == _entrySide && Position != 0m)
		{
			// Update entry price with the actual fill to improve profit tracking accuracy.
			_entryPrice = fillPrice;
			return;
		}

		if (_entryPrice is not decimal entryPrice || _entrySide is null)
			return;

		if (Position != 0m)
			return;

		if (price is not decimal exitPrice || exitPrice <= 0m)
			return;

		var direction = _entrySide == Sides.Buy ? 1m : -1m;
		var gain = (exitPrice - entryPrice) * direction;

		if (gain > 0m)
		{
			_lossStreak = 0;
		}
		else if (gain < 0m)
		{
			_lossStreak++;
		}

		_entryPrice = null;
		_entrySide = null;
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State.IsActive())
				return true;
		}

		return false;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			step = 1m;
			for (var i = 0; i < decimals; i++)
				step /= 10m;
		}

		var decimalsCount = security.Decimals;
		var multiplier = (decimalsCount == 3 || decimalsCount == 5) ? 10m : 1m;

		return step * multiplier;
	}
}
