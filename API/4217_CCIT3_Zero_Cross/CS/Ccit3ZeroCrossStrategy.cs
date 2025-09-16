using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Operating modes supported by <see cref="Ccit3ZeroCrossStrategy"/>.
/// </summary>
public enum Ccit3Mode
{
	/// <summary>
	/// Classic CCIT3 calculation with persistent Tillson T3 smoothing.
	/// </summary>
	Simple,

	/// <summary>
	/// Non-recalculated CCIT3 variant that evaluates the Tillson chain only for the latest bar.
	/// </summary>
	NoRecalc,
}

/// <summary>
/// Applied price options compatible with the CCIT3 port.
/// </summary>
public enum CciAppliedPriceType
{
	/// <summary>
	/// Use candle close price.
	/// </summary>
	Close,

	/// <summary>
	/// Use candle open price.
	/// </summary>
	Open,

	/// <summary>
	/// Use candle high price.
	/// </summary>
	High,

	/// <summary>
	/// Use candle low price.
	/// </summary>
	Low,

	/// <summary>
	/// Use median price ((High + Low) / 2).
	/// </summary>
	Median,

	/// <summary>
	/// Use typical price ((High + Low + Close) / 3).
	/// </summary>
	Typical,

	/// <summary>
	/// Use weighted price ((High + Low + 2 * Close) / 4).
	/// </summary>
	Weighted,
}

