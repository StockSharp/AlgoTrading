using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI-adaptive T3 strategy that trades when the T3 crosses its 2-bar lag.
/// </summary>
public class RsiAdaptiveT3Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minT3Length;
	private readonly StrategyParam<int> _maxT3Length;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _e1;
	private decimal? _e2;
	private decimal? _e3;
	private decimal? _e4;
	private decimal? _e5;
	private decimal? _e6;
	private decimal? _t3Prev1;
	private decimal? _t3Prev2;
	private decimal? _t3Prev3;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MinT3Length { get => _minT3Length.Value; set => _minT3Length.Value = value; }
	public int MaxT3Length { get => _maxT3Length.Value; set => _maxT3Length.Value = value; }
	public decimal VolumeFactor { get => _volumeFactor.Value; set => _volumeFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiAdaptiveT3Strategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_minT3Length = Param(nameof(MinT3Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Min T3 Length", "Minimum T3 length", "Indicators");

		_maxT3Length = Param(nameof(MaxT3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("Max T3 Length", "Maximum T3 length", "Indicators");

		_volumeFactor = Param(nameof(VolumeFactor), 0.7m)
			.SetDisplay("Volume Factor", "T3 volume factor", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_rsi, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsi = rsiValue.ToDecimal();
		var scale = 1m - rsi / 100m;
		var length = (int)Math.Round(MinT3Length + (MaxT3Length - MinT3Length) * scale);

		var t3 = CalcT3(candle.ClosePrice, length);

		var t3Lag = _t3Prev2;
		var diff = t3Lag.HasValue ? t3 - t3Lag.Value : 0m;
		var prevDiff = _t3Prev1.HasValue && _t3Prev3.HasValue ? _t3Prev1.Value - _t3Prev3.Value : 0m;

		var longEntry = t3Lag.HasValue && prevDiff <= 0m && diff > 0m;
		var longExit = t3Lag.HasValue && prevDiff >= 0m && diff < 0m;

		if (longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (longExit && Position > 0)
			SellMarket(Position);

		_t3Prev3 = _t3Prev2;
		_t3Prev2 = _t3Prev1;
		_t3Prev1 = t3;
	}

	private decimal CalcT3(decimal price, int length)
	{
		var alpha = 2m / (length + 1m);

		_e1 = _e1.HasValue ? alpha * price + (1m - alpha) * _e1.Value : price;
		_e2 = _e2.HasValue ? alpha * _e1.Value + (1m - alpha) * _e2.Value : _e1;
		_e3 = _e3.HasValue ? alpha * _e2.Value + (1m - alpha) * _e3.Value : _e2;
		_e4 = _e4.HasValue ? alpha * _e3.Value + (1m - alpha) * _e4.Value : _e3;
		_e5 = _e5.HasValue ? alpha * _e4.Value + (1m - alpha) * _e5.Value : _e4;
		_e6 = _e6.HasValue ? alpha * _e5.Value + (1m - alpha) * _e6.Value : _e5;

		var v = VolumeFactor;
		var c1 = -v * v * v;
		var c2 = 3m * v * v + 3m * v * v * v;
		var c3 = -6m * v * v - 3m * v - 3m * v * v * v;
		var c4 = 1m + 3m * v + v * v * v + 3m * v * v;

		return c1 * _e6.Value + c2 * _e5.Value + c3 * _e4.Value + c4 * _e3.Value;
	}
}

