using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Cronex CCI indicator.
/// </summary>
public class CronexCciStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private EMA _fastMa;
	private EMA _slowMa;
	private Order _pendingOrder;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Fast smoothing period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothing period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CronexCciStrategy"/>.
	/// </summary>
	public CronexCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 25)
			.SetRange(5, 100)
			.SetDisplay("CCI Period", "CCI calculation length", "Indicators")
			;

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetRange(2, 50)
			.SetDisplay("Fast Period", "Fast smoothing period", "Indicators")
			;

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetRange(2, 100)
			.SetDisplay("Slow Period", "Slow smoothing period", "Indicators")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading");
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

		_prevFast = null;
		_prevSlow = null;
		_fastMa = _slowMa = null;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = _prevSlow = null;
		_pendingOrder = null;
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		_fastMa = new EMA { Length = FastPeriod };
		_slowMa = new EMA { Length = SlowPeriod };
		Indicators.Add(_fastMa);
		Indicators.Add(_slowMa);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;
		var fastResult = _fastMa.Process(cciValue, candle.OpenTime, true);
		var slowResult = _slowMa.Process(cciValue, candle.OpenTime, true);
		if (!fastResult.IsFormed || !slowResult.IsFormed) return;
		var fast = fastResult.ToDecimal();
		var slow = slowResult.ToDecimal();
		var buy = _prevFast is decimal oldFast && _prevSlow is decimal oldSlow && oldFast >= oldSlow && fast < slow;
		var sell = _prevFast is decimal oldFast2 && _prevSlow is decimal oldSlow2 && oldFast2 <= oldSlow2 && fast > slow;
		_prevFast = fast;
		_prevSlow = slow;
		if (!IsFormedAndOnlineAndAllowTrading() || (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))) return;
		if (buy)
		{
			if (Position < 0m && EnableShortExit) _pendingOrder = BuyMarket(-Position + (EnableLongEntry ? Volume : 0m));
			else if (Position == 0m && EnableLongEntry) _pendingOrder = BuyMarket(Volume);
		}
		else if (sell)
		{
			if (Position > 0m && EnableLongExit) _pendingOrder = SellMarket(Position + (EnableShortEntry ? Volume : 0m));
			else if (Position == 0m && EnableShortEntry) _pendingOrder = SellMarket(Volume);
		}
	}
}
