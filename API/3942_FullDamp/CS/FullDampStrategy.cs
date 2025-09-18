using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Full Damp strategy converted from the MQL version.
/// </summary>
public class FullDampStrategy : Strategy
{
	private const decimal OversoldLevel = 30m;
	private const decimal OverboughtLevel = 70m;
	
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod1;
	private readonly StrategyParam<int> _bollingerPeriod2;
	private readonly StrategyParam<int> _bollingerPeriod3;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<int> _stopOffsetPoints;
	
	private decimal? _wideUpperBand;
	private decimal? _wideLowerBand;
	private decimal? _mediumUpperBand;
	private decimal? _mediumLowerBand;
	private decimal? _narrowUpperBand;
	private decimal? _narrowLowerBand;
	
	private int _longRsiCounter;
	private int _shortRsiCounter;
	
	private bool _longPending;
	private bool _shortPending;
	private bool _longRsiReady;
	private bool _shortRsiReady;
	
	private decimal? _longLowestLow;
	private decimal? _shortHighestHigh;
	
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortTargetPrice;
	
	private bool _longHalfClosed;
	private bool _shortHalfClosed;
	
	/// <summary>
	/// Candle type to use for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Period for the first Bollinger Bands indicator (width = 1).
	/// </summary>
	public int BollingerPeriod1
	{
		get => _bollingerPeriod1.Value;
		set => _bollingerPeriod1.Value = value;
	}
	
	/// <summary>
	/// Period for the second Bollinger Bands indicator (width = 2).
	/// </summary>
	public int BollingerPeriod2
	{
		get => _bollingerPeriod2.Value;
		set => _bollingerPeriod2.Value = value;
	}
	
	/// <summary>
	/// Period for the third Bollinger Bands indicator (width = 3).
	/// </summary>
	public int BollingerPeriod3
	{
		get => _bollingerPeriod3.Value;
		set => _bollingerPeriod3.Value = value;
	}
	
	/// <summary>
	/// RSI period for overbought and oversold detection.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}
	
