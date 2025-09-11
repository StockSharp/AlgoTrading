using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy that buys when price is above the trend SMA and RSI crosses above the oversold level.
/// ATR-based stop loss and take profit manage the position.
/// </summary>
public class NseIndexStrategyWithEntryExitMarkersStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLoss;
	private decimal _takeProfit;
	private decimal _prevRsi;
	private bool _isRsiInitialized;

	/// <summary>
	/// SMA period for trend filtering.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level for RSI.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss and take profit.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref=\"NseIndexStrategyWithEntryExitMarkersStrategy\"/> class.
	/// </summary>
	public NseIndexStrategyWithEntryExitMarkersStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 200)
			.SetDisplay("SMA Period", "SMA period for trend filtering", "General")
			.SetCanOptimize(true)
			.SetRange(1, 1000);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "General")
			.SetCanOptimize(true)
			.SetRange(1, 1000);

		_rsiOversold = Param(nameof(RsiOversold), 40m)
			.SetDisplay("RSI Oversold", "Oversold level for RSI", "General")
			.SetCanOptimize(true)
			.SetRange(0m, 100m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR calculation period", "General")
			.SetCanOptimize(true)
			.SetRange(1, 1000);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for stop loss and take profit", "General")
			.SetCanOptimize(true)
			.SetRange(0.1m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_stopLoss = 0m;
		_takeProfit = 0m;
		_prevRsi = 0m;
		_isRsiInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SimpleMovingAverage
		{
			Length = SmaPeriod
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, rsi);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
			{
				ClosePosition();
				_stopLoss = 0m;
				_takeProfit = 0m;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isRsiInitialized)
		{
			_prevRsi = rsiValue;
			_isRsiInitialized = true;
			return;
		}

		var inUptrend = candle.ClosePrice > smaValue;
		var crossUp = _prevRsi <= RsiOversold && rsiValue > RsiOversold;

		if (inUptrend && crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopLoss = candle.ClosePrice - AtrMultiplier * atrValue;
			_takeProfit = candle.ClosePrice + AtrMultiplier * atrValue;
		}

		_prevRsi = rsiValue;
	}
}
