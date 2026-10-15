using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares consecutive confirmed price pivots with their MACD-line values.
/// Enters only on a later MACD/signal cross and fully exits on the reverse cross or protection.
/// </summary>
public class MacdDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMacdPeriod;
	private readonly StrategyParam<int> _slowMacdPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _divergencePeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private readonly List<(decimal High, decimal Low, decimal Macd)> _window = new();
	private (decimal Price, decimal Macd)? _lastLow;
	private (decimal Price, decimal Macd)? _lastHigh;
	private decimal? _previousMacd;
	private decimal? _previousSignal;
	private int _bar;
	private int _bullishUntil;
	private int _bearishUntil;
	private Order _pendingOrder;

	public int FastMacdPeriod { get => _fastMacdPeriod.Value; set => _fastMacdPeriod.Value = value; }
	public int SlowMacdPeriod { get => _slowMacdPeriod.Value; set => _slowMacdPeriod.Value = value; }
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }
	public int DivergencePeriod { get => _divergencePeriod.Value; set => _divergencePeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	public MacdDivergenceStrategy()
	{
		_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12).SetGreaterThanZero()
			.SetDisplay("Fast MACD Period", "Fast close EMA length", "Indicators");
		_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26).SetGreaterThanZero()
			.SetDisplay("Slow MACD Period", "Slow close EMA length", "Indicators");
		_signalPeriod = Param(nameof(SignalPeriod), 9).SetGreaterThanZero()
			.SetDisplay("Signal Period", "MACD signal EMA length", "Indicators");
		_divergencePeriod = Param(nameof(DivergencePeriod), 5).SetRange(3, 31)
			.SetDisplay("Divergence Period", "Odd-width confirmed pivot window and maximum bars to wait for a cross", "Pattern");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "MACD and pivot timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order =>
		{
			_pendingOrder = order;
			if (Position != 0m && order.Side == (Position > 0m ? Sides.Sell : Sides.Buy))
				_bullishUntil = _bearishUntil = 0;
		};
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_window.Clear();
		_lastLow = _lastHigh = null;
		_previousMacd = _previousSignal = null;
		_bar = _bullishUntil = _bearishUntil = 0;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (FastMacdPeriod >= SlowMacdPeriod || DivergencePeriod % 2 != 1)
			throw new InvalidOperationException("FastMacdPeriod must be below SlowMacdPeriod and DivergencePeriod must be odd.");
		ClearState();
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastMacdPeriod },
				LongMa = { Length = SlowMacdPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};
		var candles = SubscribeCandles(CandleType);
		candles.BindEx(macd, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native actual-fill protection evaluates executable quotes between signal candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue output)
	{
		if (candle.State != CandleStates.Finished || !output.Indicator.IsFormed ||
			output is not MovingAverageConvergenceDivergenceSignalValue value ||
			value.Macd is not decimal line || value.Signal is not decimal signal)
			return;
		_bar++;
		var oldLine = _previousMacd;
		var oldSignal = _previousSignal;
		var upCross = oldLine is decimal oldUpLine && oldSignal is decimal oldUpSignal &&
			oldUpLine <= oldUpSignal && line > signal;
		var downCross = oldLine is decimal oldDownLine && oldSignal is decimal oldDownSignal &&
			oldDownLine >= oldDownSignal && line < signal;
		var bullishReady = _bullishUntil >= _bar;
		var bearishReady = _bearishUntil >= _bar;
		_previousMacd = line;
		_previousSignal = signal;
		_window.Add((candle.HighPrice, candle.LowPrice, line));
		if (_window.Count == DivergencePeriod)
		{
			var centerIndex = DivergencePeriod / 2;
			var center = _window[centerIndex];
			if (_window.Where((_, index) => index != centerIndex).All(bar => center.Low < bar.Low))
			{
				var divergence = _lastLow is { } previousLow &&
					center.Low < previousLow.Price && center.Macd > previousLow.Macd;
				_lastLow = (center.Low, center.Macd);
				_bullishUntil = divergence ? _bar + DivergencePeriod : 0;
			}
			if (_window.Where((_, index) => index != centerIndex).All(bar => center.High > bar.High))
			{
				var divergence = _lastHigh is { } previousHigh &&
					center.High > previousHigh.Price && center.Macd < previousHigh.Macd;
				_lastHigh = (center.High, center.Macd);
				_bearishUntil = divergence ? _bar + DivergencePeriod : 0;
			}
			_window.RemoveAt(0);
		}
		if (!IsFormedAndOnlineAndAllowTrading() ||
			_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m && downCross)
		{
			SellMarket(Position);
			_bullishUntil = _bearishUntil = 0;
		}
		else if (Position < 0m && upCross)
		{
			BuyMarket(Math.Abs(Position));
			_bullishUntil = _bearishUntil = 0;
		}
		else if (Position == 0m)
		{
			if (bullishReady && upCross)
			{
				BuyMarket(Volume);
				_bullishUntil = _bearishUntil = 0;
			}
			else if (bearishReady && downCross)
			{
				SellMarket(Volume);
				_bullishUntil = _bearishUntil = 0;
			}
		}
	}
}
