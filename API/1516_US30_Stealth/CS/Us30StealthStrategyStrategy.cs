using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// US30 Stealth strategy based on trend, engulfing patterns and volume.
/// </summary>
public class Us30StealthStrategyStrategy : Strategy
{
	private readonly StrategyParam<int> _maLen;
	private readonly StrategyParam<int> _volMaLen;
	private readonly StrategyParam<int> _hlLookback;
	private readonly StrategyParam<decimal> _rrRatio;
	private readonly StrategyParam<decimal> _maxCandleSize;
	private readonly StrategyParam<decimal> _pipValue;
	private readonly StrategyParam<decimal> _riskAmount;
	private readonly StrategyParam<decimal> _largeCandleThreshold;
	private readonly StrategyParam<int> _maSlopeLen;
	private readonly StrategyParam<decimal> _minSlope;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _maQueue = new();
	private int _lhCount;
	private int _hlCount;
	private decimal _prevOpen;
	private decimal _prevClose;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLen
	{
		get => _maLen.Value;
		set => _maLen.Value = value;
	}

	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int VolMaLen
	{
		get => _volMaLen.Value;
		set => _volMaLen.Value = value;
	}

	/// <summary>
	/// Lookback for high/low detection.
	/// </summary>
	public int HlLookback
	{
		get => _hlLookback.Value;
		set => _hlLookback.Value = value;
	}

	/// <summary>
	/// Risk-to-reward ratio.
	/// </summary>
	public decimal RrRatio
	{
		get => _rrRatio.Value;
		set => _rrRatio.Value = value;
	}

	/// <summary>
	/// Maximum candle size in points.
	/// </summary>
	public decimal MaxCandleSize
	{
		get => _maxCandleSize.Value;
		set => _maxCandleSize.Value = value;
	}

	/// <summary>
	/// Value of one pip.
	/// </summary>
	public decimal PipValue
	{
		get => _pipValue.Value;
		set => _pipValue.Value = value;
	}

	/// <summary>
	/// Risk per trade in cash.
	/// </summary>
	public decimal RiskAmount
	{
		get => _riskAmount.Value;
		set => _riskAmount.Value = value;
	}

	/// <summary>
	/// Threshold for large candles.
	/// </summary>
	public decimal LargeCandleThreshold
	{
		get => _largeCandleThreshold.Value;
		set => _largeCandleThreshold.Value = value;
	}

	/// <summary>
	/// Moving average slope lookback bars.
	/// </summary>
	public int MaSlopeLen
	{
		get => _maSlopeLen.Value;
		set => _maSlopeLen.Value = value;
	}

