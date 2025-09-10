using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// All Divergences with trend strategy - trades RSI divergences filtered by moving average.
/// </summary>
public class AllDivergencesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _useMaRisk;
	private readonly StrategyParam<int> _maRiskCandles;
	private readonly StrategyParam<bool> _useProtection;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private SimpleMovingAverage _ma;
	private RelativeStrengthIndex _rsi;
	private decimal? _lastLowPrice;
	private decimal? _lastLowRsi;
	private decimal? _lastHighPrice;
	private decimal? _lastHighRsi;
	private int _longAgainstCount;
	private int _shortAgainstCount;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

	/// <summary>
	/// Use MA based risk management.
	/// </summary>
	public bool UseMaRisk { get => _useMaRisk.Value; set => _useMaRisk.Value = value; }

	/// <summary>
	/// Number of consecutive closes against MA to exit.
	/// </summary>
	public int MaRiskCandles { get => _maRiskCandles.Value; set => _maRiskCandles.Value = value; }

	/// <summary>
	/// Use stop loss and take profit.
	/// </summary>
	public bool UseProtection { get => _useProtection.Value; set => _useProtection.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public AllDivergencesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_maLength = Param(nameof(MaLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Length of moving average", "MA")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("RSI Length", "RSI period", "RSI")
		.SetCanOptimize(true)
		.SetOptimize(7, 21, 2);

		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_useMaRisk = Param(nameof(UseMaRisk), false)
		.SetDisplay("Use MA Risk Management", "Exit when price closes against MA", "Risk");

		_maRiskCandles = Param(nameof(MaRiskCandles), 3)
		.SetGreaterThanZero()
		.SetDisplay("MA Risk Candles", "Consecutive closes against MA", "Risk");

		_useProtection = Param(nameof(UseProtection), false)
		.SetDisplay("Use Protection", "Enable stop loss and take profit", "Protection");

		_stopPercent = Param(nameof(StopPercent), 1m)
		.SetRange(0.1m, 10m)
		.SetDisplay("Stop %", "Stop loss percentage", "Protection")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetRange(0.1m, 10m)
		.SetDisplay("Take Profit %", "Take profit percentage", "Protection")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);
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

		_lastLowPrice = null;
		_lastLowRsi = null;
		_lastHighPrice = null;
		_lastHighRsi = null;
		_longAgainstCount = 0;
		_shortAgainstCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SimpleMovingAverage { Length = MaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx([_rsi, _ma], ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		if (UseProtection)
		{
			StartProtection(
			new Unit(TakeProfitPercent / 100m, UnitTypes.Percent),
			new Unit(StopPercent / 100m, UnitTypes.Percent));
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_rsi.IsFormed || !_ma.IsFormed)
		return;

		if (values[0].ToNullableDecimal() is not decimal rsiValue)
		return;

		if (values[1].ToNullableDecimal() is not decimal maValue)
		return;

		var closePrice = candle.ClosePrice;
		var longTrend = EnableLong && closePrice > maValue;
		var shortTrend = EnableShort && closePrice < maValue;

		if (longTrend && _lastLowPrice is decimal prevLow && _lastLowRsi is decimal prevRsi)
		{
			if (candle.LowPrice < prevLow && rsiValue > prevRsi && Position <= 0)
			BuyMarket();
		}

		if (shortTrend && _lastHighPrice is decimal prevHigh && _lastHighRsi is decimal prevHighRsi)
		{
			if (candle.HighPrice > prevHigh && rsiValue < prevHighRsi && Position >= 0)
			SellMarket();
		}

		if (_lastLowPrice is null || candle.LowPrice < _lastLowPrice)
		{
			_lastLowPrice = candle.LowPrice;
			_lastLowRsi = rsiValue;
		}

		if (_lastHighPrice is null || candle.HighPrice > _lastHighPrice)
		{
			_lastHighPrice = candle.HighPrice;
			_lastHighRsi = rsiValue;
		}

		if (UseMaRisk)
		{
			if (Position > 0)
			{
				if (closePrice < maValue)
				_longAgainstCount++;
				else
				_longAgainstCount = 0;

				if (_longAgainstCount >= MaRiskCandles)
				{
					ClosePosition();
					_longAgainstCount = 0;
				}
			}
			else if (Position < 0)
			{
				if (closePrice > maValue)
				_shortAgainstCount++;
				else
				_shortAgainstCount = 0;

				if (_shortAgainstCount >= MaRiskCandles)
				{
					ClosePosition();
					_shortAgainstCount = 0;
				}
			}
		}
	}
}
