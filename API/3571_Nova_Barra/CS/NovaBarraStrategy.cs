using System;

using StockSharp.Algo.Strategies;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader example NovaBarra.mq5.
/// Demonstrates how to react to the first detected bar and subsequent new bars using the high-level candle API.
/// </summary>
public class NovaBarraStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _previousCandleTime;
	private bool _isFirstCandle = true;

	public NovaBarraStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Time frame that drives bar detection.", "General")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Candle type used to detect finished bars.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(OnProcessCandle)
			.Start();
	}

	private void OnProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			OnTickInsideCurrentBar(candle);
			return;
		}

		var currentTime = candle.OpenTime;

		if (_isFirstCandle)
		{
			_previousCandleTime = currentTime;
			_isFirstCandle = false;

			OnFirstBar(currentTime, candle);
			OnNewBarCommonWork(candle);
			return;
		}

		if (_previousCandleTime == currentTime)
		{
			OnTickInsideCurrentBar(candle);
			return;
		}

		_previousCandleTime = currentTime;

		OnRegularBar(currentTime, candle);
		OnNewBarCommonWork(candle);
	}

	private void OnFirstBar(DateTimeOffset currentTime, ICandleMessage candle)
	{
		AddInfoLog($"Initial bar detected at {currentTime:O}, {candle.SecurityId} has already progressed.");
	}

	private void OnRegularBar(DateTimeOffset currentTime, ICandleMessage candle)
	{
		AddInfoLog($"New bar started at {currentTime:O} with open {candle.OpenPrice}.");
	}

	private void OnNewBarCommonWork(ICandleMessage candle)
	{
		AddDebugLog($"Processing bar {candle.OpenTime:O} close {candle.ClosePrice}.");
	}

	private void OnTickInsideCurrentBar(ICandleMessage candle)
	{
		AddDebugLog($"Update received inside ongoing bar {candle.OpenTime:O}.");
	}
}
