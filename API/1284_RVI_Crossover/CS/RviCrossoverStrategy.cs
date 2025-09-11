using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RVI crossover strategy.
/// Enters long when RVI crosses above its signal line while EMA is above VWMA.
/// Enters short when RVI crosses below its signal line while EMA is below VWMA.
/// </summary>
public class RviCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _vwmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;
	private ExponentialMovingAverage _ema;
	private VolumeWeightedMovingAverage _vwma;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	/// <summary>
	/// Length for RVI calculation.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// Length for RVI signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Length for EMA filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Length for VWMA filter.
	/// </summary>
	public int VwmaLength
	{
		get => _vwmaLength.Value;
		set => _vwmaLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RviCrossoverStrategy"/>.
	/// </summary>
	public RviCrossoverStrategy()
	{
		_rviLength = Param(nameof(RviLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for RVI", "General")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length for signal line", "General")
			.SetCanOptimize(true);

		_emaLength = Param(nameof(EmaLength), 31)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length for EMA", "General")
			.SetCanOptimize(true);

		_vwmaLength = Param(nameof(VwmaLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Length", "Length for VWMA", "General")
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

		_rvi = default;
		_signal = default;
		_ema = default;
		_vwma = default;
		_prevRvi = default;
		_prevSignal = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi = new RelativeVigorIndex { Length = RviLength };
		_signal = new SimpleMovingAverage { Length = SignalLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_vwma = new VolumeWeightedMovingAverage { Length = VwmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rvi);
			DrawIndicator(area, _signal);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviValue = _rvi.Process(candle);
		var signalValue = _signal.Process(rviValue);
		var emaValue = _ema.Process(candle);
		var vwmaValue = _vwma.Process(candle);

		if (!rviValue.IsFinal || !signalValue.IsFinal || !emaValue.IsFinal || !vwmaValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rvi = rviValue.ToDecimal();
		var signal = signalValue.ToDecimal();
		var ema = emaValue.ToDecimal();
		var vwma = vwmaValue.ToDecimal();

		var bullish = ema < vwma;
		var bearish = ema > vwma;

		var longCondition = _prevRvi <= _prevSignal && rvi > signal && bearish;
		var shortCondition = _prevRvi >= _prevSignal && rvi < signal && bullish;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
