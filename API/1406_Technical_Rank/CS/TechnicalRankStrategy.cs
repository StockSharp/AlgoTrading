namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy based on composite technical rank across multiple indicators.
/// Opens long when rank exceeds upper threshold and short when below lower threshold.
/// </summary>
public class TechnicalRankStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _sma200Val;
	private decimal _sma50Val;
	private decimal _ema12Val;
	private decimal _ema26Val;
	private decimal _rsiVal;
	private bool _sma200Formed;
	private bool _sma50Formed;
	private bool _ema12Formed;
	private bool _ema26Formed;
	private bool _rsiFormed;

	/// <summary>
	/// Upper rank threshold.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Lower rank threshold.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
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
	/// Initializes a new instance of <see cref="TechnicalRankStrategy"/>.
	/// </summary>
	public TechnicalRankStrategy()
	{
		_upperThreshold = Param(nameof(UpperThreshold), 5m)
			.SetDisplay("Upper Threshold", "Technical rank above this opens long", "Parameters");

		_lowerThreshold = Param(nameof(LowerThreshold), -5m)
			.SetDisplay("Lower Threshold", "Technical rank below this opens short", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma200 = new SimpleMovingAverage { Length = 200 };
		var sma50 = new SimpleMovingAverage { Length = 50 };
		var ema12 = new ExponentialMovingAverage { Length = 12 };
		var ema26 = new ExponentialMovingAverage { Length = 26 };
		var rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma200, (candle, val) =>
			{
				_sma200Val = val;
				_sma200Formed = sma200.IsFormed;
			})
			.Bind(sma50, (candle, val) =>
			{
				_sma50Val = val;
				_sma50Formed = sma50.IsFormed;
			})
			.Bind(ema12, (candle, val) =>
			{
				_ema12Val = val;
				_ema12Formed = ema12.IsFormed;
			})
			.Bind(ema26, (candle, val) =>
			{
				_ema26Val = val;
				_ema26Formed = ema26.IsFormed;
			})
			.Bind(rsi, (candle, val) =>
			{
				_rsiVal = val;
				_rsiFormed = rsi.IsFormed;
			})
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma50);
			DrawIndicator(area, sma200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma200Formed || !_sma50Formed || !_ema12Formed || !_ema26Formed || !_rsiFormed)
			return;

		var close = candle.ClosePrice;

		// PPO calculation
		var ppo = _ema26Val == 0 ? 0m : 100m * (_ema12Val - _ema26Val) / _ema26Val;

		// Short-term RSI component
		var stRsi = 0.05m * _rsiVal;

		// Long-term MA component
		var longTermMa = _sma200Val == 0 ? 0m : 0.30m * 100m * (close - _sma200Val) / _sma200Val;

		// Mid-term MA component
		var midTermMa = _sma50Val == 0 ? 0m : 0.15m * 100m * (close - _sma50Val) / _sma50Val;

		// PPO component
		var stPpo = 0.50m * ppo;

		// Composite rank
		var rank = longTermMa + midTermMa + stPpo + stRsi;

		if (rank > UpperThreshold && Position <= 0)
		{
			BuyMarket();
		}
		else if (rank < LowerThreshold && Position >= 0)
		{
			SellMarket();
		}

		_prevClose = close;
	}
}
