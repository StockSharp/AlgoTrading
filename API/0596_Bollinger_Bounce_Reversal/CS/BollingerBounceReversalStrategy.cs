using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades Bollinger Band bounce reversals with MACD and volume confirmation.
/// Limits entries per day and applies fixed percent stop loss and take profit.
/// </summary>
public class BollingerBounceReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbStdDev;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;
	
	private MovingAverageConvergenceDivergenceSignal _macd;
	private BollingerBands _bollinger;
	private SimpleMovingAverage _volumeSma;
	
	private decimal _prevClose;
	private decimal _prevLowerBand;
	private decimal _prevUpperBand;
	private bool _hasPrev;
	
	private DateTime _currentDate;
	private int _tradesToday;
	
	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	
	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BbStdDev { get => _bbStdDev.Value; set => _bbStdDev.Value = value; }
	
	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFastLength { get => _macdFast.Value; set => _macdFast.Value = value; }
	
	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlowLength { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	
	/// <summary>
	/// MACD signal length.
	/// </summary>
	public int MacdSignalLength { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	
	/// <summary>
	/// Volume moving average period.
	/// </summary>
	public int VolumePeriod { get => _volumePeriod.Value; set => _volumePeriod.Value = value; }
	
	/// <summary>
	/// Volume spike factor.
	/// </summary>
	public decimal VolumeFactor { get => _volumeFactor.Value; set => _volumeFactor.Value = value; }
	
	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	
	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	
	/// <summary>
	/// Maximum number of trades per day.
	/// </summary>
	public int MaxTradesPerDay { get => _maxTradesPerDay.Value; set => _maxTradesPerDay.Value = value; }
	
	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public BollingerBounceReversalStrategy()
	{
		_bbPeriod = Param(nameof(BollingerPeriod), 20)
		.SetDisplay("BB Period", "Bollinger Bands period", "Indicators");
		
		_bbStdDev = Param(nameof(BbStdDev), 2m)
		.SetDisplay("BB StdDev", "Standard deviation multiplier", "Indicators");
		
		_macdFast = Param(nameof(MacdFastLength), 12)
		.SetDisplay("MACD Fast", "Fast EMA length", "MACD");
		
		_macdSlow = Param(nameof(MacdSlowLength), 26)
		.SetDisplay("MACD Slow", "Slow EMA length", "MACD");
		
		_macdSignal = Param(nameof(MacdSignalLength), 9)
		.SetDisplay("MACD Signal", "Signal EMA length", "MACD");
		
		_volumePeriod = Param(nameof(VolumePeriod), 20)
		.SetDisplay("Volume MA Period", "Volume SMA period", "Indicators");
		
		_volumeFactor = Param(nameof(VolumeFactor), 1m)
		.SetDisplay("Volume Factor", "Volume spike factor", "Indicators");
		
		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
		
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
		
		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5)
		.SetDisplay("Max Trades/Day", "Maximum number of trades per day", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevClose = 0;
		_prevLowerBand = 0;
		_prevUpperBand = 0;
		_hasPrev = false;
		_currentDate = DateTime.MinValue;
		_tradesToday = 0;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BbStdDev
		};
		
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};
		
		_volumeSma = new SimpleMovingAverage { Length = VolumePeriod };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _bollinger, ProcessCandle)
		.Start();
		
		StartProtection(
		new Unit(TakeProfitPercent, UnitTypes.Percent),
		new Unit(StopLossPercent, UnitTypes.Percent));
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var bb = (BollingerBandsValue)bollingerValue;
		
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
		return;
		
		if (bb.UpBand is not decimal upperBand || bb.LowBand is not decimal lowerBand)
		return;
		
		var volAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		
		if (!_bollinger.IsFormed || !_macd.IsFormed || !_volumeSma.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevLowerBand = lowerBand;
			_prevUpperBand = upperBand;
			_hasPrev = true;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevLowerBand = lowerBand;
			_prevUpperBand = upperBand;
			_hasPrev = true;
			return;
		}
		
		var candleDate = candle.OpenTime.Date;
		if (candleDate != _currentDate)
		{
			_currentDate = candleDate;
			_tradesToday = 0;
		}
		
		var volumeHigh = candle.TotalVolume >= volAvg * VolumeFactor;
		
		var longSignal = _hasPrev &&
		_prevClose < _prevLowerBand &&
		candle.ClosePrice > lowerBand &&
		macdLine > signalLine &&
		volumeHigh &&
		_tradesToday < MaxTradesPerDay &&
		Position <= 0;
		
		var shortSignal = _hasPrev &&
		_prevClose > _prevUpperBand &&
		candle.ClosePrice < upperBand &&
		macdLine < signalLine &&
		volumeHigh &&
		_tradesToday < MaxTradesPerDay &&
		Position >= 0;
		
		if (longSignal)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
		else if (shortSignal)
		{
			SellMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
		
		_prevClose = candle.ClosePrice;
		_prevLowerBand = lowerBand;
		_prevUpperBand = upperBand;
		_hasPrev = true;
	}
}

