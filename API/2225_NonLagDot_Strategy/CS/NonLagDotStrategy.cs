using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on NonLagDot indicator trend changes.
/// Uses a simple moving average to approximate NonLagDot behavior.
/// When the moving average slope turns positive, a long position is opened.
/// When the slope turns negative, a short position is opened.
/// Opposite positions are closed before opening new ones.
/// </summary>
public class NonLagDotStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SMA _sma;
	private decimal? _prevSma;
	private int _prevTrend;

	/// <summary>
	/// Moving average length approximating NonLagDot.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public NonLagDotStrategy()
	{
		_length = Param(nameof(Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Moving average period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percent based stop-loss", "Risk");
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

		_sma = new SMA { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		StartProtection(new(), new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check strategy readiness
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Store first value and exit
		if (_prevSma is null)
		{
			_prevSma = sma;
			return;
		}

		// Determine current trend based on slope
		var trend = sma > _prevSma ? 1 : sma < _prevSma ? -1 : _prevTrend;

		// Open long on upward change
		if (trend > 0 && _prevTrend < 0 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Open short on downward change
		else if (trend < 0 && _prevTrend > 0 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevTrend = trend;
		_prevSma = sma;
	}
}
