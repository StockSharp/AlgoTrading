using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uses fast and slow RSI crossovers to detect entry signals.
/// Simplified from the "RSI Dual Cloud EA" MetaTrader strategy.
/// </summary>
public class RsiDualCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;

	private RelativeStrengthIndex _fastRsi;
	private RelativeStrengthIndex _slowRsi;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast RSI period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow RSI period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public RsiDualCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for RSI calculations", "General");

		_fastLength = Param(nameof(FastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI", "Fast RSI period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 42)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI", "Slow RSI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = null;
		_prevSlow = null;

		_fastRsi = new RelativeStrengthIndex { Length = FastLength };
		_slowRsi = new RelativeStrengthIndex { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastRsi, _slowRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastRsi.IsFormed || !_slowRsi.IsFormed)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		var crossUp = _prevFast < _prevSlow && fastValue > slowValue;
		var crossDown = _prevFast > _prevSlow && fastValue < slowValue;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		var minSpread = 5m;

		if (crossUp && Math.Abs(fastValue - slowValue) >= minSpread)
		{
			if (Position <= 0)
				BuyMarket(Position < 0 ? Math.Abs(Position) + volume : volume);
		}
		else if (crossDown && Math.Abs(fastValue - slowValue) >= minSpread)
		{
			if (Position >= 0)
				SellMarket(Position > 0 ? Math.Abs(Position) + volume : volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_fastRsi = null;
		_slowRsi = null;
		_prevFast = null;
		_prevSlow = null;

		base.OnReseted();
	}
}
