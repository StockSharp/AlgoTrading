using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy combining Livermore pivot points with Seykota trend filter and ATR exits.
/// </summary>
public class LivermoreSeykotaBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _mainEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _trailAtrMultiplier;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private readonly List<ICandleMessage> _candles = [];
	private decimal? _lastPivotHigh;
	private decimal? _lastPivotLow;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="LivermoreSeykotaBreakoutStrategy"/>.
	/// </summary>
	public LivermoreSeykotaBreakoutStrategy()
	{
		_mainEmaLength = Param(nameof(MainEmaLength), 50)
			.SetDisplay("Main EMA Length", "Primary EMA period", "Indicators")
			.SetCanOptimize(true);

		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetDisplay("Fast EMA Length", "Fast EMA for trend filter", "Indicators")
			.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 200)
			.SetDisplay("Slow EMA Length", "Slow EMA for trend filter", "Indicators")
			.SetCanOptimize(true);

		_pivotLength = Param(nameof(PivotLength), 3)
			.SetDisplay("Pivot Length", "Bars left/right for pivot", "General")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Indicators")
			.SetCanOptimize(true);

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 3m)
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop loss", "Risk")
			.SetCanOptimize(true);

		_trailAtrMultiplier = Param(nameof(TrailAtrMultiplier), 2m)
			.SetDisplay("Trail ATR Mult", "ATR multiplier for trailing", "Risk")
			.SetCanOptimize(true);

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetDisplay("Volume SMA Length", "Period for volume SMA", "Volume")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// Main EMA length.
	/// </summary>
	public int MainEmaLength { get => _mainEmaLength.Value; set => _mainEmaLength.Value = value; }

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// Bars left/right for pivot detection.
	/// </summary>
	public int PivotLength { get => _pivotLength.Value; set => _pivotLength.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopAtrMultiplier { get => _stopAtrMultiplier.Value; set => _stopAtrMultiplier.Value = value; }

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal TrailAtrMultiplier { get => _trailAtrMultiplier.Value; set => _trailAtrMultiplier.Value = value; }

	/// <summary>
	/// Period for volume SMA.
	/// </summary>
	public int VolumeSmaLength { get => _volumeSmaLength.Value; set => _volumeSmaLength.Value = value; }

	/// <summary>
	/// Candle data type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_lastPivotHigh = null;
		_lastPivotLow = null;
		_highestPrice = default;
		_lowestPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var emaMain = new ExponentialMovingAverage { Length = MainEmaLength };
		var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaMain, emaFast, emaSlow, atr, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, emaMain);
			DrawIndicator(priceArea, emaFast);
			DrawIndicator(priceArea, emaSlow);
			DrawOwnTrades(priceArea);
		}

		var volumeArea = CreateChartArea();
		if (volumeArea != null)
			DrawIndicator(volumeArea, _volumeSma);
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaMain, decimal emaFast, decimal emaSlow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		_candles.Add(candle);
		var maxCount = PivotLength * 2 + 1;
		if (_candles.Count > maxCount)
			_candles.RemoveAt(0);

		if (_candles.Count == maxCount)
		{
			var pivotIndex = PivotLength;
			var pivotCandle = _candles[pivotIndex];

			var isHigh = true;
			var isLow = true;

			for (var i = 0; i < maxCount; i++)
			{
				if (i == pivotIndex)
					continue;

				var c = _candles[i];
				if (c.HighPrice >= pivotCandle.HighPrice)
					isHigh = false;
				if (c.LowPrice <= pivotCandle.LowPrice)
					isLow = false;
			}

			if (isHigh)
				_lastPivotHigh = pivotCandle.HighPrice;
			if (isLow)
				_lastPivotLow = pivotCandle.LowPrice;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || !_volumeSma.IsFormed)
			return;

		var buyCond = _lastPivotHigh is decimal lastHigh && candle.ClosePrice > lastHigh && candle.ClosePrice > emaMain;
		var buyTrend = emaFast > emaSlow;
		var buyVolume = candle.TotalVolume > volumeAvg;

		var sellCond = _lastPivotLow is decimal lastLow && candle.ClosePrice < lastLow && candle.ClosePrice < emaMain;
		var sellTrend = emaFast < emaSlow;
		var sellVolume = candle.TotalVolume > volumeAvg;

		if (buyCond && buyTrend && buyVolume && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
		}
		else if (sellCond && sellTrend && sellVolume && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var stopPrice = PositionPrice - atr * StopAtrMultiplier;
			var trailPrice = _highestPrice - atr * TrailAtrMultiplier;
			var exit = Math.Max(stopPrice, trailPrice);

			if (candle.ClosePrice <= exit)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			var stopPrice = PositionPrice + atr * StopAtrMultiplier;
			var trailPrice = _lowestPrice + atr * TrailAtrMultiplier;
			var exit = Math.Min(stopPrice, trailPrice);

			if (candle.ClosePrice >= exit)
				BuyMarket(Math.Abs(Position));
		}
	}
}
