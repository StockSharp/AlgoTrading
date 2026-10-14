using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Contrarian Donchian Channel strategy.
/// The channel spans the previous DonchianPeriod candles. A low at or below the lower band buys and a high at or above the upper
/// band sells short. Each trade has a StopLossPercent stop and a target RiskRewardRatio times farther away, and is also closed when
/// price reaches the opposite band. After a stop-loss, entries in the same direction pause for PauseCandles candles.
/// </summary>
public class ContrarianDcStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _pauseCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private int _longPause;
	private int _shortPause;

	/// <summary>
	/// Previous candles the channel spans.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candles to skip same-direction entries after a stop-loss.
	/// </summary>
	public int PauseCandles
	{
		get => _pauseCandles.Value;
		set => _pauseCandles.Value = value;
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
	public ContrarianDcStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators");

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 1.7m)
			.SetGreaterThanZero()
			.SetDisplay("Risk/Reward", "Target distance as a multiple of the stop distance", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_pauseCandles = Param(nameof(PauseCandles), 3)
			.SetNotNegative()
			.SetDisplay("Pause Candles", "Candles to skip same-direction entries after a stop-loss", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevUpper = null;
		_prevLower = null;
		_stopPrice = null;
		_takePrice = null;
		_longPause = 0;
		_shortPause = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var donchian = new DonchianChannels { Length = DonchianPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The channel is measured on the candles before this one.
		var upper = _prevUpper;
		var lower = _prevLower;

		if (donchianValue.IsFormed && donchianValue is IDonchianChannelsValue { UpperBand: decimal currentUpper, LowerBand: decimal currentLower })
		{
			_prevUpper = currentUpper;
			_prevLower = currentLower;
		}

		if (_longPause > 0)
			_longPause--;

		if (_shortPause > 0)
			_shortPause--;

		if (upper is not decimal channelHigh || lower is not decimal channelLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_longPause = PauseCandles;
				ClearLevels();
			}
			else if ((_takePrice is decimal take && candle.HighPrice >= take) || candle.HighPrice >= channelHigh)
			{
				SellMarket(Position);
				ClearLevels();
			}
			return;
		}

		if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				_shortPause = PauseCandles;
				ClearLevels();
			}
			else if ((_takePrice is decimal take && candle.LowPrice <= take) || candle.LowPrice <= channelLow)
			{
				BuyMarket(-Position);
				ClearLevels();
			}
			return;
		}

		var close = candle.ClosePrice;
		var stopDistance = close * StopLossPercent / 100m;

		if (candle.LowPrice <= channelLow && _longPause == 0)
		{
			BuyMarket(Volume);
			_stopPrice = close - stopDistance;
			_takePrice = close + stopDistance * RiskRewardRatio;
		}
		else if (candle.HighPrice >= channelHigh && _shortPause == 0)
		{
			SellMarket(Volume);
			_stopPrice = close + stopDistance;
			_takePrice = close - stopDistance * RiskRewardRatio;
		}
	}

	private void ClearLevels()
	{
		_stopPrice = null;
		_takePrice = null;
	}
}