	/// <summary>
	/// Minimal slope threshold.
	/// </summary>
	public decimal MinSlope
	{
		get => _minSlope.Value;
		set => _minSlope.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public Us30StealthStrategyStrategy()
	{
		_maLen = Param(nameof(MaLen), 50)
			.SetDisplay("MA Length", "Moving average length", "Indicators")
			.SetCanOptimize(true);

		_volMaLen = Param(nameof(VolMaLen), 20)
			.SetDisplay("Volume MA Length", "Volume moving average length", "Indicators")
			.SetCanOptimize(true);

		_hlLookback = Param(nameof(HlLookback), 5)
			.SetDisplay("High/Low Lookback", "Lookback for swing detection", "Signals")
			.SetCanOptimize(true);

		_rrRatio = Param(nameof(RrRatio), 2.2m)
			.SetDisplay("Risk/Reward", "Risk-to-reward ratio", "Risk")
			.SetCanOptimize(true);

		_maxCandleSize = Param(nameof(MaxCandleSize), 30m)
			.SetDisplay("Max Candle Size", "Maximum candle size", "Risk")
			.SetCanOptimize(true);

		_pipValue = Param(nameof(PipValue), 1m)
			.SetDisplay("Pip Value", "Value of one pip", "Risk");

		_riskAmount = Param(nameof(RiskAmount), 50m)
			.SetDisplay("Risk Amount", "Risk per trade", "Risk")
			.SetCanOptimize(true);

		_largeCandleThreshold = Param(nameof(LargeCandleThreshold), 25m)
			.SetDisplay("Large Candle Threshold", "Threshold for large candles", "Risk")
			.SetCanOptimize(true);

		_maSlopeLen = Param(nameof(MaSlopeLen), 3)
			.SetDisplay("MA Slope Lookback", "Bars for slope calculation", "Indicators")
			.SetCanOptimize(true);

		_minSlope = Param(nameof(MinSlope), 0.1m)
			.SetDisplay("Min Slope", "Minimal slope threshold", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_maQueue.Clear();
		_lhCount = 0;
		_hlCount = 0;
		_prevOpen = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ma = new SimpleMovingAverage { Length = MaLen };
		var volMa = new SimpleMovingAverage { Length = VolMaLen };
		var highest = new Highest { Length = HlLookback + 1 };
		var lowest = new Lowest { Length = HlLookback + 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, volMa, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, volMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma, decimal volMa, decimal highestHigh, decimal lowestLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var slope = 0m;
		if (_maQueue.Count >= MaSlopeLen)
			slope = ma - _maQueue.Peek();

		_maQueue.Enqueue(ma);
		if (_maQueue.Count > MaSlopeLen)
			_maQueue.Dequeue();

		var isSlopeUp = slope > MinSlope;
		var isSlopeDown = slope < -MinSlope;
		var isDownTrend = candle.ClosePrice < ma && isSlopeDown;
		var isUpTrend = candle.ClosePrice > ma && isSlopeUp;

		var isLowerHigh = candle.HighPrice < highestHigh;
		if (isLowerHigh)
			_lhCount++;

		var isHigherLow = candle.LowPrice > lowestLow;
		if (isHigherLow)
			_hlCount++;

		var bearEng = candle.ClosePrice < candle.OpenPrice &&
			candle.ClosePrice < _prevOpen &&
			candle.OpenPrice > _prevClose &&
			candle.ClosePrice <= _prevOpen &&
			candle.OpenPrice >= _prevClose;

		var bullEng = candle.ClosePrice > candle.OpenPrice &&
			candle.ClosePrice > _prevOpen &&
			candle.OpenPrice < _prevClose &&
			candle.ClosePrice >= _prevOpen &&
			candle.OpenPrice <= _prevClose;

		var volOk = candle.TotalVolume > volMa;
		var hour = candle.OpenTime.Hour;
		var inSession = hour >= 22 || hour < 19;

		var rawCandleSize = candle.HighPrice - candle.LowPrice;
		var useHalfCandle = rawCandleSize > LargeCandleThreshold;
		var slSize = useHalfCandle ? rawCandleSize / 2m : rawCandleSize;
		var validSize = rawCandleSize <= MaxCandleSize;

		var sellSig = inSession && isDownTrend && _lhCount % 3 == 0 && _lhCount > 0 && bearEng && volOk && validSize;
		var buySig = inSession && isUpTrend && _hlCount % 3 == 0 && _hlCount > 0 && bullEng && volOk && validSize;

		var positionSize = slSize > 0m ? RiskAmount / (slSize * PipValue) : 0m;

		if (sellSig && positionSize > 0m)
		{
			CancelActiveOrders();
			SellMarket(positionSize);
			BuyLimit(positionSize, candle.ClosePrice - RrRatio * slSize);
			BuyStop(positionSize, candle.ClosePrice + slSize);
			_lhCount = 0;
		}
		else if (buySig && positionSize > 0m)
		{
			CancelActiveOrders();
			BuyMarket(positionSize);
			SellLimit(positionSize, candle.ClosePrice + RrRatio * slSize);
			SellStop(positionSize, candle.ClosePrice - slSize);
			_hlCount = 0;
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}
}
