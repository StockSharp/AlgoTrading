namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Volume Weighted Moving Average with Standard Deviation filter.
/// Opens long when VWMA momentum exceeds thresholds and short on opposite.
/// </summary>
public class VolumeWeightedMaStDevStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _vwmaLength;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;

	private VolumeWeightedMovingAverage _vwma = null!;
	private StandardDeviation _stdDev = null!;

	private decimal? _prevVwma;

	public VolumeWeightedMaStDevStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");

		_vwmaLength = Param(nameof(VwmaLength), 12)
			.SetDisplay("VWMA Length", "Period for Volume Weighted MA", "Indicators");

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Period for standard deviation", "Indicators");

		_k1 = Param(nameof(K1), 1.5m)
			.SetDisplay("K1", "First deviation multiplier", "Signal")
			.SetCanOptimize(true);

		_k2 = Param(nameof(K2), 2.5m)
			.SetDisplay("K2", "Second deviation multiplier", "Signal")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int VwmaLength
	{
		get => _vwmaLength.Value;
		set => _vwmaLength.Value = value;
	}

	public int StdPeriod
	{
		get => _stdPeriod.Value;
		set => _stdPeriod.Value = value;
	}

	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
	}

	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevVwma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vwma = new VolumeWeightedMovingAverage { Length = VwmaLength };
		_stdDev = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vwma, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vwma.IsFormed)
		{
			_prevVwma = vwmaValue;
			return;
		}

		if (_prevVwma is null)
		{
			_prevVwma = vwmaValue;
			return;
		}

		var diff = vwmaValue - _prevVwma.Value;
		var stdValue = _stdDev.Process(diff, candle.ServerTime, true).ToNullableDecimal();

		if (stdValue is null || !_stdDev.IsFormed)
		{
			_prevVwma = vwmaValue;
			return;
		}

		var filter1 = K1 * stdValue.Value;
		var filter2 = K2 * stdValue.Value;
		_ = filter2;

		var bulls = diff > filter1;
		var bears = diff < -filter1;

		if (bulls && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bears && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevVwma = vwmaValue;
	}
}
