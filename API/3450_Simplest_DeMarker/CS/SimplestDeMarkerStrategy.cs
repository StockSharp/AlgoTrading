using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplest DeMarker Strategy.
/// Uses the DeMarker oscillator to detect overbought and oversold zones and trades on crossovers.
/// </summary>
public class SimplestDeMarkerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<bool> _tradeOnBarOpen;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousDeMarker;
	private int _holdState;
	private int _pendingDirection;
	private bool _waitForNextBar;
	private DateTimeOffset? _lastCandleTime;

	/// <summary>
	/// Order volume.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Period for the DeMarker indicator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for the DeMarker indicator.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level for the DeMarker indicator.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Execute entries at the open of the next bar.
	/// </summary>
	public bool TradeOnBarOpen
	{
		get => _tradeOnBarOpen.Value;
		set => _tradeOnBarOpen.Value = value;
	}

	/// <summary>
	/// Stop-loss size in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit size in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Initializes a new instance of the <see cref="SimplestDeMarkerStrategy"/>.
	/// </summary>
	public SimplestDeMarkerStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "General");

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "Period for the DeMarker indicator", "Indicator")
		.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 0.7m)
		.SetDisplay("Overbought Level", "Upper threshold for DeMarker", "Indicator")
		.SetRange(0.5m, 0.9m)
		.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 0.3m)
		.SetDisplay("Oversold Level", "Lower threshold for DeMarker", "Indicator")
		.SetRange(0.1m, 0.5m)
		.SetCanOptimize(true);

		_tradeOnBarOpen = Param(nameof(TradeOnBarOpen), false)
		.SetDisplay("Trade On Bar Open", "Place orders on the next bar open", "Trading Logic");

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
		.SetDisplay("Stop Loss Points", "Stop-loss size in price points", "Risk Management")
		.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150)
		.SetDisplay("Take Profit Points", "Take-profit size in price points", "Risk Management")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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

		_previousDeMarker = null;
		_holdState = 0;
		_pendingDirection = 0;
		_waitForNextBar = false;
		_lastCandleTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		var deMarker = new DeMarker { Length = DeMarkerPeriod };

		subscription
		.Bind(deMarker, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}

		var step = Security?.PriceStep ?? 1m;
		var takeProfit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : new Unit();
		var stopLoss = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : new Unit();

		StartProtection(
		takeProfit: takeProfit,
		stopLoss: stopLoss,
		isStopTrailing: false,
		useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var isNewBar = !_lastCandleTime.HasValue || candle.OpenTime > _lastCandleTime;

		if (_pendingDirection != 0 && _waitForNextBar && isNewBar)
		{
			ExecuteSignal(_pendingDirection);
			_pendingDirection = 0;
			_waitForNextBar = false;
		}

		if (_previousDeMarker is null)
		{
			_previousDeMarker = deMarkerValue;
			_lastCandleTime = candle.OpenTime;
			return;
		}

		if (_previousDeMarker > OverboughtLevel)
		{
			_holdState = 1;
		}
		else if (_previousDeMarker < OversoldLevel)
		{
			_holdState = -1;
		}
		else
		{
			_holdState = 0;
		}

		var signal = 0;

		if (_holdState == 1 && deMarkerValue < OverboughtLevel && _previousDeMarker > OverboughtLevel)
		{
			signal = -1;
		}
		else if (_holdState == -1 && deMarkerValue > OversoldLevel && _previousDeMarker < OversoldLevel)
		{
			signal = 1;
		}

		if (signal != 0 && Position == 0)
		{
			if (TradeOnBarOpen)
			{
				_pendingDirection = signal;
				_waitForNextBar = true;
			}
			else
			{
				ExecuteSignal(signal);
			}
		}

		_previousDeMarker = deMarkerValue;
		_lastCandleTime = candle.OpenTime;
	}

	private void ExecuteSignal(int direction)
	{
		if (direction == 0)
		{
			return;
		}

		if (direction > 0 && Position == 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume);
		}
		else if (direction < 0 && Position == 0)
		{
			CancelActiveOrders();
			SellMarket(Volume);
		}
	}
}
