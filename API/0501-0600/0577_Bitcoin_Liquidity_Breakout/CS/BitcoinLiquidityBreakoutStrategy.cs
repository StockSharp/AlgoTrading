using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Liquidity Breakout strategy.
/// Long only: buys when volume exceeds LiquidityThreshold times its SMA(LiquidityPeriod), the close has risen more than
/// PriceChangeThreshold percent from the previous close, SMA(FastMaPeriod) is above SMA(SlowMaPeriod), RSI(RsiPeriod) is below 65
/// and ATR(VolatilityPeriod) is above its 10-bar SMA. The long closes when the fast SMA crosses below the slow SMA or RSI rises
/// above 70; percent stop loss and take profit protect it (0 disables either).
/// </summary>
public class BitcoinLiquidityBreakoutStrategy : Strategy
{
	private const decimal _rsiEntryMax = 65m;
	private const decimal _rsiExit = 70m;
	private const int _atrSmaLength = 10;

	private readonly StrategyParam<decimal> _liquidityThreshold;
	private readonly StrategyParam<decimal> _priceChangeThreshold;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<int> _liquidityPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _atrSma;
	private decimal? _prevClose;
	private decimal? _prevFast;
	private decimal? _prevSlow;

	/// <summary>
	/// Multiple of the volume SMA that marks high liquidity.
	/// </summary>
	public decimal LiquidityThreshold { get => _liquidityThreshold.Value; set => _liquidityThreshold.Value = value; }

	/// <summary>
	/// Minimum close-to-close rise in percent.
	/// </summary>
	public decimal PriceChangeThreshold { get => _priceChangeThreshold.Value; set => _priceChangeThreshold.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int VolatilityPeriod { get => _volatilityPeriod.Value; set => _volatilityPeriod.Value = value; }

	/// <summary>
	/// Volume SMA period.
	/// </summary>
	public int LiquidityPeriod { get => _liquidityPeriod.Value; set => _liquidityPeriod.Value = value; }

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Take profit in percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BitcoinLiquidityBreakoutStrategy()
	{
		_liquidityThreshold = Param(nameof(LiquidityThreshold), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Liquidity Threshold", "Multiple of the volume SMA that marks high liquidity", "Entry");

		_priceChangeThreshold = Param(nameof(PriceChangeThreshold), 1.5m)
			.SetDisplay("Price Change Threshold %", "Minimum close-to-close rise", "Entry");

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Period", "ATR period", "Indicators");

		_liquidityPeriod = Param(nameof(LiquidityPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Liquidity Period", "Volume SMA period", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Fast SMA period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow SMA period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 7m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit in percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_volumeSma = null;
		_atrSma = null;
		_prevClose = null;
		_prevFast = null;
		_prevSlow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevFast = null;
		_prevSlow = null;
		_volumeSma = new SimpleMovingAverage { Length = LiquidityPeriod };
		_atrSma = new SimpleMovingAverage { Length = _atrSmaLength };

		var fast = new SimpleMovingAverage { Length = FastMaPeriod };
		var slow = new SimpleMovingAverage { Length = SlowMaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var atr = new AverageTrueRange { Length = VolatilityPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, rsi, atr, ProcessCandle)
			.Start();

		var take = TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit();
		var stop = StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit();
		StartProtection(take, stop, useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeAverage = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var atrAverage = _atrSma.Process(atr, candle.ServerTime, true).ToDecimal();

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;

		_prevClose = close;
		_prevFast = fast;
		_prevSlow = slow;

		if (!_volumeSma.IsFormed || !_atrSma.IsFormed || prevClose is not decimal pc || pc <= 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var crossDown = prevFast is decimal pf && prevSlow is decimal ps && pf >= ps && fast < slow;
			if (crossDown || rsi > _rsiExit)
				SellMarket(Position);

			return;
		}

		var priceChange = (close - pc) / pc * 100m;

		if (Position == 0
			&& candle.TotalVolume > volumeAverage * LiquidityThreshold
			&& priceChange > PriceChangeThreshold
			&& fast > slow
			&& rsi < _rsiEntryMax
			&& atr > atrAverage)
		{
			BuyMarket(Volume);
		}
	}
}
