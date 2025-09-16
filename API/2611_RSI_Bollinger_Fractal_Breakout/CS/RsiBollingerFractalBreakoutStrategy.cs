using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines RSI-based Bollinger Bands with fractal breakouts and Parabolic SAR trailing.
/// </summary>
public class RsiBollingerFractalBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _indentPips;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _sarTrailingPips;
	private readonly StrategyParam<DataType> _candleType;
	
	private RelativeStrengthIndex _rsi = null!;
	private BollingerBands _bollinger = null!;
	private ParabolicSar _parabolicSar = null!;
	
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	
	private decimal? _pendingLongEntry;
	private decimal? _pendingLongStop;
	private decimal? _pendingLongTake;
	private decimal? _pendingShortEntry;
	private decimal? _pendingShortStop;
	private decimal? _pendingShortTake;
	
	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	
	private decimal _pipSize;
	
	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;
	private int _fractalCount;
	
	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands period applied to RSI values.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}
	
	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}
	
	/// <summary>
	/// Parabolic SAR acceleration step.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}
	
	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}
	
	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}
	
	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}
	
	/// <summary>
	/// Offset added to the fractal breakout level in pips.
	/// </summary>
	public decimal IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}
	
	/// <summary>
	/// RSI upper threshold used to cancel sell stops.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}
	
	/// <summary>
	/// RSI lower threshold used to cancel buy stops.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}
	
	/// <summary>
	/// Additional distance required between Parabolic SAR and price in pips before trailing.
	/// </summary>
	public decimal SarTrailingPips
	{
		get => _sarTrailingPips.Value;
		set => _sarTrailingPips.Value = value;
	}
	
	/// <summary>
	/// Candle data type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="RsiBollingerFractalBreakoutStrategy"/>.
	/// </summary>
	public RsiBollingerFractalBreakoutStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 8)
			.SetDisplay("RSI Period", "RSI averaging period", "RSI")
			.SetGreaterThanZero();
		
		_bandsPeriod = Param(nameof(BandsPeriod), 14)
			.SetDisplay("Bollinger Period", "RSI Bollinger period", "Bollinger")
			.SetGreaterThanZero();
		
		_bandsDeviation = Param(nameof(BandsDeviation), 1m)
			.SetDisplay("Bollinger Deviation", "Standard deviations on RSI", "Bollinger")
			.SetGreaterThanZero();
		
		_sarStep = Param(nameof(SarStep), 0.003m)
			.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Parabolic SAR")
			.SetGreaterThanZero();
		
		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Parabolic SAR")
			.SetGreaterThanZero();
		
		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");
		
		_stopLossPips = Param(nameof(StopLossPips), 135m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk");
		
		_indentPips = Param(nameof(IndentPips), 15m)
			.SetDisplay("Indent (pips)", "Offset from fractal breakout", "Entries");
		
		_rsiUpper = Param(nameof(RsiUpper), 70m)
			.SetDisplay("RSI Upper", "Overbought threshold", "RSI");
		
		_rsiLower = Param(nameof(RsiLower), 30m)
			.SetDisplay("RSI Lower", "Oversold threshold", "RSI");
		
		_sarTrailingPips = Param(nameof(SarTrailingPips), 10m)
			.SetDisplay("SAR Trailing (pips)", "Extra distance before SAR trailing", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_bollinger = new BollingerBands { Length = BandsPeriod, Width = BandsDeviation };
		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};
		
		_pipSize = GetPipSize();
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 1m;
		
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _parabolicSar, ProcessCandle)
			.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _parabolicSar);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		var bbValue = _bollinger.Process(new DecimalIndicatorValue(_bollinger, rsiValue, candle.OpenTime));
		if (!bbValue.IsFinal || bbValue is not BollingerBandsValue bandsValue)
		{
			UpdateTrailingAndExits(candle, sarValue);
			return;
		}
		
		if (bandsValue.UpBand is not decimal upperBand ||
			bandsValue.LowBand is not decimal lowerBand ||
			bandsValue.MovingAverage is not decimal middleBand)
		{
			UpdateTrailingAndExits(candle, sarValue);
			return;
		}
		
		UpdateFractals(candle);
		UpdateTrailingAndExits(candle, sarValue);
		
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		
		if (_fractalCount < 5)
			return;
		
		var upFractal = DetectUpperFractal();
		var downFractal = DetectLowerFractal();
		
		if (rsiValue < RsiLower)
			CancelBuyStop();
		
		if (rsiValue > RsiUpper)
			CancelSellStop();
		
		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;
		
		if (upFractal is decimal upper &&
			rsiValue > upperBand &&
			candle.ClosePrice < upper &&
			_buyStopOrder == null)
		{
			var entryPrice = NormalizePrice(upper + IndentPips * _pipSize);
			var stopPrice = StopLossPips > 0m ? NormalizePrice(entryPrice - StopLossPips * _pipSize) : (decimal?)null;
			var takePrice = TakeProfitPips > 0m ? NormalizePrice(entryPrice + TakeProfitPips * _pipSize) : (decimal?)null;
			
			CancelBuyStop();
			_buyStopOrder = BuyStop(volume, entryPrice);
			_pendingLongEntry = entryPrice;
			_pendingLongStop = stopPrice;
			_pendingLongTake = takePrice;
		}
		
		if (downFractal is decimal lower &&
			rsiValue < lowerBand &&
			candle.ClosePrice > lower &&
			_sellStopOrder == null)
		{
			var entryPrice = NormalizePrice(lower - IndentPips * _pipSize);
			var stopPrice = StopLossPips > 0m ? NormalizePrice(entryPrice + StopLossPips * _pipSize) : (decimal?)null;
			var takePrice = TakeProfitPips > 0m ? NormalizePrice(entryPrice - TakeProfitPips * _pipSize) : (decimal?)null;
			
			CancelSellStop();
			_sellStopOrder = SellStop(volume, entryPrice);
			_pendingShortEntry = entryPrice;
			_pendingShortStop = stopPrice;
			_pendingShortTake = takePrice;
		}
	}
	
	private void UpdateFractals(ICandleMessage candle)
	{
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;
		
		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;
		
		if (_fractalCount < 5)
			_fractalCount++;
	}
	
	private decimal? DetectUpperFractal()
	{
		if (_fractalCount < 5)
			return null;
		
		return _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5 ? _h3 : null;
	}
	
	private decimal? DetectLowerFractal()
	{
		if (_fractalCount < 5)
			return null;
		
		return _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5 ? _l3 : null;
	}
	
	private void UpdateTrailingAndExits(ICandleMessage candle, decimal sarValue)
	{
		if (Position > 0)
		{
			if (_longTakeProfit is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				return;
			}
			
			if (_longStopPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				return;
			}
			
			if (SarTrailingPips > 0m)
			{
				var trailingDistance = SarTrailingPips * _pipSize;
				if (sarValue < candle.ClosePrice - trailingDistance)
				{
					if (_longStopPrice is null || sarValue > _longStopPrice.Value)
					_longStopPrice = NormalizePrice(sarValue);
				}
			}
		}
		else if (Position < 0)
		{
			var absPosition = Math.Abs(Position);
			if (_shortTakeProfit is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(absPosition);
				return;
			}
			
			if (_shortStopPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(absPosition);
				return;
			}
			
			if (SarTrailingPips > 0m)
			{
				var trailingDistance = SarTrailingPips * _pipSize;
				if (sarValue > candle.ClosePrice + trailingDistance)
				{
					if (_shortStopPrice is null || sarValue < _shortStopPrice.Value)
					_shortStopPrice = NormalizePrice(sarValue);
				}
			}
		}
	}
	
	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);
		
		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakeProfit = null;
			_shortStopPrice = null;
			_shortTakeProfit = null;
			_pendingLongEntry = null;
			_pendingLongStop = null;
			_pendingLongTake = null;
			_pendingShortEntry = null;
			_pendingShortStop = null;
			_pendingShortTake = null;
			return;
		}
		
		if (delta > 0 && Position > 0)
		{
			if (_pendingLongEntry is decimal)
			{
				_longStopPrice = _pendingLongStop;
				_longTakeProfit = _pendingLongTake;
			}
			
			CancelSellStop();
			_buyStopOrder = null;
			_pendingLongEntry = null;
			_pendingLongStop = null;
			_pendingLongTake = null;
		}
		else if (delta < 0 && Position < 0)
		{
			if (_pendingShortEntry is decimal)
			{
				_shortStopPrice = _pendingShortStop;
				_shortTakeProfit = _pendingShortTake;
			}
			
			CancelBuyStop();
			_sellStopOrder = null;
			_pendingShortEntry = null;
			_pendingShortStop = null;
			_pendingShortTake = null;
		}
	}
	
	private void CancelBuyStop()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);
		
		_buyStopOrder = null;
		_pendingLongEntry = null;
		_pendingLongStop = null;
		_pendingLongTake = null;
	}
	
	private void CancelSellStop()
	{
		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);
		
		_sellStopOrder = null;
		_pendingShortEntry = null;
		_pendingShortStop = null;
		_pendingShortTake = null;
	}
	
	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;
		
		var temp = step;
		var decimals = 0;
		while (temp != Math.Truncate(temp) && decimals < 10)
		{
			temp *= 10m;
			decimals++;
		}
		
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}
	
	private decimal NormalizePrice(decimal price)
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return price;
		
		var steps = decimal.Round(price / step, 0, MidpointRounding.AwayFromZero);
		return steps * step;
	}
}
