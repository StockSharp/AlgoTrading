using System;
using System.Collections.Generic;
using System.Text;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays a matrix of up or down arrows comparing current price with past values.
/// </summary>
public class SMADirectionalMatrixLuxAlgoStrategy : Strategy
{
	private readonly StrategyParam<int> _min;
	private readonly StrategyParam<int> _max;
	private readonly StrategyParam<int> _columns;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(Momentum mom, int length)> _momentums = new();
	private readonly StringBuilder _builder = new();
	private DateTimeOffset _lastTime;
	private int _processed;

	/// <summary>
	/// Minimum lookback.
	/// </summary>
	public int Min
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	/// <summary>
	/// Maximum lookback.
	/// </summary>
	public int Max
	{
		get => _max.Value;
		set => _max.Value = value;
	}

	/// <summary>
	/// Number of columns in matrix.
	/// </summary>
	public int Columns
	{
		get => _columns.Value;
		set => _columns.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SMADirectionalMatrixLuxAlgoStrategy()
	{
		_min = Param(nameof(Min), 15)
			.SetDisplay("Min Lookback", "Minimum lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_max = Param(nameof(Max), 28)
			.SetDisplay("Max Lookback", "Maximum lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 1);

		_columns = Param(nameof(Columns), 4)
			.SetDisplay("Columns", "Number of columns in matrix", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 6, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_momentums.Clear();
		_builder.Clear();
		_lastTime = default;
		_processed = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		for (var i = Min; i <= Max; i++)
		{
			var mom = new Momentum { Length = i };
			var len = i;
			_momentums.Add((mom, len));
		}

		var subscription = SubscribeCandles(CandleType);

		foreach (var (mom, len) in _momentums)
		{
			var m = mom;
			var l = len;
			subscription.Bind(m, (c, v) => ProcessMomentum(c, v, l, m));
		}

		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMomentum(ICandleMessage candle, decimal value, int length, Momentum momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!momentum.IsFormed)
			return;

		if (candle.OpenTime != _lastTime)
		{
			_lastTime = candle.OpenTime;
			_processed = 0;
			_builder.Clear();
		}

		_processed++;

		var sym = value > 0 ? "📈" : "📉";
		var per = Pad(length, Max);
		var space = (length - Min) % Columns == Columns - 1 ? "\n\n" : string.Empty;

		_builder.Append('｜').Append(per).Append(" : ").Append(sym).Append(space);

		if (_processed == _momentums.Count)
			AddInfoLog(_builder.ToString());
	}

	private static string Pad(int value, int max)
	{
		var digitsMax = Digits(max);
		var digitsValue = Digits(value);
		return new string('0', digitsMax - digitsValue) + value;
	}

	private static int Digits(int x)
	{
		return x >= 100 ? 3 : x >= 10 ? 2 : 1;
	}
}
