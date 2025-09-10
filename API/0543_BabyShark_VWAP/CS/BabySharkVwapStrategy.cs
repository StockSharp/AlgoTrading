using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BabyShark VWAP strategy with OBV-based RSI filter.
/// </summary>
public class BabySharkVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _higherLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<int> _cooldown;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private VolumeWeightedMovingAverage _vwap;
	private StandardDeviation _std;
	private OnBalanceVolume _obv;
	private RelativeStrengthIndex _rsi;

	private decimal _entryPrice;
	private int _barsSinceExit;

	/// <summary>
	/// VWAP and deviation period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// OBV RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal HigherLevel
	{
		get => _higherLevel.Value;
		set => _higherLevel.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Cooldown period after trade.
	/// </summary>
	public int Cooldown
	{
		get => _cooldown.Value;
		set => _cooldown.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type for data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BabySharkVwapStrategy"/>.
	/// </summary>
	public BabySharkVwapStrategy()
	{
		_length = Param(nameof(Length), 60)
			.SetGreaterThanZero()
			.SetDisplay("VWAP Length", "Period for VWAP and deviation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_rsiLength = Param(nameof(RsiLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI of OBV", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_higherLevel = Param(nameof(HigherLevel), 70m)
			.SetDisplay("RSI Higher Level", "Overbought level", "Parameters");

		_lowerLevel = Param(nameof(LowerLevel), 30m)
			.SetDisplay("RSI Lower Level", "Oversold level", "Parameters");

		_cooldown = Param(nameof(Cooldown), 10)
			.SetGreaterThanOrEqual(0)
			.SetDisplay("Cooldown", "Bars to wait after trade", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.6m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percent stop loss", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_entryPrice = default;
		_barsSinceExit = Cooldown;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vwap = new VolumeWeightedMovingAverage { Length = Length };
		_std = new StandardDeviation { Length = Length };
		_obv = new OnBalanceVolume();
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);

		subscription.BindEx(_obv, ProcessIndicators).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}

		StartProtection(stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var obv = obvValue.ToDecimal();
		var rsi = _rsi.Process(obv).ToDecimal();
		var vwap = _vwap.Process(candle).ToDecimal();
		var dev = _std.Process(candle.ClosePrice).ToDecimal();

		ProcessCandle(candle, vwap, dev, rsi);
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwap, decimal dev, decimal rsi)
	{
		_barsSinceExit++;

		var upper = vwap + dev * 2m;
		var lower = vwap - dev * 2m;

		if (Position == 0)
		{
			if (_barsSinceExit >= Cooldown)
			{
				if (candle.ClosePrice <= lower && rsi <= LowerLevel)
				{
					RegisterBuy();
					_entryPrice = candle.ClosePrice;
					_barsSinceExit = 0;
				}
				else if (candle.ClosePrice >= upper && rsi >= HigherLevel)
				{
					RegisterSell();
					_entryPrice = candle.ClosePrice;
					_barsSinceExit = 0;
				}
			}
		}
		else if (Position > 0)
		{
			var stop = _entryPrice * (1 - StopLossPercent / 100m);
			if (candle.ClosePrice >= vwap || candle.ClosePrice <= stop)
			{
				RegisterSell();
				_barsSinceExit = 0;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice * (1 + StopLossPercent / 100m);
			if (candle.ClosePrice <= vwap || candle.ClosePrice >= stop)
			{
				RegisterBuy();
				_barsSinceExit = 0;
			}
		}
	}
}
