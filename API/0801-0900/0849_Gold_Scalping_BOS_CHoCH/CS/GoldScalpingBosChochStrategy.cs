using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold scalping BOS and CHoCH strategy.
/// The last swing high and low are the highest high and lowest low of the SwingLength candles before the current one. A high above the
/// last swing high (break of structure) together with a close crossing above the last swing low (change of character) goes long; a low
/// below the last swing low with a close crossing below the last swing high goes short. The stop sits at the RecentLength low (high) and
/// the target TakeProfitFactor times that risk away.
/// </summary>
public class GoldScalpingBosChochStrategy : Strategy
{
	private readonly StrategyParam<int> _recentLength;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _swingHigh;
	private decimal? _swingLow;
	private decimal? _prevSwingHigh;
	private decimal? _prevSwingLow;
	private decimal? _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Candles used for the stop level.
	/// </summary>
	public int RecentLength
	{
		get => _recentLength.Value;
		set => _recentLength.Value = value;
	}

	/// <summary>
	/// Candles used for the swing levels.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
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
	public GoldScalpingBosChochStrategy()
	{
		_recentLength = Param(nameof(RecentLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Recent Length", "Candles used for the stop level", "Structure");

		_swingLength = Param(nameof(SwingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Swing Length", "Candles used for the swing levels", "Structure");

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Factor", "Target distance as a multiple of the stop distance", "Risk");

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
		_swingHigh = null;
		_swingLow = null;
		_prevSwingHigh = null;
		_prevSwingLow = null;
		_prevClose = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var swingHighest = new Highest { Length = SwingLength };
		var swingLowest = new Lowest { Length = SwingLength };
		var recentHighest = new Highest { Length = RecentLength };
		var recentLowest = new Lowest { Length = RecentLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(swingHighest, swingLowest, recentHighest, recentLowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal swingHighest, decimal swingLowest, decimal recentHighest, decimal recentLowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Swing levels of the candles before this one, and the levels one candle earlier for the cross.
		var lastSwingHigh = _swingHigh;
		var lastSwingLow = _swingLow;
		var prevSwingHigh = _prevSwingHigh;
		var prevSwingLow = _prevSwingLow;
		var prevClose = _prevClose;

		_prevSwingHigh = _swingHigh;
		_prevSwingLow = _swingLow;
		_swingHigh = swingHighest;
		_swingLow = swingLowest;
		_prevClose = candle.ClosePrice;

		if (ManagePosition(candle))
			return;

		if (lastSwingHigh is not decimal sh || lastSwingLow is not decimal sl || prevSwingHigh is not decimal psh || prevSwingLow is not decimal psl || prevClose is not decimal pc)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || Position != 0)
			return;

		var close = candle.ClosePrice;

		if (candle.HighPrice > sh && pc <= psl && close > sl && recentLowest < close)
		{
			BuyMarket(Volume);
			_stopPrice = recentLowest;
			_takePrice = close + (close - recentLowest) * TakeProfitFactor;
		}
		else if (candle.LowPrice < sl && pc >= psh && close < sh && recentHighest > close)
		{
			SellMarket(Volume);
			_stopPrice = recentHighest;
			_takePrice = close - (recentHighest - close) * TakeProfitFactor;
		}
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
