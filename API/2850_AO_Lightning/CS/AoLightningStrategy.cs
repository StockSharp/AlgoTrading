using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AO Lightning strategy.
/// Pyramids positions based on the Awesome Oscillator momentum slope.
/// </summary>
public class AoLightningStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _aoShortPeriod;
	private readonly StrategyParam<int> _aoLongPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AwesomeOscillator _awesomeOscillator = null!;
	private decimal? _previousAo;

	/// <summary>
	/// Volume used for each incremental entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of entries allowed per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Fast SMA length inside the Awesome Oscillator.
	/// </summary>
	public int AoShortPeriod
	{
		get => _aoShortPeriod.Value;
		set => _aoShortPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA length inside the Awesome Oscillator.
	/// </summary>
	public int AoLongPeriod
	{
		get => _aoLongPeriod.Value;
		set => _aoLongPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AoLightningStrategy"/>.
	/// </summary>
	public AoLightningStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order size per entry", "Trading")
			.SetCanOptimize(true);

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetDisplay("Max Positions", "Maximum entries per side", "Trading")
			.SetCanOptimize(true);

		_aoShortPeriod = Param(nameof(AoShortPeriod), 5)
			.SetDisplay("AO Fast", "Short SMA period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true);

		_aoLongPeriod = Param(nameof(AoLongPeriod), 34)
			.SetDisplay("AO Slow", "Long SMA period for Awesome Oscillator", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Source candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousAo = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_awesomeOscillator = new AwesomeOscillator
		{
			ShortPeriod = AoShortPeriod,
			LongPeriod = AoLongPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_awesomeOscillator, ProcessCandle)
			.Start();

		// Plot candles and the oscillator for visual monitoring.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _awesomeOscillator);
			DrawOwnTrades(area);
		}

		// Enable built-in protections (e.g. stop-loss management) once at start.
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal aoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Store the first oscillator reading and wait for a reference slope.
		if (_previousAo is null)
		{
			_previousAo = aoValue;
			return;
		}

		var volume = TradeVolume;
		if (volume <= 0)
		{
			_previousAo = aoValue;
			return;
		}

		var maxPositions = MaxPositions;
		if (maxPositions <= 0)
		{
			_previousAo = aoValue;
			return;
		}

		var maxExposure = maxPositions * volume;

		// Falling AO slope favours long accumulation.
		if (aoValue < _previousAo)
		{
			if (Position < 0)
			{
				var orderVolume = Math.Abs(Position) + volume;
				if (volume <= maxExposure)
					BuyMarket(orderVolume);
			}
			else if (Position + volume <= maxExposure)
			{
				BuyMarket(volume);
			}
		}
		// Rising AO slope favours short accumulation.
		else if (aoValue > _previousAo)
		{
			if (Position > 0)
			{
				var orderVolume = Position + volume;
				if (volume <= maxExposure)
					SellMarket(orderVolume);
			}
			else if (Math.Abs(Position) + volume <= maxExposure)
			{
				SellMarket(volume);
			}
		}

		_previousAo = aoValue;
	}
}
