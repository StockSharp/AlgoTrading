using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades AUDUSD daily MACD crossovers.
/// </summary>
public class MacdCrossAudusdD1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsMacdAboveSignal;
	private bool _hasPrev;
	private decimal _pipSize;

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit to stop loss ratio.
	/// </summary>
	public decimal RewardRatio
	{
		get => _rewardRatio.Value;
		set => _rewardRatio.Value = value;
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
	/// Initializes <see cref="MacdCrossAudusdD1Strategy"/>.
	/// </summary>
	public MacdCrossAudusdD1Strategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order volume in lots", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 40)
			.SetDisplay("Stop Loss (pips)", "Stop loss size in pips", "Risk")
			.SetGreaterThanZero();

		_rewardRatio = Param(nameof(RewardRatio), 3m)
			.SetDisplay("Reward Ratio", "Take profit multiple of stop loss", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_prevIsMacdAboveSignal = false;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = (Security?.PriceStep ?? 0.0001m) * 10m;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(StopLossPips * RewardRatio * _pipSize, UnitTypes.Absolute),
			new Unit(StopLossPips * _pipSize, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hour = candle.OpenTime.LocalDateTime.Hour;
		if (hour <= 5 || hour >= 15)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var isMacdAboveSignal = macd > signal;

		if (!_hasPrev)
		{
			_prevIsMacdAboveSignal = isMacdAboveSignal;
			_hasPrev = true;
			return;
		}

		var crossedUp = isMacdAboveSignal && !_prevIsMacdAboveSignal;
		var crossedDown = !isMacdAboveSignal && _prevIsMacdAboveSignal;

		if (crossedUp && Position == 0)
		{
			BuyMarket(Volume);
		}
		else if (crossedDown && Position == 0)
		{
			SellMarket(Volume);
		}

		_prevIsMacdAboveSignal = isMacdAboveSignal;
	}
}
