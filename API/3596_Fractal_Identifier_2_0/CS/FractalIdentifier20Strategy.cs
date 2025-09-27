using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MT5 script "Fractal Identifier 2.0" by reporting the most recent upper fractal.
/// </summary>
public class FractalIdentifier20Strategy : Strategy
{
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highHistory = new();
	private decimal? _lastFractalHigh;

	/// <summary>
	/// Gets or sets how many finished candles are scanned for the latest fractal high.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}

	/// <summary>
	/// Gets or sets the candle type used to evaluate fractal highs.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalIdentifier20Strategy"/> class.
	/// </summary>
	public FractalIdentifier20Strategy()
	{
		_lookbackBars = Param(nameof(LookbackBars), 10)
			.SetDisplay("Lookback bars", "Number of recently completed candles scanned for fractal highs.", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary candle series used to evaluate fractals.", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highHistory.Clear();
		_lastFractalHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to the configured candle series using the high level API.
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Display the base candle series to help visualize the detected fractal highs.
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Maintain a rolling buffer with the highs of recently completed candles.
		_highHistory.Enqueue(candle.HighPrice);

		var maxSize = Math.Max(5, LookbackBars + 5);
		while (_highHistory.Count > maxSize)
			_highHistory.Dequeue();

		if (_highHistory.Count < 5)
			return;

		var highs = _highHistory.ToArray();
		var lastCenter = highs.Length - 3;
		var minCenter = Math.Max(2, lastCenter - (LookbackBars - 1));

		decimal? newestFractal = null;

		// Scan from the most recent confirmed center towards older bars until a fractal high is found.
		for (var center = lastCenter; center >= minCenter; center--)
		{
			var high = highs[center];

			if (high <= highs[center - 1] || high <= highs[center - 2] ||
				high <= highs[center + 1] || high <= highs[center + 2])
			{
				continue;
			}

			newestFractal = high;
			break;
		}

		if (newestFractal == null || newestFractal == _lastFractalHigh)
			return;

		_lastFractalHigh = newestFractal;

		// Log the detected level to replicate the original indicator comment output.
		LogInfo("Most recent fractal high: {0}", newestFractal.Value);
	}
}

