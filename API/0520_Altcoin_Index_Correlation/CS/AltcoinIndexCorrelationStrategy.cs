using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Altcoin Index Correlation Strategy - trades when EMA trends on the symbol and reference index align.
/// </summary>
public class AltcoinIndexCorrelationStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLen;
	private readonly StrategyParam<int> _slowEmaLen;
	private readonly StrategyParam<int> _indexFastEmaLen;
	private readonly StrategyParam<int> _indexSlowEmaLen;
	private readonly StrategyParam<bool> _skipIndex;
	private readonly StrategyParam<bool> _inverseSignal;
	private readonly StrategyParam<Security> _indexSecurity;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _indexFast;
	private decimal _indexSlow;
	private bool _indexReady;

	/// <summary>
	/// Length of fast EMA for main security.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLen.Value;
		set => _fastEmaLen.Value = value;
	}

	/// <summary>
	/// Length of slow EMA for main security.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLen.Value;
		set => _slowEmaLen.Value = value;
	}

	/// <summary>
	/// Length of fast EMA for reference index.
	/// </summary>
	public int IndexFastEmaLength
	{
		get => _indexFastEmaLen.Value;
		set => _indexFastEmaLen.Value = value;
	}

	/// <summary>
	/// Length of slow EMA for reference index.
	/// </summary>
	public int IndexSlowEmaLength
	{
		get => _indexSlowEmaLen.Value;
		set => _indexSlowEmaLen.Value = value;
	}

	/// <summary>
	/// Skip using reference index in calculations.
	/// </summary>
	public bool SkipIndexReference
	{
		get => _skipIndex.Value;
		set => _skipIndex.Value = value;
	}

	/// <summary>
	/// Inverse correlation logic.
	/// </summary>
	public bool InverseSignal
	{
		get => _inverseSignal.Value;
		set => _inverseSignal.Value = value;
	}

	/// <summary>
	/// Reference index security.
	/// </summary>
	public Security IndexSecurity
	{
		get => _indexSecurity.Value;
		set => _indexSecurity.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public AltcoinIndexCorrelationStrategy()
	{
		_fastEmaLen = Param(nameof(FastEmaLength), 47)
			.SetDisplay("Fast EMA", "Fast EMA length", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_slowEmaLen = Param(nameof(SlowEmaLength), 50)
			.SetDisplay("Slow EMA", "Slow EMA length", "EMA Settings")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_indexFastEmaLen = Param(nameof(IndexFastEmaLength), 47)
			.SetDisplay("Index Fast EMA", "Fast EMA length for index", "Index Reference")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_indexSlowEmaLen = Param(nameof(IndexSlowEmaLength), 50)
			.SetDisplay("Index Slow EMA", "Slow EMA length for index", "Index Reference")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_skipIndex = Param(nameof(SkipIndexReference), false)
			.SetDisplay("Skip Index", "Ignore index correlation", "Index Reference");

		_inverseSignal = Param(nameof(InverseSignal), false)
			.SetDisplay("Inverse Signal", "Use inverse correlation logic", "Index Reference");

		_indexSecurity = Param<Security>(nameof(IndexSecurity))
			.SetDisplay("Index Security", "Reference index security", "Data")
			.SetRequired();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (IndexSecurity, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_indexFast = 0m;
		_indexSlow = 0m;
		_indexReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var indexFastEma = new ExponentialMovingAverage { Length = IndexFastEmaLength };
		var indexSlowEma = new ExponentialMovingAverage { Length = IndexSlowEmaLength };

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.Bind(fastEma, slowEma, ProcessMainCandle)
			.Start();

		var indexSub = SubscribeCandles(CandleType, security: IndexSecurity);
		indexSub
			.Bind(indexFastEma, indexSlowEma, ProcessIndexCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndexCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_indexFast = fast;
		_indexSlow = slow;
		_indexReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		bool goLong;
		bool goShort;

		if (SkipIndexReference || !_indexReady)
		{
			goLong = fast > slow;
			goShort = fast < slow;
		}
		else
		{
			goLong = fast > slow && _indexFast > _indexSlow;
			goShort = fast < slow && _indexFast < _indexSlow;

			if (InverseSignal)
			{
				goLong = fast < slow && _indexFast > _indexSlow;
				goShort = fast > slow && _indexFast < _indexSlow;
			}
		}

		if (goLong && Position <= 0)
			RegisterBuy();
		else if (goShort && Position >= 0)
			RegisterSell();
	}
}
