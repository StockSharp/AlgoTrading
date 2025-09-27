namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Conversion of the MetaTrader 4 expert advisor "Starter" (2005 release).
/// Combines a Laguerre RSI proxy, EMA slope confirmation and a CCI filter.
/// Implements adaptive lot sizing inspired by the original LotsOptimized routine.
/// </summary>
public class Starter2005Strategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _riskDivider;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _laguerreEntryTolerance;
	private readonly StrategyParam<decimal> _laguerreExitHigh;
	private readonly StrategyParam<decimal> _laguerreExitLow;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema = null!;
	private CommodityChannelIndex _cci = null!;

	private decimal? _previousMa;
	private decimal _lagL0;
	private decimal _lagL1;
	private decimal _lagL2;
	private decimal _lagL3;
	private bool _laguerreFormed;

	private decimal? _entryPrice;
	private decimal _entryVolume;
	private Sides? _entrySide;
	private int _consecutiveLosses;

	/// <summary>
	/// Initializes a new instance of the <see cref="Starter2005Strategy"/> class.
	/// </summary>
	public Starter2005Strategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial lot size used when risk-based sizing is unavailable", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_maximumRisk = Param(nameof(MaximumRisk), 0.036m)
			.SetNotNegative()
			.SetDisplay("Maximum Risk", "Fraction of account equity considered for sizing", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 0.1m, 0.005m);

		_riskDivider = Param(nameof(RiskDivider), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Divider", "Divisor applied to risk capital (mimics the original /500 rule)", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 50m);

		_decreaseFactor = Param(nameof(DecreaseFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Decrease Factor", "Lot reduction factor after consecutive losses", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of the exponential moving average applied to median price", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 1);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index lookback length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_cciThreshold = Param(nameof(CciThreshold), 5m)
			.SetNotNegative()
			.SetDisplay("CCI Threshold", "Absolute CCI level required for signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 50m, 1m);

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.66m)
			.SetRange(0.1m, 0.9m)
			.SetDisplay("Laguerre Gamma", "Smoothing factor of the Laguerre RSI filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.3m, 0.9m, 0.05m);

		_laguerreEntryTolerance = Param(nameof(LaguerreEntryTolerance), 0.02m)
			.SetRange(0m, 0.3m)
			.SetDisplay("Laguerre Entry Tolerance", "Closeness to 0/1 required to mimic the original equality checks", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.1m, 0.005m);

		_laguerreExitHigh = Param(nameof(LaguerreExitHigh), 0.9m)
			.SetRange(0.5m, 1m)
			.SetDisplay("Laguerre Exit High", "Upper exit level for long positions", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0.6m, 1m, 0.05m);

		_laguerreExitLow = Param(nameof(LaguerreExitLow), 0.1m)
			.SetRange(0m, 0.5m)
			.SetDisplay("Laguerre Exit Low", "Lower exit level for short positions", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0m, 0.4m, 0.05m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance in price points before profit is locked", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe processed by the strategy", "General");
	}

	/// <summary>
	/// Base lot size used when the risk model produces a smaller value.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of the portfolio considered for risk-based sizing.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Divider applied to the risk capital (mirrors the /500 rule).
	/// </summary>
	public decimal RiskDivider
	{
		get => _riskDivider.Value;
		set => _riskDivider.Value = value;
	}

	/// <summary>
	/// Lot reduction factor after consecutive losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// EMA length applied to median price.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// CCI lookback period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI level required for entry.
	/// </summary>
	public decimal CciThreshold
	{
		get => _cciThreshold.Value;
		set => _cciThreshold.Value = value;
	}

	/// <summary>
	/// Laguerre smoothing factor (gamma).
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	/// <summary>
	/// Tolerance applied when checking Laguerre against 0 or 1.
	/// </summary>
	public decimal LaguerreEntryTolerance
	{
		get => _laguerreEntryTolerance.Value;
		set => _laguerreEntryTolerance.Value = value;
	}

	/// <summary>
	/// Laguerre exit threshold for long positions.
	/// </summary>
	public decimal LaguerreExitHigh
	{
		get => _laguerreExitHigh.Value;
		set => _laguerreExitHigh.Value = value;
	}

	/// <summary>
	/// Laguerre exit threshold for short positions.
	/// </summary>
	public decimal LaguerreExitLow
	{
		get => _laguerreExitLow.Value;
		set => _laguerreExitLow.Value = value;
	}

	/// <summary>
	/// Profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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

		_ema = null!;
		_cci = null!;
		_previousMa = null;
		_lagL0 = _lagL1 = _lagL2 = _lagL3 = 0m;
		_laguerreFormed = false;
		_entryPrice = null;
		_entryVolume = 0m;
		_entrySide = null;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate median price to replicate PRICE_MEDIAN from MetaTrader.
		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var maValue = _ema.Process(medianPrice, candle.OpenTime, true);
		var cciValue = _cci.Process(candle.ClosePrice, candle.OpenTime, true);

		var ma = maValue.ToDecimal();
		var cci = cciValue.ToDecimal();

		if (!_ema.IsFormed || !_cci.IsFormed)
		{
			_previousMa = ma;
			return;
		}

		var laguerre = CalculateLaguerre(candle.ClosePrice);
		if (!_laguerreFormed)
		{
			_previousMa = ma;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousMa = ma;
			return;
		}

		var previousMa = _previousMa;
		_previousMa = ma;
		if (!previousMa.HasValue)
			return;

		var maRising = ma > previousMa.Value;
		var maFalling = ma < previousMa.Value;
		var entryTolerance = LaguerreEntryTolerance;
		var takeProfitDistance = GetTakeProfitDistance();
		var price = GetDecisionPrice(candle);

		if (Position == 0m && !HasActiveOrders())
		{
			if (maRising && laguerre <= entryTolerance && cci < -CciThreshold && AllowLong())
			{
				var volume = CalculateOrderVolume(price);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_entrySide = Sides.Buy;
					_entryPrice = price;
					_entryVolume = volume;
					LogInfo($"Opening long. Laguerre={laguerre:F4}, CCI={cci:F2}, EMA rising.");
				}
			}
			else if (maFalling && laguerre >= 1m - entryTolerance && cci > CciThreshold && AllowShort())
			{
				var volume = CalculateOrderVolume(price);
				if (volume > 0m)
				{
					SellMarket(volume);
					_entrySide = Sides.Sell;
					_entryPrice = price;
					_entryVolume = volume;
					LogInfo($"Opening short. Laguerre={laguerre:F4}, CCI={cci:F2}, EMA falling.");
				}
			}
		}

		if (_entrySide == Sides.Buy && Position > 0m && _entryPrice.HasValue)
		{
			var gain = price - _entryPrice.Value;
			if ((LaguerreExitHigh > 0m && laguerre >= LaguerreExitHigh) || (takeProfitDistance > 0m && gain >= takeProfitDistance))
			{
				var volume = Math.Abs(Position);
				if (volume <= 0m)
					volume = _entryVolume;

				if (volume > 0m && !HasActiveOrders())
				{
					SellMarket(volume);
					RegisterTradeResult(gain);
					ResetPositionState();
					LogInfo($"Closing long. Laguerre={laguerre:F4}, gain={gain:F5}.");
				}
			}
		}
		else if (_entrySide == Sides.Sell && Position < 0m && _entryPrice.HasValue)
		{
			var gain = _entryPrice.Value - price;
			if ((LaguerreExitLow > 0m && laguerre <= LaguerreExitLow) || (takeProfitDistance > 0m && gain >= takeProfitDistance))
			{
				var volume = Math.Abs(Position);
				if (volume <= 0m)
					volume = _entryVolume;

				if (volume > 0m && !HasActiveOrders())
				{
					BuyMarket(volume);
					RegisterTradeResult(gain);
					ResetPositionState();
					LogInfo($"Closing short. Laguerre={laguerre:F4}, gain={gain:F5}.");
				}
			}
		}
		else if (Position == 0m && !HasActiveOrders())
		{
			ResetPositionState();
		}
	}

	private decimal CalculateLaguerre(decimal price)
	{
		var gamma = LaguerreGamma;
		var l0Prev = _lagL0;
		var l1Prev = _lagL1;
		var l2Prev = _lagL2;
		var l3Prev = _lagL3;

		_lagL0 = (1m - gamma) * price + gamma * l0Prev;
		_lagL1 = -gamma * _lagL0 + l0Prev + gamma * l1Prev;
		_lagL2 = -gamma * _lagL1 + l1Prev + gamma * l2Prev;
		_lagL3 = -gamma * _lagL2 + l2Prev + gamma * l3Prev;

		decimal cu = 0m;
		decimal cd = 0m;

		if (_lagL0 >= _lagL1)
			cu = _lagL0 - _lagL1;
		else
			cd = _lagL1 - _lagL0;

		if (_lagL1 >= _lagL2)
			cu += _lagL1 - _lagL2;
		else
			cd += _lagL2 - _lagL1;

		if (_lagL2 >= _lagL3)
			cu += _lagL2 - _lagL3;
		else
			cd += _lagL3 - _lagL2;

		var denominator = cu + cd;
		var result = denominator == 0m ? 0m : cu / denominator;

		_laguerreFormed = true;
		return result;
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		var volume = BaseVolume;

		if (MaximumRisk > 0m && RiskDivider > 0m)
		{
			var portfolio = Portfolio;
			var equity = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
			if (equity > 0m && price > 0m)
			{
				var riskVolume = equity * MaximumRisk / RiskDivider;
				riskVolume /= price;
				if (riskVolume > volume)
					volume = riskVolume;
			}
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
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

	private decimal GetTakeProfitDistance()
	{
		if (TakeProfitPoints <= 0m)
			return 0m;

		var point = Security?.PriceStep ?? 0m;
		if (point <= 0m)
		{
			var decimals = Security?.Decimals ?? 4;
			point = 1m;
			for (var i = 0; i < decimals; i++)
				point /= 10m;
		}

		return TakeProfitPoints * point;
	}

	private decimal GetDecisionPrice(ICandleMessage candle)
	{
		var last = Security?.LastPrice;
		if (last is decimal lastPrice && lastPrice > 0m)
			return lastPrice;

		if (candle.ClosePrice > 0m)
			return candle.ClosePrice;

		return candle.OpenPrice;
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

	private void RegisterTradeResult(decimal gain)
	{
		if (gain > 0m)
		{
			if (_consecutiveLosses > 0)
				LogInfo($"Profit resets loss streak of {_consecutiveLosses} trades.");

			_consecutiveLosses = 0;
		}
		else if (gain < 0m)
		{
			_consecutiveLosses++;
			LogInfo($"Loss streak increased to {_consecutiveLosses}.");
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_entryVolume = 0m;
		_entrySide = null;
	}
}