	/// <summary>
	/// Amount of completed candles to validate the RSI condition.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = value;
	}
	
	/// <summary>
	/// Offset for stop calculation expressed in instrument points.
	/// </summary>
	public int StopOffsetPoints
	{
		get => _stopOffsetPoints.Value;
		set => _stopOffsetPoints.Value = value;
	}
	
	/// <summary>
	/// Initializes a new instance of <see cref="FullDampStrategy"/>.
	/// </summary>
	public FullDampStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for analysis", "General");
		
		_bollingerPeriod1 = Param(nameof(BollingerPeriod1), 20)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Bollinger Period (Width 1)", "Period for the narrow bands", "Indicators");
		
		_bollingerPeriod2 = Param(nameof(BollingerPeriod2), 20)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Bollinger Period (Width 2)", "Period for the medium bands", "Indicators");
		
		_bollingerPeriod3 = Param(nameof(BollingerPeriod3), 20)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Bollinger Period (Width 3)", "Period for the wide bands", "Indicators");
		
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("RSI Period", "Period for the RSI filter", "Indicators");
		
		_lookbackBars = Param(nameof(LookbackBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("RSI Lookback", "Candles to confirm RSI", "Risk");
		
		_stopOffsetPoints = Param(nameof(StopOffsetPoints), 10)
			.SetGreaterThanZero()
			.SetDisplay("Stop Offset", "Offset in points for stop placement", "Risk");
		
		Volume = 1;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var bollingerWide = new BollingerBands
		{
			Length = BollingerPeriod3,
			Width = 3m
		};
		
		var bollingerMedium = new BollingerBands
		{
			Length = BollingerPeriod2,
			Width = 2m
		};
		
		var bollingerNarrow = new BollingerBands
		{
			Length = BollingerPeriod1,
			Width = 1m
		};
		
		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};
		
		var subscription = SubscribeCandles(CandleType);
		
		subscription
			.BindEx(bollingerWide, OnBollingerWide)
			.BindEx(bollingerMedium, OnBollingerMedium)
			.BindEx(bollingerNarrow, OnBollingerNarrow)
			.Bind(rsi, OnProcess)
			.Start();
	}
	
	private void OnBollingerWide(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;
		
		var bands = (BollingerBandsValue)value;
		
		if (bands.UpBand is decimal upper && bands.LowBand is decimal lower)
		{
			_wideUpperBand = upper;
			_wideLowerBand = lower;
		}
	}
	
	private void OnBollingerMedium(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;
		
		var bands = (BollingerBandsValue)value;
		
		if (bands.UpBand is decimal upper && bands.LowBand is decimal lower)
		{
			_mediumUpperBand = upper;
			_mediumLowerBand = lower;
		}
	}
	
	private void OnBollingerNarrow(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;
		
		var bands = (BollingerBandsValue)value;
		
		if (bands.UpBand is decimal upper && bands.LowBand is decimal lower)
		{
			_narrowUpperBand = upper;
			_narrowLowerBand = lower;
		}
	}
	
	private void OnProcess(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		UpdateRsiCounters(rsiValue);
		
		ManageLongPosition(candle);
		ManageShortPosition(candle);
		
		if (_wideLowerBand.HasValue && Position <= 0 && candle.Low <= _wideLowerBand.Value)
		{
			_longPending = true;
			_longLowestLow = candle.Low;
			_longRsiReady = _longRsiCounter > 0;
		}
		else if (_longPending)
		{
			_longLowestLow = _longLowestLow.HasValue
				? Math.Min(_longLowestLow.Value, candle.Low)
				: candle.Low;
		}
		
		if (_wideUpperBand.HasValue && Position >= 0 && candle.High >= _wideUpperBand.Value)
		{
			_shortPending = true;
			_shortHighestHigh = candle.High;
			_shortRsiReady = _shortRsiCounter > 0;
		}
		else if (_shortPending)
		{
			_shortHighestHigh = _shortHighestHigh.HasValue
				? Math.Max(_shortHighestHigh.Value, candle.High)
				: candle.High;
		}
		
		if (_longPending && _longRsiReady && _mediumLowerBand.HasValue && Position <= 0 && candle.Close > _mediumLowerBand.Value)
		{
			EnterLong(candle);
		}
		
		if (_shortPending && _shortRsiReady && _mediumUpperBand.HasValue && Position >= 0 && candle.Close < _mediumUpperBand.Value)
		{
			EnterShort(candle);
		}
		
		if (Position == 0)
		{
			ResetLongPositionState();
			ResetShortPositionState();
		}
	}
	
	private void UpdateRsiCounters(decimal rsiValue)
	{
		if (rsiValue < OversoldLevel)
		{
			_longRsiCounter = LookbackBars;
		}
		else if (_longRsiCounter > 0)
		{
			_longRsiCounter--;
		}
		
		if (rsiValue > OverboughtLevel)
		{
			_shortRsiCounter = LookbackBars;
		}
		else if (_shortRsiCounter > 0)
		{
			_shortRsiCounter--;
		}
	}
	
	private void ManageLongPosition(ICandleMessage candle)
	{
		if (Position <= 0)
			return;
		
		if (_longStopPrice.HasValue && candle.Low <= _longStopPrice.Value)
		{
			SellMarket(Position);
			ResetLongPositionState();
			return;
		}
		
		if (_mediumUpperBand.HasValue && candle.High >= _mediumUpperBand.Value)
		{
			SellMarket(Position);
			ResetLongPositionState();
			return;
		}
		
		if (_longTargetPrice.HasValue && !_longHalfClosed && candle.High >= _longTargetPrice.Value)
		{
			var halfVolume = Position / 2m;
			
			if (halfVolume > 0)
			{
				_longHalfClosed = true;
				SellMarket(halfVolume);
				_longStopPrice = _longEntryPrice;
				_longTargetPrice = null;
			}
		}
	}
	
	private void ManageShortPosition(ICandleMessage candle)
	{
		if (Position >= 0)
			return;
		
		var positionAbs = Math.Abs(Position);
		
		if (_shortStopPrice.HasValue && candle.High >= _shortStopPrice.Value)
		{
			BuyMarket(positionAbs);
			ResetShortPositionState();
			return;
		}
		
		if (_mediumLowerBand.HasValue && candle.Low <= _mediumLowerBand.Value)
		{
			BuyMarket(positionAbs);
			ResetShortPositionState();
			return;
		}
		
		if (_shortTargetPrice.HasValue && !_shortHalfClosed && candle.Low <= _shortTargetPrice.Value)
		{
			var halfVolume = positionAbs / 2m;
			
			if (halfVolume > 0)
			{
				_shortHalfClosed = true;
				BuyMarket(halfVolume);
				_shortStopPrice = _shortEntryPrice;
				_shortTargetPrice = null;
			}
		}
	}
	
	private void EnterLong(ICandleMessage candle)
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var stopOffset = StopOffsetPoints * priceStep;
		
		var stopPrice = (_longLowestLow ?? candle.Low) - stopOffset;
		stopPrice = Security?.ShrinkPrice(stopPrice) ?? stopPrice;
		
		BuyMarket(Volume);
		
		_longPending = false;
		_longRsiReady = false;
		_longLowestLow = null;
		_longStopPrice = stopPrice;
		_longEntryPrice = candle.Close;
		
		if (_longStopPrice.HasValue)
		{
			_longTargetPrice = _longEntryPrice + (_longEntryPrice - _longStopPrice.Value);
		}
		
		_longHalfClosed = false;
	}
	
	private void EnterShort(ICandleMessage candle)
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var stopOffset = StopOffsetPoints * priceStep;
		
		var stopPrice = (_shortHighestHigh ?? candle.High) + stopOffset;
		stopPrice = Security?.ShrinkPrice(stopPrice) ?? stopPrice;
		
		SellMarket(Volume);
		
		_shortPending = false;
		_shortRsiReady = false;
		_shortHighestHigh = null;
		_shortStopPrice = stopPrice;
		_shortEntryPrice = candle.Close;
		
		if (_shortStopPrice.HasValue)
		{
			_shortTargetPrice = _shortEntryPrice - (_shortStopPrice.Value - _shortEntryPrice);
		}
		
		_shortHalfClosed = false;
	}
	
	private void ResetLongPositionState()
	{
		_longEntryPrice = null;
		_longStopPrice = null;
		_longTargetPrice = null;
		_longHalfClosed = false;
	}
	
	private void ResetShortPositionState()
	{
		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
		_shortHalfClosed = false;
	}
	
	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);
		
		if (trade.Order.Side == Sides.Buy && Position > 0)
		{
			_longEntryPrice = trade.Trade.Price;
			
			if (_longStopPrice.HasValue)
			{
				_longTargetPrice = _longEntryPrice + (_longEntryPrice - _longStopPrice.Value);
			}
		}
		else if (trade.Order.Side == Sides.Sell && Position < 0)
		{
			_shortEntryPrice = trade.Trade.Price;
			
			if (_shortStopPrice.HasValue)
			{
				_shortTargetPrice = _shortEntryPrice - (_shortStopPrice.Value - _shortEntryPrice);
			}
		}
	}
}
