using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified gap strategy with optional SMA filter.
/// Trades based on selected gap direction and exits after a specified number of bars.
/// </summary>
public class SimplifiedGapWithSmaFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<int> _holdDuration;
	private readonly StrategyParam<GapTradeOptions> _tradeOption;
	private readonly StrategyParam<bool> _useSmaFilter;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private int _barsInPosition;

	/// <summary>Gap trade direction.</summary>
	public enum GapTradeOptions
	{
		LongUpGap,
		ShortDownGap,
		ShortUpGap,
		LongDownGap,
	}

	public decimal GapThreshold { get => _gapThreshold.Value; set => _gapThreshold.Value = value; }
	public int HoldDuration { get => _holdDuration.Value; set => _holdDuration.Value = value; }
	public GapTradeOptions TradeOption { get => _tradeOption.Value; set => _tradeOption.Value = value; }
	public bool UseSmaFilter { get => _useSmaFilter.Value; set => _useSmaFilter.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SimplifiedGapWithSmaFilterStrategy()
	{
		_gapThreshold = Param(nameof(GapThreshold), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Gap Threshold %", "Minimum gap size in percent", "General");

		_holdDuration = Param(nameof(HoldDuration), 10)
			.SetGreaterThanZero()
			.SetDisplay("Hold Duration", "Bars to hold position", "General");

		_tradeOption = Param(nameof(TradeOption), GapTradeOptions.LongUpGap)
			.SetDisplay("Trade Option", "Gap trading direction", "General");

		_useSmaFilter = Param(nameof(UseSmaFilter), false)
			.SetDisplay("Use SMA Filter", "Enable SMA trend filter", "SMA Filter");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period", "SMA Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0m;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_prevClose != 0m)
		{
			if (Position != 0)
			{
				_barsInPosition++;
				if (_barsInPosition >= HoldDuration)
				{
					if (Position > 0)
						SellMarket();
					else
						BuyMarket();

					_barsInPosition = 0;
				}
			}

			var open = candle.OpenPrice;
			var gap = (open - _prevClose) / _prevClose * 100m;
			var upGap = open > _prevClose && gap >= GapThreshold;
			var downGap = open < _prevClose && Math.Abs(gap) >= GapThreshold;

			var allowLong = !UseSmaFilter || close > smaValue;
			var allowShort = !UseSmaFilter || close < smaValue;

			switch (TradeOption)
			{
				case GapTradeOptions.LongUpGap when upGap && allowLong && Position <= 0:
					BuyMarket();
					break;
				case GapTradeOptions.ShortDownGap when downGap && allowShort && Position >= 0:
					SellMarket();
					break;
				case GapTradeOptions.ShortUpGap when upGap && allowShort && Position >= 0:
					SellMarket();
					break;
				case GapTradeOptions.LongDownGap when downGap && allowLong && Position <= 0:
					BuyMarket();
					break;
			}
		}

		_prevClose = close;
	}
}
