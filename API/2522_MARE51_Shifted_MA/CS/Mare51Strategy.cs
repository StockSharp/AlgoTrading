using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MARE5.1 strategy that trades shifted SMA crossovers with time filtering.
/// </summary>
public class Mare51Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _movingAverageShift;
	private readonly StrategyParam<int> _sessionOpenHour;
	private readonly StrategyParam<int> _sessionCloseHour;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fastSma;
	private SMA _slowSma;
	private decimal?[] _fastBuffer;
	private decimal?[] _slowBuffer;
	private ICandleMessage _previousCandle;
	private decimal _pipSize;

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int MovingAverageShift
	{
		get => _movingAverageShift.Value;
		set => _movingAverageShift.Value = value;
	}

	public int SessionOpenHour
	{
		get => _sessionOpenHour.Value;
		set => _sessionOpenHour.Value = value;
	}

	public int SessionCloseHour
	{
		get => _sessionCloseHour.Value;
		set => _sessionCloseHour.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Mare51Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
			.SetDisplay("Volume", "Default order volume", "Trading")
			.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 35m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in adjusted pips", "Risk")
			.SetNotNegative();

		_stopLossPips = Param(nameof(StopLossPips), 55m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in adjusted pips", "Risk")
			.SetNotNegative();

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 79)
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators")
			.SetGreaterThanZero();

		_movingAverageShift = Param(nameof(MovingAverageShift), 4)
			.SetDisplay("MA Shift", "Forward shift applied to both SMAs", "Indicators")
			.SetNotNegative();

		_sessionOpenHour = Param(nameof(SessionOpenHour), 2)
			.SetDisplay("Session Open Hour", "Inclusive start hour for trading", "Session")
			.SetRange(0, 23);

		_sessionCloseHour = Param(nameof(SessionCloseHour), 3)
			.SetDisplay("Session Close Hour", "Inclusive end hour for trading", "Session")
			.SetRange(0, 23);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle data type", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_fastSma = null;
		_slowSma = null;
		_fastBuffer = null;
		_slowBuffer = null;
		_previousCandle = null;
		_pipSize = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SessionOpenHour >= SessionCloseHour)
			throw new InvalidOperationException("SessionOpenHour must be less than SessionCloseHour.");

		Volume = TradeVolume;

		_fastSma = new SMA { Length = FastPeriod };
		_slowSma = new SMA { Length = SlowPeriod };

		_fastBuffer = new decimal?[MovingAverageShift + 6];
		_slowBuffer = new decimal?[MovingAverageShift + 6];

		_pipSize = CalculatePipSize();
		var takeProfitUnit = TakeProfitPips > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: new Unit(0m);
		var stopLossUnit = StopLossPips > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: new Unit(0m);

		StartProtection(takeProfitUnit, stopLossUnit, useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_fastBuffer == null || _slowBuffer == null)
			return;

		// Shift raw SMA values so we can later access shifted indexes.
		for (var i = _fastBuffer.Length - 1; i > 0; i--)
		{
			_fastBuffer[i] = _fastBuffer[i - 1];
			_slowBuffer[i] = _slowBuffer[i - 1];
		}

		_fastBuffer[0] = fastValue;
		_slowBuffer[0] = slowValue;

		var previousCandle = _previousCandle;
		_previousCandle = candle;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (previousCandle == null)
			return;

		if (_fastSma == null || _slowSma == null)
			return;

		if (!_fastSma.IsFormed || !_slowSma.IsFormed)
			return;

		var fast0 = GetShiftedValue(_fastBuffer, 0);
		var fast2 = GetShiftedValue(_fastBuffer, 2);
		var fast5 = GetShiftedValue(_fastBuffer, 5);
		var slow0 = GetShiftedValue(_slowBuffer, 0);
		var slow2 = GetShiftedValue(_slowBuffer, 2);
		var slow5 = GetShiftedValue(_slowBuffer, 5);

		if (fast0 is not decimal f0 || fast2 is not decimal f2 || fast5 is not decimal f5 ||
			slow0 is not decimal s0 || slow2 is not decimal s2 || slow5 is not decimal s5)
		{
			return;
		}

		if (!IsWithinSession(candle.OpenTime))
			return;

		var point = Security?.PriceStep ?? 0m;
		if (point <= 0m)
			point = 0.0001m;

		var bearishPrevious = previousCandle.ClosePrice < previousCandle.OpenPrice;
		var bullishPrevious = previousCandle.ClosePrice > previousCandle.OpenPrice;

		var sellSignal = (s0 - f0) >= point && (f2 - s2) >= point && (f5 - s5) >= point && bearishPrevious;
		var buySignal = (f0 - s0) >= point && (s2 - f2) >= point && (s5 - f5) >= point && bullishPrevious;

		if (Position != 0)
			return;

		if (sellSignal)
		{
			// Enter short when slow SMA overtakes the fast SMA and previous bars confirm the reversal.
			SellMarket(TradeVolume);
		}
		else if (buySignal)
		{
			// Enter long when fast SMA overtakes the slow SMA and previous bars confirm the reversal.
			BuyMarket(TradeVolume);
		}
	}

	private decimal? GetShiftedValue(decimal?[] buffer, int index)
	{
		var targetIndex = index + MovingAverageShift;
		if (buffer == null)
			return null;
		if (targetIndex < 0 || targetIndex >= buffer.Length)
			return null;
		return buffer[targetIndex];
	}

	private bool IsWithinSession(DateTimeOffset time)
	{
		var hour = time.Hour;
		return hour >= SessionOpenHour && hour <= SessionCloseHour;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var scale = GetDecimalScale(step);
		return (scale == 3 || scale == 5) ? step * 10m : step;
	}

	private static int GetDecimalScale(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
