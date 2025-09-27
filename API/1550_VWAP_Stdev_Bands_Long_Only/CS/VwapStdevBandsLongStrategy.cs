using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP strategy with standard deviation bands, long only.
/// </summary>
public class VwapStdevBandsLongStrategy : Strategy
{
	private readonly StrategyParam<decimal> _devUp;
	private readonly StrategyParam<decimal> _devDown;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<int> _gapMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _sessionDate;
	private decimal _vwapSum;
	private decimal _volSum;
	private decimal _v2Sum;
	private decimal _prevClose;
	private decimal _prevLower;
	private bool _hasPrev;
	private decimal _lastEntryPrice;
	private DateTimeOffset? _lastEntryTime;

	/// <summary>
	/// Standard deviation multiplier above VWAP.
	/// </summary>
	public decimal DevUp { get => _devUp.Value; set => _devUp.Value = value; }

	/// <summary>
	/// Standard deviation multiplier below VWAP.
	/// </summary>
	public decimal DevDown { get => _devDown.Value; set => _devDown.Value = value; }

	/// <summary>
	/// Profit target in price units.
	/// </summary>
	public decimal ProfitTarget { get => _profitTarget.Value; set => _profitTarget.Value = value; }

	/// <summary>
	/// Gap between entries in minutes.
	/// </summary>
	public int GapMinutes { get => _gapMinutes.Value; set => _gapMinutes.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapStdevBandsLongStrategy()
	{
		_devUp = Param(nameof(DevUp), 1.28m)
			.SetGreaterThanZero()
			.SetDisplay("Stdev Up", "Std dev above", "Parameters");

		_devDown = Param(nameof(DevDown), 1.28m)
			.SetGreaterThanZero()
			.SetDisplay("Stdev Down", "Std dev below", "Parameters");

		_profitTarget = Param(nameof(ProfitTarget), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Target in price", "Parameters");

		_gapMinutes = Param(nameof(GapMinutes), 15)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Gap Minutes", "Gap before new order", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_sessionDate = default;
		_vwapSum = 0m;
		_volSum = 0m;
		_v2Sum = 0m;
		_prevClose = 0m;
		_prevLower = 0m;
		_hasPrev = false;
		_lastEntryPrice = 0m;
		_lastEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.UtcDateTime.Date;
		var volume = candle.TotalVolume ?? 0m;
		var price = (candle.HighPrice + candle.LowPrice) / 2m;

		if (date != _sessionDate)
		{
			_sessionDate = date;
			_vwapSum = price * volume;
			_volSum = volume;
			_v2Sum = volume * price * price;
			_hasPrev = false;
		}
		else
		{
			_vwapSum += price * volume;
			_volSum += volume;
			_v2Sum += volume * price * price;
		}

		if (_volSum == 0m)
			return;

		var vwap = _vwapSum / _volSum;
		var variance = _v2Sum / _volSum - vwap * vwap;
		var dev = (decimal)Math.Sqrt((double)Math.Max(variance, 0m));
		var lower = vwap - DevDown * dev;

		var canEnter = !_lastEntryTime.HasValue || candle.OpenTime - _lastEntryTime >= TimeSpan.FromMinutes(GapMinutes);
		var crossedLower = _hasPrev && _prevClose >= _prevLower && candle.ClosePrice < lower;

		if (crossedLower && canEnter)
		{
			_lastEntryPrice = candle.ClosePrice;
			_lastEntryTime = candle.OpenTime;
			BuyMarket();
		}

		if (Position > 0 && candle.ClosePrice - _lastEntryPrice >= ProfitTarget)
		{
			SellMarket();
			_lastEntryTime = null;
		}

		_prevClose = candle.ClosePrice;
		_prevLower = lower;
		_hasPrev = true;
	}
}

