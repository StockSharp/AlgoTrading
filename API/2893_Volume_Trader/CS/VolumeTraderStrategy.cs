namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Volume based reversal strategy that reacts to increasing or decreasing tick volume.
/// </summary>
public class VolumeTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _volume;

	private decimal? _previousVolume;
	private decimal? _previousPreviousVolume;

	/// <summary>
	/// Candle type used to calculate the signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Inclusive start hour of the trading session.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Inclusive end hour of the trading session.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Order volume used when opening a new position.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VolumeTraderStrategy"/>.
	/// </summary>
	public VolumeTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for signal calculation", "General");

		_startHour = Param(nameof(StartHour), 9)
			.SetDisplay("Start Hour", "Inclusive start hour for trading", "Session")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 18)
			.SetDisplay("End Hour", "Inclusive end hour for trading", "Session")
			.SetRange(0, 23);

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume for entries", "Trading");
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

		_previousVolume = null;
		_previousPreviousVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Wait until the candle is finished to avoid partial data.
		if (candle.State != CandleStates.Finished)
			return;

		var currentVolume = candle.TotalVolume;

		if (_previousVolume.HasValue && _previousPreviousVolume.HasValue)
		{
			// MQL version trades at the open of the next bar, so use the next bar time for the filter.
			var nextBarTime = candle.CloseTime;
			var hour = nextBarTime.Hour;
			var inSession = hour >= StartHour && hour <= EndHour;

			if (inSession && IsFormedAndOnlineAndAllowTrading())
			{
				var prevVolume = _previousVolume.Value;
				var prevPrevVolume = _previousPreviousVolume.Value;

				// Rising volume suggests upward pressure -> go long.
				if (prevVolume > prevPrevVolume && Position <= 0)
				{
					var volumeToTrade = Volume + (Position < 0 ? Math.Abs(Position) : 0m);

					if (volumeToTrade > 0)
					{
						BuyMarket(volumeToTrade);
						LogInfo($"Volume increased from {prevPrevVolume} to {prevVolume}. Opening long position.");
					}
				}
				// Falling volume suggests weakening demand -> go short.
				else if (prevVolume < prevPrevVolume && Position >= 0)
				{
					var volumeToTrade = Volume + (Position > 0 ? Math.Abs(Position) : 0m);

					if (volumeToTrade > 0)
					{
						SellMarket(volumeToTrade);
						LogInfo($"Volume decreased from {prevPrevVolume} to {prevVolume}. Opening short position.");
					}
				}
			}
		}

		// Shift stored volumes so the latest closed candle becomes the previous reference.
		_previousPreviousVolume = _previousVolume;
		_previousVolume = currentVolume;
	}
}