/// <summary>
/// Port of the MetaTrader CCIT3 expert advisor that trades zero-line crosses of a Tillson-smoothed CCI.
/// </summary>
public class Ccit3ZeroCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingPoints;
	private readonly StrategyParam<bool> _tradeOverturn;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<CciAppliedPriceType> _cciPriceType;
	private readonly StrategyParam<int> _t3Period;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<Ccit3Mode> _mode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _maxDrawdownTarget;

	private CommodityChannelIndex? _cci;
	private decimal _pointValue;
	private decimal _alpha;
	private decimal _beta;
	private decimal _c1;
	private decimal _c2;
	private decimal _c3;
	private decimal _c4;
	private decimal _simpleE1;
	private decimal _simpleE2;
	private decimal _simpleE3;
	private decimal _simpleE4;
	private decimal _simpleE5;
	private decimal _simpleE6;
	private decimal? _previousT3;

	/// <summary>
	/// Initializes a new instance of <see cref="Ccit3ZeroCrossStrategy"/>.
	/// </summary>
	public Ccit3ZeroCrossStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1750m)
			.SetDisplay("Take Profit (pts)", "Take profit distance expressed in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(200m, 4000m, 100m);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss (pts)", "Stop loss distance expressed in points", "Risk");

		_trailingPoints = Param(nameof(TrailingPoints), 0m)
			.SetDisplay("Trailing Stop (pts)", "Trailing stop distance expressed in points", "Risk");

		_tradeOverturn = Param(nameof(TradeOverturn), false)
			.SetDisplay("Trade Overturn", "Reverse the position on opposite signals", "Trading");

		_cciPeriod = Param(nameof(CciPeriod), 285)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period used by the Commodity Channel Index", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(100, 400, 5);

		_cciPriceType = Param(nameof(CciPriceType), CciAppliedPriceType.Typical)
			.SetDisplay("CCI Price", "Applied price used for the CCI input", "Indicator");

		_t3Period = Param(nameof(T3Period), 60)
			.SetGreaterThanZero()
			.SetDisplay("T3 Period", "Tillson T3 smoothing period", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_volumeFactor = Param(nameof(VolumeFactor), 0.618m)
			.SetDisplay("T3 Volume Factor", "Tillson T3 volume factor (B coefficient)", "Indicator");

		_mode = Param(nameof(Mode), Ccit3Mode.Simple)
			.SetDisplay("Mode", "Choose between Simple and NoRecalc CCIT3", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for analysis", "General");

		_maxDrawdownTarget = Param(nameof(MaxDrawdownTarget), 0m)
			.SetDisplay("Max Drawdown Target", "Balance divisor for adaptive position sizing", "Risk");
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}


	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingPoints
	{
		get => _trailingPoints.Value;
		set => _trailingPoints.Value = value;
	}

	/// <summary>
	/// True to close and reverse the position on the opposite signal.
	/// </summary>
	public bool TradeOverturn
	{
		get => _tradeOverturn.Value;
		set => _tradeOverturn.Value = value;
	}

	/// <summary>
	/// Period applied to the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Applied price used as the CCI input.
	/// </summary>
	public CciAppliedPriceType CciPriceType
	{
		get => _cciPriceType.Value;
		set => _cciPriceType.Value = value;
	}

	/// <summary>
	/// Tillson T3 smoothing period.
	/// </summary>
	public int T3Period
	{
		get => _t3Period.Value;
		set => _t3Period.Value = value;
	}

	/// <summary>
	/// Tillson T3 volume factor (B coefficient).
	/// </summary>
	public decimal VolumeFactor
	{
		get => _volumeFactor.Value;
		set => _volumeFactor.Value = value;
	}

	/// <summary>
	/// Selected CCIT3 calculation mode.
	/// </summary>
	public Ccit3Mode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Balance divisor used to scale the order volume (set to zero to disable).
	/// </summary>
	public decimal MaxDrawdownTarget
	{
		get => _maxDrawdownTarget.Value;
		set => _maxDrawdownTarget.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_cci = null;
		_previousT3 = null;
		_simpleE1 = _simpleE2 = _simpleE3 = _simpleE4 = _simpleE5 = _simpleE6 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
			_pointValue = 1m;

		InitializeSmoothing();

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
		};

		_previousT3 = null;

		Volume = AlignVolume(OrderVolume);

		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute) : null;
		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute) : null;
		var trailingUnit = TrailingPoints > 0m ? new Unit(TrailingPoints * _pointValue, UnitTypes.Absolute) : null;

		StartProtection(
			takeProfit: takeProfitUnit,
			stopLoss: stopLossUnit,
			trailingStop: trailingUnit,
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var cci = _cci;
		if (cci == null)
			return;

		var price = GetAppliedPrice(candle, CciPriceType);
		var cciValue = cci.Process(price, candle.OpenTime, true).ToDecimal();

		if (!cci.IsFormed)
			return;

		var currentT3 = CalculateT3(cciValue);
		var previousT3 = _previousT3;
		_previousT3 = currentT3;

		if (!previousT3.HasValue)
			return;

		var crossToLong = currentT3 <= 0m && previousT3.Value > 0m;
		var crossToShort = currentT3 >= 0m && previousT3.Value < 0m;

		if (!crossToLong && !crossToShort)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		if (crossToLong)
		{
			TryEnterLong(volume);
		}
		else if (crossToShort)
		{
			TryEnterShort(volume);
		}
	}

	private decimal CalculateT3(decimal cciValue)
	{
		if (Mode == Ccit3Mode.Simple)
		{
			_simpleE1 = _alpha * cciValue + _beta * _simpleE1;
			_simpleE2 = _alpha * _simpleE1 + _beta * _simpleE2;
			_simpleE3 = _alpha * _simpleE2 + _beta * _simpleE3;
			_simpleE4 = _alpha * _simpleE3 + _beta * _simpleE4;
			_simpleE5 = _alpha * _simpleE4 + _beta * _simpleE5;
			_simpleE6 = _alpha * _simpleE5 + _beta * _simpleE6;

			return _c1 * _simpleE6 + _c2 * _simpleE5 + _c3 * _simpleE4 + _c4 * _simpleE3;
		}

		var e1 = _alpha * cciValue;
		var e2 = _alpha * e1;
		var e3 = _alpha * e2;
		var e4 = _alpha * e3;
		var e5 = _alpha * e4;
		var e6 = _alpha * e5;

		return _c1 * e6 + _c2 * e5 + _c3 * e4 + _c4 * e3;
	}

	private void TryEnterLong(decimal volume)
	{
		var position = Position;

		if (position > 0m)
			return;

		if (position < 0m)
		{
			if (!TradeOverturn || !AllowShortExit)
				return;

			CancelActiveOrders();
			BuyMarket(-position);
		}
		else
		{
			CancelActiveOrders();
		}

		if (AllowLongEntry)
			BuyMarket(volume);
	}

	private void TryEnterShort(decimal volume)
	{
		var position = Position;

		if (position < 0m)
			return;

		if (position > 0m)
		{
			if (!TradeOverturn || !AllowLongExit)
				return;

			CancelActiveOrders();
			SellMarket(position);
		}
		else
		{
			CancelActiveOrders();
		}

		if (AllowShortEntry)
			SellMarket(volume);
	}

	private decimal GetTradeVolume()
	{
		var volume = OrderVolume;

		if (MaxDrawdownTarget > 0m)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (balance.HasValue && balance.Value > 0m)
			{
				volume = volume * balance.Value / MaxDrawdownTarget;
			}
		}

		return AlignVolume(volume);
	}

	private decimal AlignVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.VolumeMin ?? 0m;
		var max = security.VolumeMax ?? decimal.MaxValue;

		if (min > 0m && volume < min)
			volume = min;

		if (max > 0m && volume > max)
			volume = max;

		if (step > 0m)
		{
			var steps = Math.Round(volume / step);
			volume = steps * step;
		}

		return volume;
	}

	private void InitializeSmoothing()
	{
		_simpleE1 = _simpleE2 = _simpleE3 = _simpleE4 = _simpleE5 = _simpleE6 = 0m;

		var period = Math.Max(1, (T3Period + 1) / 2);
		_alpha = 2m / (period + 1m);
		_beta = 1m - _alpha;

		var b = VolumeFactor;
		var b2 = b * b;
		var b3 = b2 * b;

		_c1 = -b3;
		_c2 = 3m * (b2 + b3);
		_c3 = -3m * (2m * b2 + b + b3);
		_c4 = 1m + 3m * b + b3 + 3m * b2;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, CciAppliedPriceType priceType)
	{
		return priceType switch
		{
			CciAppliedPriceType.Close => candle.ClosePrice,
			CciAppliedPriceType.Open => candle.OpenPrice,
			CciAppliedPriceType.High => candle.HighPrice,
			CciAppliedPriceType.Low => candle.LowPrice,
			CciAppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CciAppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CciAppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}
}
