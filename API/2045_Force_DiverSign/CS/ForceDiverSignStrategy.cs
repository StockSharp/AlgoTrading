using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Force DiverSign strategy.
/// Detects divergence between fast and slow Force Index values
/// combined with a specific candle pattern.
/// Opens a long position on bullish divergence and a short position on bearish divergence.
/// </summary>
public class ForceDiverSignStrategy : Strategy
{
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType1;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType2;
	private readonly StrategyParam<DataType> _candleType;
	private LengthIndicator<decimal> _ma1;
	private LengthIndicator<decimal> _ma2;
	private readonly decimal[] _opens = new decimal[5];
	private readonly decimal[] _closes = new decimal[5];
	private readonly decimal[] _f1 = new decimal[5];
	private readonly decimal[] _f2 = new decimal[5];
	private decimal _prevClose;
	private int _count;
	/// <summary>
	/// Fast Force Index period.
	/// </summary>
	public int Period1
	{
		get => _period1.Value;
		set => _period1.Value = value;
	}
	/// <summary>
	/// Slow Force Index period.
	/// </summary>
	public int Period2
	{
		get => _period2.Value;
		set => _period2.Value = value;
	}
	/// <summary>
	/// Moving average type for the fast Force Index.
	/// </summary>
	public MovingAverageTypeEnum MaType1
	{
		get => _maType1.Value;
		set => _maType1.Value = value;
	}
	/// <summary>
	/// Moving average type for the slow Force Index.
	/// </summary>
	public MovingAverageTypeEnum MaType2
	{
		get => _maType2.Value;
		set => _maType2.Value = value;
	}
	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Initializes a new instance of <see cref="ForceDiverSignStrategy"/>.
	/// </summary>
	public ForceDiverSignStrategy()
	{
		_period1 = Param(nameof(Period1), 3)
		.SetGreaterThanZero()
		.SetDisplay("Fast Period", "Period for fast Force index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 10, 1);
		_period2 = Param(nameof(Period2), 7)
		.SetGreaterThanZero()
		.SetDisplay("Slow Period", "Period for slow Force index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);
		_maType1 = Param(nameof(MaType1), MovingAverageTypeEnum.Exponential)
		.SetDisplay("Fast MA Type", "Moving average type for fast Force", "Indicators");
		_maType2 = Param(nameof(MaType2), MovingAverageTypeEnum.Exponential)
		.SetDisplay("Slow MA Type", "Moving average type for slow Force", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_ma1 = null;
		_ma2 = null;
		_prevClose = 0m;
		_count = 0;
		Array.Clear(_opens);
		Array.Clear(_closes);
		Array.Clear(_f1);
		Array.Clear(_f2);
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_ma1 = CreateMa(MaType1, Period1);
		_ma2 = CreateMa(MaType2, Period2);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1);
			DrawIndicator(area, _ma2);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		if (_count == 0)
		{
			_prevClose = candle.ClosePrice;
			Shift(_opens, candle.OpenPrice);
			Shift(_closes, candle.ClosePrice);
			_count++;
			return;
		}
		var force = (candle.ClosePrice - _prevClose) * candle.TotalVolume;
		_prevClose = candle.ClosePrice;
		var f1 = _ma1.Process(force).ToDecimal();
		var f2 = _ma2.Process(force).ToDecimal();
		Shift(_opens, candle.OpenPrice);
		Shift(_closes, candle.ClosePrice);
		Shift(_f1, f1);
		Shift(_f2, f2);
		if (_count < 5)
		{
			_count++;
			return;
		}
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		var sellSignal = _opens[3] < _closes[3] && _opens[2] > _closes[2] && _opens[1] < _closes[1]
		&& _f1[4] < _f1[3] && _f1[3] > _f1[2] && _f1[2] < _f1[1]
		&& _f2[4] < _f2[3] && _f2[3] > _f2[2] && _f2[2] < _f2[1]
		&& ((_f1[3] > _f1[1] && _f2[3] < _f2[1]) || (_f1[3] < _f1[1] && _f2[3] > _f2[1]));
		var buySignal = _opens[3] > _closes[3] && _opens[2] < _closes[2] && _opens[1] > _closes[1]
		&& _f1[4] > _f1[3] && _f1[3] < _f1[2] && _f1[2] > _f1[1]
		&& _f2[4] > _f2[3] && _f2[3] < _f2[2] && _f2[2] > _f2[1]
		&& ((_f1[3] > _f1[1] && _f2[3] < _f2[1]) || (_f1[3] < _f1[1] && _f2[3] > _f2[1]));
		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
	private static void Shift(decimal[] array, decimal value)
	{
		for (var i = array.Length - 1; i > 0; i--)
		array[i] = array[i - 1];
		array[0] = value;
	}
	private static LengthIndicator<decimal> CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Weighted moving average.</summary>
		Weighted,
		/// <summary>Volume weighted moving average.</summary>
		VolumeWeighted,
	}
}
