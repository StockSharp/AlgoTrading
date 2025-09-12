namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MultiLayer Awesome Oscillator Saucer strategy.
/// Builds on the Awesome Oscillator saucer pattern and fractal trend detection.
/// Places up to five layered buy stop orders when bullish saucer signals appear.
/// </summary>
public class MultiLayerAwesomeOscillatorSaucerStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;
	
	private ExponentialMovingAverage _filterEma;
	private SmoothedMovingAverage _teethSmma;
	private AwesomeOscillator _ao;
	
	private readonly decimal?[] _teethBuffer = new decimal?[6];
	private int _teethCount;
	
	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	
	private decimal? _upFractalLevel;
	private decimal? _downFractalLevel;
	private decimal? _upFractalActivation;
	private decimal? _downFractalActivation;
	
	private int _trend;
	private int _prevTrend;
	private decimal? _saucerActivation;
	private int _signalsInRow;
	
	private decimal _aoPrev1;
	private decimal _aoPrev2;
	
	/// <summary>
	/// EMA filter length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}
	
	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Start of trading period.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}
	
	/// <summary>
	/// End of trading period.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}
	
	/// <summary>
	/// Initialize <see cref="MultiLayerAwesomeOscillatorSaucerStrategy"/>.
	/// </summary>
	public MultiLayerAwesomeOscillatorSaucerStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Length of EMA filter", "General");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		
		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(new DateTime(2023, 1, 1), TimeSpan.Zero))
		.SetDisplay("Trade Start", "Start time", "Time");
		
		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(new DateTime(2025, 1, 1), TimeSpan.Zero))
		.SetDisplay("Trade Stop", "End time", "Time");
		
		Volume = 1;
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
		
		Array.Clear(_teethBuffer);
		_teethCount = 0;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_upFractalLevel = null;
		_downFractalLevel = null;
		_upFractalActivation = null;
		_downFractalActivation = null;
		_trend = 0;
		_prevTrend = 0;
		_saucerActivation = null;
		_signalsInRow = 0;
		_aoPrev1 = 0m;
		_aoPrev2 = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();
		
		_filterEma = new ExponentialMovingAverage { Length = EmaLength };
		_teethSmma = new SmoothedMovingAverage { Length = 8 };
		_ao = new AwesomeOscillator();
		
		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle);
		sub.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _filterEma);
			DrawIndicator(area, _teethSmma);
			DrawIndicator(area, _ao);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		
		var teethValue = _teethSmma.Process(median);
		var emaValue = _filterEma.Process(candle.ClosePrice);
		var aoValue = _ao.Process(candle.HighPrice, candle.LowPrice);
		
		if (!teethValue.IsFinal || !emaValue.IsFinal || !aoValue.IsFinal)
		{
			UpdateBuffers(candle);
			return;
		}
		
		var teeth = teethValue.GetValue<decimal>();
		var filterEma = emaValue.GetValue<decimal>();
		var ao = aoValue.GetValue<decimal>();
		
		_teethBuffer[_teethCount % _teethBuffer.Length] = teeth;
		_teethCount++;
		decimal? shiftedTeeth = _teethCount > 5 ? _teethBuffer[(_teethCount - 6) % _teethBuffer.Length] : null;
		if (shiftedTeeth is not decimal t)
		{
			UpdateBuffers(candle);
			return;
		}
		
		UpdateBuffers(candle);
		
		decimal? upFractalPrice = null;
		decimal? downFractalPrice = null;
		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
		upFractalPrice = _h3;
		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
		downFractalPrice = _l3;
		
		_upFractalLevel = upFractalPrice ?? _upFractalLevel;
		_downFractalLevel = downFractalPrice ?? _downFractalLevel;
		
		if (_upFractalLevel is decimal uf && uf > t)
		_upFractalActivation = uf;
		if (_downFractalLevel is decimal df && df < t)
		_downFractalActivation = df;
		
		if (_upFractalActivation is decimal upAct && candle.HighPrice > upAct)
		{
			_trend = 1;
			_upFractalActivation = null;
			_downFractalActivation = _downFractalLevel;
		}
		if (_downFractalActivation is decimal downAct && candle.LowPrice < downAct)
		{
			_trend = -1;
			_downFractalActivation = null;
			_upFractalActivation = _upFractalLevel;
		}
		
		if (_trend == 1)
		_upFractalActivation = null;
		else if (_trend == -1)
		_downFractalActivation = null;
		
		var diff = ao - _aoPrev1;
		var saucerSignal = ao > _aoPrev1 && _aoPrev1 < _aoPrev2 && ao > 0 && _aoPrev1 > 0 && _aoPrev2 > 0 &&
		_trend == 1 && candle.ClosePrice > filterEma;
		
		if (saucerSignal)
		_saucerActivation = candle.HighPrice;
		
		if (_saucerActivation is decimal prevAct && candle.HighPrice < prevAct && diff > 0)
		{
			_saucerActivation = candle.HighPrice;
			saucerSignal = true;
		}
		
		if ((_saucerActivation is decimal sAct && candle.HighPrice > sAct) || diff < 0)
		_saucerActivation = null;
		
		if (saucerSignal && _saucerActivation != null)
		{
			_signalsInRow++;
			if (_signalsInRow <= 5 && candle.ServerTime >= TradeStart && candle.ServerTime <= TradeStop)
			{
				var price = _saucerActivation.Value + (Security?.PriceStep ?? 0.01m);
				BuyStop(Volume, price);
			}
		}
		
		if (diff < 0)
		CancelActiveOrders();
		
		if (_trend == -1 && _prevTrend == 1)
		{
			CancelActiveOrders();
			if (Position > 0)
			SellMarket(Position);
			_signalsInRow = 0;
		}
		
		_prevTrend = _trend;
		_aoPrev2 = _aoPrev1;
		_aoPrev1 = ao;
	}
	
	private void UpdateBuffers(ICandleMessage candle)
	{
		_h5 = _h4; _h4 = _h3; _h3 = _h2; _h2 = _h1; _h1 = candle.HighPrice;
		_l5 = _l4; _l4 = _l3; _l3 = _l2; _l2 = _l1; _l1 = candle.LowPrice;
	}
}
