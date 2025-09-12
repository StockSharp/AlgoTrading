using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Measures directional bias over a window and trades when bias and range conditions align.
/// Uses ATR-based targets and exits after a maximum number of bars.
/// </summary>
public class VolatilityBiasModelStrategy : Strategy
{
	private readonly StrategyParam<int> _biasWindow;
	private readonly StrategyParam<decimal> _biasThreshold;
	private readonly StrategyParam<decimal> _rangeMin;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<int> _maxBars;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _biasSma;
	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;

	private int _barsInPosition;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _isLong;

	/// <summary>
	/// Lookback window for bias.
	/// </summary>
	public int BiasWindow
	{
		get => _biasWindow.Value;
		set => _biasWindow.Value = value;
	}

	/// <summary>
	/// Minimum ratio of bullish bars to consider long.
	/// </summary>
	public decimal BiasThreshold
	{
		get => _biasThreshold.Value;
		set => _biasThreshold.Value = value;
	}

	/// <summary>
	/// Minimum price range percentage.
	/// </summary>
	public decimal RangeMin
	{
		get => _rangeMin.Value;
		set => _rangeMin.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Maximum bars to hold a position.
	/// </summary>
	public int MaxBars
	{
		get => _maxBars.Value;
		set => _maxBars.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public VolatilityBiasModelStrategy()
	{
		_biasWindow = Param(nameof(BiasWindow), 10)
			.SetGreaterThanZero()
			.SetDisplay("Bias Window", "Bars for bias calculation", "Parameters");

		_biasThreshold = Param(nameof(BiasThreshold), 0.6m)
			.SetRange(0m, 1m)
			.SetDisplay("Bias Threshold", "Directional bias threshold", "Parameters");

		_rangeMin = Param(nameof(RangeMin), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Range Min", "Minimum range percentage", "Parameters");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Risk reward ratio", "Parameters");

		_maxBars = Param(nameof(MaxBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Max Bars", "Maximum bars to hold", "Parameters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "Length for ATR", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_biasSma = new SimpleMovingAverage { Length = BiasWindow };
		_highest = new Highest { Length = BiasWindow };
		_lowest = new Lowest { Length = BiasWindow };
		_atr = new AverageTrueRange { Length = AtrLength };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(_highest, _lowest, _atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upRatio = _biasSma.Process(candle.ClosePrice > candle.OpenPrice ? 1m : 0m, candle.OpenTime, true).ToDecimal();
		var rangePerc = lowest == 0m ? 0m : (highest - lowest) / lowest;

		if (Position == 0)
		{
			if (upRatio >= BiasThreshold && rangePerc > RangeMin)
			{
				_isLong = true;
				_stopPrice = candle.ClosePrice - atrValue;
				_takePrice = candle.ClosePrice + atrValue * RiskReward;
				BuyMarket();
				_barsInPosition = 0;
			}
			else if (upRatio <= 1 - BiasThreshold && rangePerc > RangeMin)
			{
				_isLong = false;
				_stopPrice = candle.ClosePrice + atrValue;
				_takePrice = candle.ClosePrice - atrValue * RiskReward;
				SellMarket();
				_barsInPosition = 0;
			}
		}
		else
		{
			_barsInPosition++;

			if (_isLong)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice || _barsInPosition >= MaxBars)
					SellMarket();
			}
			else
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice || _barsInPosition >= MaxBars)
					BuyMarket();
			}
		}
	}
}
