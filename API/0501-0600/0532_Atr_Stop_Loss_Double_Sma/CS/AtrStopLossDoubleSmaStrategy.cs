using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR stop-loss double SMA strategy.
/// A fast SMA crossing above the slow SMA goes long and a cross below goes short, reversing an opposite position. Each entry
/// fixes a stop-loss AtrMultiplier ATRs from the entry close; an AtrMultiplier of 0 disables it.
/// </summary>
public class AtrStopLossDoubleSmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _stopPrice;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Constructor.
	/// </summary>
	public AtrStopLossDoubleSmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 45)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop-loss distance in ATR multiples, 0 disables it", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var distance = AtrMultiplier * atr;

		if (pf <= ps && fast > slow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - distance;
		}
		else if (pf >= ps && fast < slow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + distance;
		}
		else if (AtrMultiplier > 0m)
		{
			if (Position > 0 && candle.LowPrice <= _stopPrice)
				SellMarket(Position);
			else if (Position < 0 && candle.HighPrice >= _stopPrice)
				BuyMarket(-Position);
		}
	}
}
