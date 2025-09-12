namespace StockSharp.Samples.Strategies;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI Long-Term strategy using 15-minute candles.
/// Enters long when RSI is oversold, fast SMA is above slow SMA, and volume exceeds its average.
/// Exits on SMA crossunder or stop loss.
/// </summary>
public class RsiLongTermStrategy15minStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<int> _sma1Length;
	private readonly StrategyParam<int> _sma2Length;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _sma1;
	private SimpleMovingAverage _sma2;
	private SimpleMovingAverage _volumeSma;

	private decimal _prevSma1;
	private decimal _prevSma2;
	private decimal _entryPrice;
	private bool _isInitialized;

	public RsiLongTermStrategy15minStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 10)
		.SetDisplay("RSI Length", "RSI period", "RSI");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
		.SetDisplay("Volume SMA Length", "Volume SMA period", "Volume");

		_sma1Length = Param(nameof(Sma1Length), 250)
		.SetDisplay("SMA1 Length", "Fast SMA period", "Trend");

		_sma2Length = Param(nameof(Sma2Length), 500)
		.SetDisplay("SMA2 Length", "Slow SMA period", "Trend");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.5m)
		.SetDisplay("Volume Multiplier", "Volume threshold multiplier", "Volume");

		_stopLossPercent = Param(nameof(StopLossPercent), 5m)
		.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int VolumeSmaLength { get => _volumeSmaLength.Value; set => _volumeSmaLength.Value = value; }
	public int Sma1Length { get => _sma1Length.Value; set => _sma1Length.Value = value; }
	public int Sma2Length { get => _sma2Length.Value; set => _sma2Length.Value = value; }
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevSma1 = 0;
		_prevSma2 = 0;
		_entryPrice = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_sma1 = new SimpleMovingAverage { Length = Sma1Length };
		_sma2 = new SimpleMovingAverage { Length = Sma2Length };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, _sma1, _sma2, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma1);
			DrawIndicator(area, _sma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal sma1Value, decimal sma2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var volSmaValue = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_rsi.IsFormed || !_sma1.IsFormed || !_sma2.IsFormed || !_volumeSma.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			_prevSma1 = sma1Value;
			_prevSma2 = sma2Value;
			_isInitialized = true;
			return;
		}

		var volumeCond = candle.TotalVolume > volSmaValue * VolumeMultiplier;
		var rsiOversold = rsiValue <= 30m;
		var longCond = rsiOversold && sma1Value > sma2Value && volumeCond;

		var crossUnder = _prevSma1 >= _prevSma2 && sma1Value < sma2Value;

		if (longCond && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (Position > 0)
		{
			var stopPrice = _entryPrice * (1m - StopLossPercent / 100m);
			if (candle.ClosePrice <= stopPrice || crossUnder)
			{
				SellMarket(Position);
				_entryPrice = 0;
			}
		}

		_prevSma1 = sma1Value;
		_prevSma2 = sma2Value;
	}
}
