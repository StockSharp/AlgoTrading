using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors the MA + RSI Expert Advisor logic from MetaTrader.
/// It sizes positions based on balance or equity and reacts to RSI and moving average signals.
/// </summary>
public class MaRsiEaStrategy : Strategy
{
	private readonly StrategyParam<LotSizingMode> _lotMode;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _balancePercent;
	private readonly StrategyParam<decimal> _equityPercent;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<CandlePriceSource> _maPrice;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<CandlePriceSource> _rsiPrice;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _maIndicator = null!;
	private RelativeStrengthIndex _rsiIndicator = null!;
	private decimal[] _maShiftBuffer = Array.Empty<decimal>();
	private int _maShiftIndex;
	private int _maShiftCount;
	private int _cachedShift = -1;

	public LotSizingMode LotOption
	{
		get => _lotMode.Value;
		set => _lotMode.Value = value;
	}

	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	public decimal PercentOfBalance
	{
		get => _balancePercent.Value;
		set => _balancePercent.Value = value;
	}

	public decimal PercentOfEquity
	{
		get => _equityPercent.Value;
		set => _equityPercent.Value = value;
	}

	public int FastMaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int FastMaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public MaMethod FastMaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public CandlePriceSource FastMaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public CandlePriceSource RsiPrice
	{
		get => _rsiPrice.Value;
		set => _rsiPrice.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MaRsiEaStrategy()
	{
		_lotMode = Param(nameof(LotOption), LotSizingMode.Balance)
			.SetDisplay("Lot Option", "How the order volume is calculated", "Position Sizing");

		_lotSize = Param(nameof(LotSize), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Fixed lot volume when using the fixed mode", "Position Sizing");

		_balancePercent = Param(nameof(PercentOfBalance), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Balance %", "Percentage of balance converted to volume", "Position Sizing");

		_equityPercent = Param(nameof(PercentOfEquity), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Equity %", "Percentage of equity converted to volume", "Position Sizing");

		_maPeriod = Param(nameof(FastMaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_maShift = Param(nameof(FastMaShift), 0)
			.SetDisplay("MA Shift", "Number of candles to shift the moving average", "Indicators");

		_maMethod = Param(nameof(FastMaMethod), MaMethod.LinearWeighted)
			.SetDisplay("MA Method", "Moving average calculation method", "Indicators");

		_maPrice = Param(nameof(FastMaPrice), CandlePriceSource.Open)
			.SetDisplay("MA Price", "Price source for the moving average", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_rsiPrice = Param(nameof(RsiPrice), CandlePriceSource.Open)
			.SetDisplay("RSI Price", "Price source fed into the RSI", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 80m)
			.SetGreaterThanZero()
			.SetDisplay("RSI Overbought", "Upper RSI threshold for short entries", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("RSI Oversold", "Lower RSI threshold for long entries", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetShiftBuffer();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maIndicator = CreateMovingAverage(FastMaMethod, FastMaPeriod);
		_rsiIndicator = new RelativeStrengthIndex { Length = RsiPeriod };

		ResetShiftBuffer();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _maIndicator);
			DrawIndicator(area, _rsiIndicator);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maPrice = GetPrice(candle, FastMaPrice);
		var rsiPrice = GetPrice(candle, RsiPrice);

		var maValue = _maIndicator.Process(maPrice, candle.OpenTime, true).ToDecimal();
		var rsiValue = _rsiIndicator.Process(rsiPrice, candle.OpenTime, true).ToDecimal();

		if (!_maIndicator.IsFormed || !_rsiIndicator.IsFormed)
			return;

		if (!TryGetShiftedMa(maValue, out var shiftedMa))
			return;

		var (buyPnL, sellPnL, cumulativePnL) = CalculateOpenPnL(candle);

		if (cumulativePnL > 0m)
		{
			CloseAllPositions();
			return;
		}

		if (cumulativePnL < 0m)
		{
			if (buyPnL > sellPnL)
			{
				BuyMarket(GetOrderVolume(candle.ClosePrice));
				return;
			}

			if (sellPnL > buyPnL)
			{
				SellMarket(GetOrderVolume(candle.ClosePrice));
				return;
			}
		}

		var openPrice = candle.OpenPrice;

		if (rsiValue >= RsiOverbought && openPrice < shiftedMa)
		{
			SellMarket(GetOrderVolume(candle.ClosePrice));
		}
		else if (rsiValue <= RsiOversold && openPrice > shiftedMa)
		{
			BuyMarket(GetOrderVolume(candle.ClosePrice));
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private (decimal buyPnL, decimal sellPnL, decimal totalPnL) CalculateOpenPnL(ICandleMessage candle)
	{
		decimal positionVolume = Position;
		if (positionVolume == 0m)
			return (0m, 0m, 0m);

		var averagePrice = Position.AveragePrice ?? 0m;
		if (averagePrice <= 0m)
			return (0m, 0m, 0m);

		var absVolume = Math.Abs(positionVolume);

		if (positionVolume > 0m)
		{
			var pnl = (candle.ClosePrice - averagePrice) * absVolume;
			return (pnl, 0m, pnl);
		}
		else
		{
			var pnl = (averagePrice - candle.ClosePrice) * absVolume;
			return (0m, pnl, pnl);
		}
	}

	private bool TryGetShiftedMa(decimal value, out decimal shifted)
	{
		var shift = Math.Max(0, FastMaShift);

		if (_cachedShift != shift)
		{
			_maShiftBuffer = new decimal[shift + 1];
			_maShiftIndex = 0;
			_maShiftCount = 0;
			_cachedShift = shift;
		}

		if (_maShiftBuffer.Length == 0)
		{
			shifted = value;
			return true;
		}

		_maShiftBuffer[_maShiftIndex] = value;
		if (_maShiftCount < _maShiftBuffer.Length)
			_maShiftCount++;

		var currentIndex = _maShiftIndex;
		_maShiftIndex = (_maShiftIndex + 1) % _maShiftBuffer.Length;

		if (_maShiftCount <= shift)
		{
			shifted = 0m;
			return false;
		}

		var index = (currentIndex - shift + _maShiftBuffer.Length) % _maShiftBuffer.Length;
		shifted = _maShiftBuffer[index];
		return true;
	}

	private decimal GetOrderVolume(decimal price)
	{
		var volume = LotSize;
		var equity = Portfolio?.CurrentValue ?? 0m;
		var normalizedPrice = price > 0m ? price : 1m;

		switch (LotOption)
		{
			case LotSizingMode.Balance:
				volume = equity > 0m ? equity * PercentOfBalance / 100m / normalizedPrice : volume;
				break;
			case LotSizingMode.Equity:
				volume = equity > 0m ? equity * PercentOfEquity / 100m / normalizedPrice : volume;
				break;
			case LotSizingMode.Fixed:
			default:
				break;
		}

		if (volume <= 0m)
			volume = LotSize;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		return volume;
	}

	private static IIndicator CreateMovingAverage(MaMethod method, int length)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = length },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePriceSource source)
	{
		return source switch
		{
			CandlePriceSource.Open => candle.OpenPrice,
			CandlePriceSource.High => candle.HighPrice,
			CandlePriceSource.Low => candle.LowPrice,
			CandlePriceSource.Close => candle.ClosePrice,
			CandlePriceSource.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePriceSource.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePriceSource.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private void ResetShiftBuffer()
	{
		_maShiftBuffer = Array.Empty<decimal>();
		_maShiftIndex = 0;
		_maShiftCount = 0;
		_cachedShift = -1;
	}
}

public enum LotSizingMode
{
	Fixed,
	Balance,
	Equity
}

public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

public enum CandlePriceSource
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}