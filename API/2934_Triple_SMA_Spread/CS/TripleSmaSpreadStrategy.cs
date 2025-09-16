using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple SMA strategy using configurable shifts and spread filters.
/// Converts the original MT5 expert advisor that trades when three simple moving averages diverge by a minimum spread.
/// </summary>
public class TripleSmaSpreadStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _middleShift;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<decimal> _maSpreadPips;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _middleMa = null!;
	private SimpleMovingAverage _slowMa = null!;

	private decimal?[] _fastHistory = Array.Empty<decimal?>();
	private decimal?[] _middleHistory = Array.Empty<decimal?>();
	private decimal?[] _slowHistory = Array.Empty<decimal?>();

	private decimal _pipSize;
	private decimal _spreadPrice;
	private decimal _halfSpreadPrice;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the fast SMA in finished bars.
	/// </summary>
	public int FastMaShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Middle SMA period.
	/// </summary>
	public int MiddleMaPeriod
	{
		get => _middlePeriod.Value;
		set => _middlePeriod.Value = value;
	}

	/// <summary>
	/// Shift for the middle SMA in finished bars.
	/// </summary>
	public int MiddleMaShift
	{
		get => _middleShift.Value;
		set => _middleShift.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the slow SMA in finished bars.
	/// </summary>
	public int SlowMaShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Minimal spread between consecutive SMAs expressed in pips.
	/// </summary>
	public decimal MaSpreadPips
	{
		get => _maSpreadPips.Value;
		set => _maSpreadPips.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="TripleSmaSpreadStrategy"/>.
	/// </summary>
	public TripleSmaSpreadStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume of each market order", "Trading");

		_fastPeriod = Param(nameof(FastMaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Period", "Length of the fast moving average", "Indicators");

		_fastShift = Param(nameof(FastMaShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Fast SMA Shift", "Bars to shift the fast SMA", "Indicators");

		_middlePeriod = Param(nameof(MiddleMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Middle SMA Period", "Length of the middle moving average", "Indicators");

		_middleShift = Param(nameof(MiddleMaShift), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Middle SMA Shift", "Bars to shift the middle SMA", "Indicators");

		_slowPeriod = Param(nameof(SlowMaPeriod), 29)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Length of the slow moving average", "Indicators");

		_slowShift = Param(nameof(SlowMaShift), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slow SMA Shift", "Bars to shift the slow SMA", "Indicators");

		_maSpreadPips = Param(nameof(MaSpreadPips), 10m)
			.SetGreaterOrEqualZero()
			.SetDisplay("MA Spread (pips)", "Minimal distance between consecutive SMAs", "Trading logic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for analysis", "General");
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

		_fastMa = null!;
		_middleMa = null!;
		_slowMa = null!;

		_fastHistory = Array.Empty<decimal?>();
		_middleHistory = Array.Empty<decimal?>();
		_slowHistory = Array.Empty<decimal?>();

		_pipSize = 0m;
		_spreadPrice = 0m;
		_halfSpreadPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_middleMa = new SimpleMovingAverage { Length = MiddleMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };

		_fastHistory = new decimal?[Math.Max(1, FastMaShift + 1)];
		_middleHistory = new decimal?[Math.Max(1, MiddleMaShift + 1)];
		_slowHistory = new decimal?[Math.Max(1, SlowMaShift + 1)];

		_pipSize = CalculatePipSize();
		_spreadPrice = MaSpreadPips * _pipSize;
		_halfSpreadPrice = _spreadPrice / 2m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _middleMa, _slowMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _middleMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal middleValue, decimal slowValue)
	{
		// Work only with finished candles to mirror the MT5 implementation.
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that indicators are formed and strategy is allowed to trade.
		if (!_fastMa.IsFormed || !_middleMa.IsFormed || !_slowMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Maintain short history buffers to emulate the indicator shift parameters from MT5.
		ShiftHistory(_fastHistory, fastValue);
		ShiftHistory(_middleHistory, middleValue);
		ShiftHistory(_slowHistory, slowValue);

		if (_fastHistory[FastMaShift] is not decimal fastShifted ||
			_middleHistory[MiddleMaShift] is not decimal middleShifted ||
			_slowHistory[SlowMaShift] is not decimal slowShifted)
		{
			return;
		}

		// Exit rules: close long if fast SMA drops below middle SMA minus half spread, close short on the opposite condition.
		if (Position > 0 && fastShifted < middleShifted - _halfSpreadPrice)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && fastShifted > middleShifted + _halfSpreadPrice)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		// Entry rules: require a full spread separation between all three moving averages.
		var bullish = fastShifted > middleShifted + _spreadPrice && middleShifted > slowShifted + _spreadPrice;
		var bearish = fastShifted < middleShifted - _spreadPrice && middleShifted < slowShifted - _spreadPrice;

		if (bullish && Position <= 0)
		{
			var volume = Volume;
			if (Position < 0)
				volume += Math.Abs(Position);

			if (volume > 0m)
				BuyMarket(volume);

			return;
		}

		if (bearish && Position >= 0)
		{
			var volume = Volume;
			if (Position > 0)
				volume += Position;

			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			step = Security?.Step ?? 0m;

		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals;

		return decimals is 3 or 5 ? step * 10m : step;
	}

	private static void ShiftHistory(decimal?[] buffer, decimal value)
	{
		for (var i = buffer.Length - 1; i > 0; i--)
		{
			buffer[i] = buffer[i - 1];
		}

		if (buffer.Length > 0)
			buffer[0] = value;
	}
}
