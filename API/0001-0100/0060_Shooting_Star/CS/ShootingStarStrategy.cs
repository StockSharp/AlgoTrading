using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only shooting star after three rising closes, optionally confirmed by the next candle.
/// Protects above the pattern high with executable best asks and finished-bar fallback.
/// </summary>
public class ShootingStarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _shadowToBodyRatio;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _confirmationRequired;

	private readonly Queue<decimal> _priorCloses = new();
	private (decimal High, decimal Close)? _candidate;
	private decimal? _patternStop;
	private Order _entryOrder;
	private Order _exitOrder;

	public decimal ShadowToBodyRatio { get => _shadowToBodyRatio.Value; set => _shadowToBodyRatio.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool ConfirmationRequired { get => _confirmationRequired.Value; set => _confirmationRequired.Value = value; }

	public ShootingStarStrategy()
	{
		_shadowToBodyRatio = Param(nameof(ShadowToBodyRatio), 2m).SetGreaterThanZero()
			.SetDisplay("Shadow/body ratio", "Minimum upper-shadow to real-body ratio", "Pattern");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for shooting-star pattern", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop above high (%)", "Percent buffer above the star high; zero places it at the high.", "Protection");
		_confirmationRequired = Param(nameof(ConfirmationRequired), true)
			.SetDisplay("Confirmation required", "Wait for the next candle to close below the star close.", "Pattern");
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
		_priorCloses.Clear();
		_candidate = null;
		_patternStop = null;
		_entryOrder = _exitOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		var asks = new Subscription(DataType.Level1, Security);
		asks.MarketData.BuildField = Level1Fields.BestAskPrice;
		SubscribeLevel1(asks).Bind(ProcessAsk).Start();
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private static bool IsPending(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessAsk(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			CheckStop(ask);
	}

	private void CheckStop(decimal executableAsk)
	{
		if (Position < 0m && _patternStop is decimal stop && executableAsk >= stop && !IsPending(_exitOrder))
		{
			_exitOrder = BuyMarket(Math.Abs(Position));
			_candidate = null;
		}
	}

	private void EnterShort(decimal patternHigh)
	{
		_patternStop = patternHigh * (1m + StopLossPercent / 100m);
		_entryOrder = SellMarket(Volume);
		_candidate = null;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var advance = _priorCloses.Count == 3;
		if (advance)
		{
			var closes = _priorCloses.ToArray();
			advance = closes[0] < closes[1] && closes[1] < closes[2];
			_priorCloses.Dequeue();
		}
		_priorCloses.Enqueue(candle.ClosePrice);
		if (Position < 0m)
		{
			CheckStop(candle.HighPrice);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
			return;
		if (_candidate is { } star)
		{
			_candidate = null;
			if (candle.ClosePrice < star.Close && IsFormedAndOnlineAndAllowTrading())
			{
				EnterShort(star.High);
				return;
			}
		}
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upper = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
		var lower = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		if (!advance || body <= 0m || upper < body * ShadowToBodyRatio || lower > body * 0.5m ||
			!IsFormedAndOnlineAndAllowTrading())
			return;
		if (ConfirmationRequired)
			_candidate = (candle.HighPrice, candle.ClosePrice);
		else
			EnterShort(candle.HighPrice);
	}
}
