using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on two moving averages with CCI filter and ATR stop-loss.
/// Opens long when fast MA crosses above slow MA and CCI crosses above zero.
/// Opens short when fast MA crosses below slow MA and CCI crosses below zero.
/// Closes positions on opposite MA crossover or when ATR-based stop is hit.
/// </summary>
public class Ma2CciStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevCci;
	private bool _isInitialized;

	private decimal _entryPrice;
	private decimal _stopLoss;
	private bool _isLong;

	/// <summary>
	/// Fast MA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow MA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for stop-loss.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Ma2CciStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the fast moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the slow moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_cciPeriod = Param(nameof(CciPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 15, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR stop-loss", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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

		_prevFast = 0;
		_prevSlow = 0;
		_prevCci = 0;
		_isInitialized = false;

		_entryPrice = 0;
		_stopLoss = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var fastMa = new SMA { Length = FastMaPeriod };
		var slowMa = new SMA { Length = SlowMaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		// Subscribe to candle data and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, cci, atr, ProcessCandle)
			.Start();

		// Chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);

			var cciArea = CreateChartArea();
			if (cciArea != null)
				DrawIndicator(cciArea, cci);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cciValue, decimal atrValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Initialize on first complete values
		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevCci = cciValue;
			_isInitialized = true;
			return;
		}

		// Stop-loss check
		if (Position > 0 && candle.ClosePrice <= _stopLoss)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && candle.ClosePrice >= _stopLoss)
		{
			BuyMarket(Math.Abs(Position));
		}

		var isFastAbove = fast > slow;
		var wasFastAbove = _prevFast > _prevSlow;
		var isCciAbove = cciValue > 0;
		var wasCciAbove = _prevCci > 0;

		// Close positions on opposite MA crossover
		if (Position > 0 && !isFastAbove && wasFastAbove)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && isFastAbove && !wasFastAbove)
		{
			BuyMarket(Math.Abs(Position));
		}
		// Open long when MA and CCI cross upward
		else if (Position <= 0 && isFastAbove && !wasFastAbove && isCciAbove && !wasCciAbove)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_isLong = true;
			_entryPrice = candle.ClosePrice;
			_stopLoss = candle.ClosePrice - atrValue;
		}
		// Open short when MA and CCI cross downward
		else if (Position >= 0 && !isFastAbove && wasFastAbove && !isCciAbove && wasCciAbove)
		{
			SellMarket(Volume + Math.Abs(Position));
			_isLong = false;
			_entryPrice = candle.ClosePrice;
			_stopLoss = candle.ClosePrice + atrValue;
		}

		// Update previous values
		_prevFast = fast;
		_prevSlow = slow;
		_prevCci = cciValue;
	}
}
