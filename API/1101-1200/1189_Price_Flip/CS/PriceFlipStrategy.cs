using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price Flip strategy using mirrored range price and SMA crossover confirmation.
/// </summary>
public class PriceFlipStrategy : Strategy
{
	private readonly StrategyParam<int> _tickerMaxLookback;
	private readonly StrategyParam<int> _tickerMinLookback;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = [];
	private decimal? _previousClose;
	private decimal? _previousInverted;
	private decimal? _previousFast;
	private decimal? _previousSlow;

	public int TickerMaxLookback { get => _tickerMaxLookback.Value; set => _tickerMaxLookback.Value = value; }
	public int TickerMinLookback { get => _tickerMinLookback.Value; set => _tickerMinLookback.Value = value; }
	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public bool UseTrendFilter { get => _useTrendFilter.Value; set => _useTrendFilter.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PriceFlipStrategy()
	{
		_tickerMaxLookback = Param(nameof(TickerMaxLookback), 100).SetGreaterThanZero();
		_tickerMinLookback = Param(nameof(TickerMinLookback), 100).SetGreaterThanZero();
		_fastMaLength = Param(nameof(FastMaLength), 12).SetGreaterThanZero();
		_slowMaLength = Param(nameof(SlowMaLength), 14).SetGreaterThanZero();
		_useTrendFilter = Param(nameof(UseTrendFilter), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
		_previousClose = null;
		_previousInverted = null;
		_previousFast = null;
		_previousSlow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		var keep = Math.Max(Math.Max(TickerMaxLookback, TickerMinLookback), SlowMaLength);
		if (_candles.Count > keep)
			_candles.RemoveRange(0, _candles.Count - keep);

		if (_candles.Count < Math.Max(Math.Max(TickerMaxLookback, TickerMinLookback), SlowMaLength))
			return;

		var fast = _candles.Skip(_candles.Count - FastMaLength).Average(c => c.ClosePrice);
		var slow = _candles.Skip(_candles.Count - SlowMaLength).Average(c => c.ClosePrice);

		if (_previousClose is decimal previousClose &&
			_previousInverted is decimal previousInverted &&
			_previousFast is decimal previousFast &&
			_previousSlow is decimal previousSlow)
		{
			var signal = GetSignal(
				previousClose,
				previousInverted,
				previousFast,
				previousSlow,
				fast,
				slow,
				candle.ClosePrice,
				UseTrendFilter);

			if (signal > 0 && Position <= 0m)
				BuyMarket(Volume + Math.Abs(Position));
			else if (signal < 0 && Position >= 0m)
				SellMarket(Volume + Math.Abs(Position));
		}

		var recentHigh = _candles.Skip(_candles.Count - TickerMaxLookback).Max(c => c.HighPrice);
		var recentLow = _candles.Skip(_candles.Count - TickerMinLookback).Min(c => c.LowPrice);

		_previousClose = candle.ClosePrice;
		_previousInverted = CalculateInvertedPrice(recentHigh, recentLow, candle.ClosePrice);
		_previousFast = fast;
		_previousSlow = slow;
	}

	internal static decimal CalculateInvertedPrice(decimal recentHigh, decimal recentLow, decimal price)
		=> recentHigh + recentLow - price;

	internal static int GetSignal(
		decimal previousClose,
		decimal previousInverted,
		decimal previousFast,
		decimal previousSlow,
		decimal fast,
		decimal slow,
		decimal currentClose,
		bool useTrendFilter)
	{
		var crossUp = previousFast <= previousSlow && fast > slow;
		var crossDown = previousFast >= previousSlow && fast < slow;

		if (previousClose > previousInverted &&
			crossUp &&
			(!useTrendFilter || currentClose > slow))
			return 1;

		if (previousClose < previousInverted &&
			crossDown &&
			(!useTrendFilter || currentClose < slow))
			return -1;

		return 0;
	}
}
