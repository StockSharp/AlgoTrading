using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian RSI strategy converted from the "Trade on qualified RSI" expert advisor.
/// </summary>
public class TradeOnQualifiedRSIStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<int> _countBars;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _stopPrice;
	private decimal _entryPrice;
	private int _aboveCounter;
	private int _belowCounter;

	/// <summary>
	/// RSI lookback period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold used to qualify short entries.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold used to qualify long entries.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Number of previous RSI bars that must stay beyond the threshold.
	/// </summary>
	public int CountBars
	{
		get => _countBars.Value;
		set => _countBars.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used as the RSI data source.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="TradeOnQualifiedRSIStrategy"/>.
	/// </summary>
	public TradeOnQualifiedRSIStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Lookback period for RSI calculation.", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 2);

		_upperThreshold = Param(nameof(UpperThreshold), 55m)
			.SetDisplay("Upper Threshold", "RSI level used to qualify short signals.", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(50m, 70m, 1m);

		_lowerThreshold = Param(nameof(LowerThreshold), 45m)
			.SetDisplay("Lower Threshold", "RSI level used to qualify long signals.", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(30m, 50m, 1m);

		_countBars = Param(nameof(CountBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Qualification Bars", "How many previous RSI bars must stay beyond the threshold.", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stopLossPoints = Param(nameof(StopLossPoints), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop loss distance expressed in price steps.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for entries.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Source timeframe for RSI calculation.", "General");
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

		Volume = TradeVolume;
		_stopPrice = null;
		_entryPrice = 0m;
		_aboveCounter = 0;
		_belowCounter = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_rsi == null || !_rsi.IsFormed)
		{
			_aboveCounter = 0;
			_belowCounter = 0;
			return;
		}

		if (Volume <= 0)
			return;

		var distance = CalculateStopDistance();
		if (distance <= 0)
			return;

		UpdateCounters(rsiValue);

		var requiredBars = CountBars + 1;

		if (Position == 0)
		{
			_stopPrice = null;
			_entryPrice = 0m;

			var shortSignal = rsiValue >= UpperThreshold && _aboveCounter >= requiredBars;
			var longSignal = rsiValue <= LowerThreshold && _belowCounter >= requiredBars;

			if (shortSignal)
			{
				LogInfo($"Open short: RSI={rsiValue:F2}, counter={_aboveCounter}");
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice + distance;
				return;
			}

			if (longSignal)
			{
				LogInfo($"Open long: RSI={rsiValue:F2}, counter={_belowCounter}");
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice - distance;
			}

			return;
		}

		if (Position > 0)
		{
			if (_stopPrice == null)
				_stopPrice = _entryPrice - distance;

			var newStop = candle.ClosePrice - distance;
			if (_stopPrice == null || newStop > _stopPrice)
				_stopPrice = newStop;

			if (_stopPrice != null && candle.LowPrice <= _stopPrice)
			{
				LogInfo($"Exit long via stop at {_stopPrice:F5}");
				SellMarket(Math.Abs(Position));
				_stopPrice = null;
				_entryPrice = 0m;
			}

			return;
		}

		if (Position < 0)
		{
			if (_stopPrice == null)
				_stopPrice = _entryPrice + distance;

			var newStop = candle.ClosePrice + distance;
			if (_stopPrice == null || newStop < _stopPrice)
				_stopPrice = newStop;

			if (_stopPrice != null && candle.HighPrice >= _stopPrice)
			{
				LogInfo($"Exit short via stop at {_stopPrice:F5}");
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
				_entryPrice = 0m;
			}
		}
	}

	private decimal CalculateStopDistance()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		return StopLossPoints * step;
	}

	private void UpdateCounters(decimal rsiValue)
	{
		// Track consecutive closes above and below the thresholds.
		if (rsiValue >= UpperThreshold)
		{
			_aboveCounter++;
		}
		else
		{
			_aboveCounter = 0;
		}

		if (rsiValue <= LowerThreshold)
		{
			_belowCounter++;
		}
		else
		{
			_belowCounter = 0;
		}
	}
}
