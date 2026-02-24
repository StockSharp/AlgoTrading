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
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for RSI calculations", "General");

		_fastLength = Param(nameof(FastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI", "Fast RSI period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 21)
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

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}
}
