using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy based on the ProfitView template.
/// </summary>
public class ProfitViewStrategyTemplateStrategy : Strategy
{
	private readonly StrategyParam<MaType> _ma1Type;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<MaType> _ma2Type;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMa1;
	private decimal _prevMa2;
	private bool _isInitialized;

	/// <summary>
	/// Moving average type.
	/// </summary>
	public enum MaType
	{
		SMA,
		EMA,
		RMA
	}

	/// <summary>
	/// Type of MA1.
	/// </summary>
	public MaType Ma1Type
	{
		get => _ma1Type.Value;
		set => _ma1Type.Value = value;
	}

	/// <summary>
	/// Length of MA1.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Type of MA2.
	/// </summary>
	public MaType Ma2Type
	{
		get => _ma2Type.Value;
		set => _ma2Type.Value = value;
	}

	/// <summary>
	/// Length of MA2.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
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
	/// Constructor.
	/// </summary>
	public ProfitViewStrategyTemplateStrategy()
	{
		_ma1Type = Param(nameof(Ma1Type), MaType.SMA)
			.SetDisplay("MA1 Type", "Type of first moving average", "Moving Averages");
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of first moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);
		_ma2Type = Param(nameof(Ma2Type), MaType.SMA)
			.SetDisplay("MA2 Type", "Type of second moving average", "Moving Averages");
		_ma2Length = Param(nameof(Ma2Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of second moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ma1 = CreateMa(Ma1Type, Ma1Length);
		var ma2 = CreateMa(Ma2Type, Ma2Length);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma1, ma2, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma1);
			DrawIndicator(area, ma2);
			DrawOwnTrades(area);
		}
	}

	private IIndicator CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.SMA => new SimpleMovingAverage { Length = length },
			MaType.EMA => new ExponentialMovingAverage { Length = length },
			MaType.RMA => new SmoothedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type))
		};
	}

	private void Process(ICandleMessage candle, decimal ma1Value, decimal ma2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevMa1 = ma1Value;
			_prevMa2 = ma2Value;
			_isInitialized = true;
			return;
		}

		var crossedUp = _prevMa1 <= _prevMa2 && ma1Value > ma2Value;
		var crossedDown = _prevMa1 >= _prevMa2 && ma1Value < ma2Value;

		if (crossedUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossedDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevMa1 = ma1Value;
		_prevMa2 = ma2Value;
	}
}
