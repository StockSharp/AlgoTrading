using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaCrossoverTpSl5EmaFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _targetPercent;
	private readonly StrategyParam<decimal> _stopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fastMa;
	private EMA _slowMa;
	private EMA _ema;
	private decimal _entryPrice;
	private bool _isLong;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal TargetPercent { get => _targetPercent.Value; set => _targetPercent.Value = value; }
	public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverTpSl5EmaFilterStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10).SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast MA period", "Indicators");
		_slowLength = Param(nameof(SlowLength), 30).SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow MA period", "Indicators");
		_emaLength = Param(nameof(EmaLength), 5).SetGreaterThanZero()
			.SetDisplay("EMA Filter", "EMA filter length", "Indicators");
		_targetPercent = Param(nameof(TargetPercent), 2m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_stopPercent = Param(nameof(StopPercent), 1m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_initialized = false;

		_fastMa = new EMA { Length = FastLength };
		_slowMa = new EMA { Length = SlowLength };
		_ema = new EMA { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_ema.IsFormed)
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		if (crossUp && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLong = true;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLong = false;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && _isLong && _entryPrice > 0)
		{
			var tp = _entryPrice * (1m + TargetPercent / 100m);
			var sl = _entryPrice * (1m - StopPercent / 100m);
			if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && !_isLong && _entryPrice > 0)
		{
			var tp = _entryPrice * (1m - TargetPercent / 100m);
			var sl = _entryPrice * (1m + StopPercent / 100m);
			if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
				BuyMarket(Math.Abs(Position));
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
