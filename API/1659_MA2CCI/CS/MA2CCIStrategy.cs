using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA2CCI strategy combines two simple moving averages with CCI and ATR-based stop.
/// </summary>
public class MA2CCIStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private CommodityChannelIndex _cci;
	private AverageTrueRange _atr;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevCci;
	private decimal? _stopPrice;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MA2CCIStrategy"/>.
	/// </summary>
	public MA2CCIStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_cciPeriod = Param(nameof(CciPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Average True Range period", "Parameters");
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

		_prevFast = null;
		_prevSlow = null;
		_prevCci = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _cci, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cci, decimal atr)
	{
		// Use only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure indicators are formed and trading is allowed
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_cci.IsFormed || !_atr.IsFormed)
			return;

		// Check stop loss
		if (_stopPrice is decimal stop)
		{
			if (Position > 0 && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
			}
			else if (Position < 0 && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
			}
		}

		if (_prevFast is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevCci = cci;
			return;
		}

		// Close positions on reverse signal
		if (Position > 0 && fast < slow && _prevFast >= _prevSlow)
		{
			SellMarket(Position);
			_stopPrice = null;
		}
		else if (Position < 0 && fast > slow && _prevFast <= _prevSlow)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = null;
		}

		// Check for entry signals
		if (fast > slow && _prevFast <= _prevSlow && cci > 0m && _prevCci <= 0m && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atr;
		}
		else if (fast < slow && _prevFast >= _prevSlow && cci < 0m && _prevCci >= 0m && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atr;
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevCci = cci;
	}
}
