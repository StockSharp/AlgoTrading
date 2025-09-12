using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Screens sector indices for sigma spikes based on return volatility.
/// </summary>
public class IndicesSectorSigmaSpikesStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _returnPeriod;
	private readonly StrategyParam<decimal> _sigmaThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<Security> _index2;
	private readonly StrategyParam<Security> _index3;
	private readonly StrategyParam<Security> _index4;
	private readonly StrategyParam<Security> _index5;
	private readonly StrategyParam<Security> _index6;
	private readonly StrategyParam<Security> _index7;
	private readonly StrategyParam<Security> _index8;
	private readonly StrategyParam<Security> _index9;
	private readonly StrategyParam<Security> _index10;
	private readonly StrategyParam<Security> _index11;
	private readonly StrategyParam<Security> _index12;
	private readonly StrategyParam<Security> _index13;

	private readonly Dictionary<Security, SigmaCalculator> _calculators = [];

	/// <summary>
	/// Lookback period for sigma calculation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Period for percentage return calculation.
	/// </summary>
	public int ReturnPeriod
	{
		get => _returnPeriod.Value;
		set => _returnPeriod.Value = value;
	}

	/// <summary>
	/// Threshold for significant sigma spikes.
	/// </summary>
	public decimal SigmaThreshold
	{
		get => _sigmaThreshold.Value;
		set => _sigmaThreshold.Value = value;
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
	/// Second index.
	/// </summary>
	public Security Index2
	{
		get => _index2.Value;
		set => _index2.Value = value;
	}

	/// <summary>
	/// Third index.
	/// </summary>
	public Security Index3
	{
		get => _index3.Value;
		set => _index3.Value = value;
	}

	/// <summary>
	/// Fourth index.
	/// </summary>
	public Security Index4
	{
		get => _index4.Value;
		set => _index4.Value = value;
	}

	/// <summary>
	/// Fifth index.
	/// </summary>
	public Security Index5
	{
		get => _index5.Value;
		set => _index5.Value = value;
	}

	/// <summary>
	/// Sixth index.
	/// </summary>
	public Security Index6
	{
		get => _index6.Value;
		set => _index6.Value = value;
	}

	/// <summary>
	/// Seventh index.
	/// </summary>
	public Security Index7
	{
		get => _index7.Value;
		set => _index7.Value = value;
	}

	/// <summary>
	/// Eighth index.
	/// </summary>
	public Security Index8
	{
		get => _index8.Value;
		set => _index8.Value = value;
	}

	/// <summary>
	/// Ninth index.
	/// </summary>
	public Security Index9
	{
		get => _index9.Value;
		set => _index9.Value = value;
	}

	/// <summary>
	/// Tenth index.
	/// </summary>
	public Security Index10
	{
		get => _index10.Value;
		set => _index10.Value = value;
	}

	/// <summary>
	/// Eleventh index.
	/// </summary>
	public Security Index11
	{
		get => _index11.Value;
		set => _index11.Value = value;
	}

	/// <summary>
	/// Twelfth index.
	/// </summary>
	public Security Index12
	{
		get => _index12.Value;
		set => _index12.Value = value;
	}

	/// <summary>
	/// Thirteenth index.
	/// </summary>
	public Security Index13
	{
		get => _index13.Value;
		set => _index13.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicesSectorSigmaSpikesStrategy"/> class.
	/// </summary>
	public IndicesSectorSigmaSpikesStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Lookback Period", "Number of returns used for sigma calculation", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 10);

		_returnPeriod = Param(nameof(ReturnPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Return Period", "Period for percentage return calculation", "Parameters");

		_sigmaThreshold = Param(nameof(SigmaThreshold), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Sigma Threshold", "Threshold for significant moves", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");

		_index2 = Param<Security>(nameof(Index2), null).SetDisplay("Index 02", "Second sector index", "Universe");
		_index3 = Param<Security>(nameof(Index3), null).SetDisplay("Index 03", "Third sector index", "Universe");
		_index4 = Param<Security>(nameof(Index4), null).SetDisplay("Index 04", "Fourth sector index", "Universe");
		_index5 = Param<Security>(nameof(Index5), null).SetDisplay("Index 05", "Fifth sector index", "Universe");
		_index6 = Param<Security>(nameof(Index6), null).SetDisplay("Index 06", "Sixth sector index", "Universe");
		_index7 = Param<Security>(nameof(Index7), null).SetDisplay("Index 07", "Seventh sector index", "Universe");
		_index8 = Param<Security>(nameof(Index8), null).SetDisplay("Index 08", "Eighth sector index", "Universe");
		_index9 = Param<Security>(nameof(Index9), null).SetDisplay("Index 09", "Ninth sector index", "Universe");
		_index10 = Param<Security>(nameof(Index10), null).SetDisplay("Index 10", "Tenth sector index", "Universe");
		_index11 = Param<Security>(nameof(Index11), null).SetDisplay("Index 11", "Eleventh sector index", "Universe");
		_index12 = Param<Security>(nameof(Index12), null).SetDisplay("Index 12", "Twelfth sector index", "Universe");
		_index13 = Param<Security>(nameof(Index13), null).SetDisplay("Index 13", "Thirteenth sector index", "Universe");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
			yield return (Security, CandleType);

		var others = new[] { Index2, Index3, Index4, Index5, Index6, Index7, Index8, Index9, Index10, Index11, Index12, Index13 };
		foreach (var sec in others)
		{
			if (sec != null)
				yield return (sec, CandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_calculators.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		AddSecurity(Security);
		AddSecurity(Index2);
		AddSecurity(Index3);
		AddSecurity(Index4);
		AddSecurity(Index5);
		AddSecurity(Index6);
		AddSecurity(Index7);
		AddSecurity(Index8);
		AddSecurity(Index9);
		AddSecurity(Index10);
		AddSecurity(Index11);
		AddSecurity(Index12);
		AddSecurity(Index13);
	}

	private void AddSecurity(Security sec)
	{
		if (sec == null)
			return;

		_calculators[sec] = new SigmaCalculator(LookbackPeriod, ReturnPeriod);

		var subscription = SubscribeCandles(CandleType, security: sec);
		subscription.Bind(c => ProcessCandle(c, sec)).Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_calculators.TryGetValue(security, out var calc))
			return;

		var (sigma, ret, xRet) = calc.Update(candle.ClosePrice);

		if (sigma is not decimal s || ret is not decimal r || xRet is not decimal xr)
			return;

		var category = s > SigmaThreshold
			? "significant gain"
			: s >= 0m
				? "weak gain"
				: s < -SigmaThreshold
					? "significant loss"
					: "weak loss";

		LogInfo($"{security.Id}: sigma={s:0.00}, return={r:0.00}%, {ReturnPeriod} period return={xr:0.00}% -> {category}");
	}

	private sealed class SigmaCalculator
	{
		private readonly StdDev _stdDev;
		private readonly Queue<decimal> _closeBuffer = new();
		private readonly int _retLen;
		private decimal? _prevClose;

		public SigmaCalculator(int length, int retLen)
		{
			_stdDev = new StdDev { Length = length };
			_retLen = retLen;
		}

		public (decimal? sigma, decimal? ret, decimal? xRet) Update(decimal close)
		{
			decimal? ret = null;
			decimal? xRet = null;

			if (_prevClose is not null)
			{
				ret = (close - _prevClose.Value) / _prevClose.Value;
				var sd = _stdDev.Process(ret.Value).GetValue<decimal>();
				var sigma = _stdDev.IsFormed && sd != 0m ? ret.Value / sd : (decimal?)null;

				_closeBuffer.Enqueue(close);
				if (_closeBuffer.Count > _retLen)
					_closeBuffer.Dequeue();

				if (_closeBuffer.Count == _retLen)
				{
					var first = _closeBuffer.Peek();
					xRet = (close - first) / first;
				}

				_prevClose = close;
				return (sigma, ret * 100m, xRet * 100m);
			}

			_prevClose = close;
			_closeBuffer.Enqueue(close);
			return (null, null, null);
		}
	}
}
