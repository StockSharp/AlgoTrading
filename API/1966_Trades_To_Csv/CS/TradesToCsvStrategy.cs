namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades To CSV strategy exports closed trades information to a CSV file.
/// It generates signals using CCI and MACD histogram values.
/// </summary>
public class TradesToCsvStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<string> _fileName;

	private CommodityChannelIndex _cci = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _entryPrice;
	private DateTimeOffset _entryTime;
	private long _ticketCounter;
	private long _currentTicket;
	private decimal _closePrice;
	private DateTimeOffset _closeTime;
	private string _closeOrderType = string.Empty;

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Take profit in absolute currency units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute currency units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// MACD fast period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Output file name for CSV export.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref=\"TradesToCsvStrategy\"/>.
	/// </summary>
	public TradesToCsvStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay(\"Candle Type\", \"Type of candles to use\", \"General\");

		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay(\"Take Profit\", \"Profit threshold for closing\", \"Risk\");

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetDisplay(\"Stop Loss\", \"Loss threshold for closing\", \"Risk\");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay(\"Volume\", \"Order volume\", \"Trading\");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay(\"CCI Period\", \"CCI calculation period\", \"Indicators\");

		_macdFast = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay(\"MACD Fast\", \"Fast EMA period\", \"Indicators\");

		_macdSlow = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay(\"MACD Slow\", \"Slow EMA period\", \"Indicators\");

		_macdSignal = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay(\"MACD Signal\", \"Signal EMA period\", \"Indicators\");

		_fileName = Param(nameof(FileName), \"myfilename.csv\")
			.SetDisplay(\"File Name\", \"CSV file name\", \"General\");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_entryTime = default;
		_ticketCounter = 0L;
		_currentTicket = 0L;
		_closePrice = 0m;
		_closeTime = default;
		_closeOrderType = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (!File.Exists(FileName))
		{
			File.WriteAllText(FileName, \"Order,Profit/Loss,Ticket Number,Open Price,Close Price,Open Time,Close Time,Symbol,Lots\\n\");
		}

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_cci, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);

			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, _macd);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var cci = cciValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var histogram = macdLine - signalLine;
		var currentPrice = candle.ClosePrice;

		var longSignal = cci > -125m && cci < -42m && histogram > -0.00114m && histogram < 0.00038m;
		var shortSignal = cci > 125m && cci < 208m && histogram > -0.00038m && histogram < 0.00190m;

		if (Position == 0)
		{
			if (longSignal)
			{
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
				_entryPrice = currentPrice;
				_entryTime = candle.CloseTime;
				_ticketCounter++;
				_currentTicket = _ticketCounter;
			}
			else if (shortSignal)
			{
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
				_entryPrice = currentPrice;
				_entryTime = candle.CloseTime;
				_ticketCounter++;
				_currentTicket = _ticketCounter;
			}

			return;
		}

		var profit = Position > 0 ? (currentPrice - _entryPrice) * Position : (_entryPrice - currentPrice) * Math.Abs(Position);

		if (Position > 0 && (shortSignal || profit > TakeProfit || profit < -StopLoss))
		{
			_closePrice = currentPrice;
			_closeTime = candle.CloseTime;
			_closeOrderType = \"Buy Order Closed\";
			RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
		}
		else if (Position < 0 && (longSignal || profit > TakeProfit || profit < -StopLoss))
		{
			_closePrice = currentPrice;
			_closeTime = candle.CloseTime;
			_closeOrderType = \"Sell Order Closed\";
			RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0 || _closeOrderType == string.Empty)
			return;

		var profit = _closeOrderType == \"Buy Order Closed\"
			? (_closePrice - _entryPrice) * Volume
			: (_entryPrice - _closePrice) * Volume;

		AppendCsv(_closeOrderType, profit, _currentTicket, _entryPrice, _closePrice, _entryTime, _closeTime, Volume);

		_entryPrice = 0m;
		_currentTicket = 0L;
		_closePrice = 0m;
		_closeTime = default;
		_closeOrderType = string.Empty;
	}

	private void AppendCsv(string order, decimal profit, long ticket, decimal openPrice, decimal closePrice,
	DateTimeOffset openTime, DateTimeOffset closeTime, decimal lots)
	{
		var line = string.Join(",",
		order,
		profit.ToString(CultureInfo.InvariantCulture),
		ticket.ToString(CultureInfo.InvariantCulture),
		openPrice.ToString(CultureInfo.InvariantCulture),
		closePrice.ToString(CultureInfo.InvariantCulture),
		openTime.ToString(\"o\", CultureInfo.InvariantCulture),
		closeTime.ToString(\"o\", CultureInfo.InvariantCulture),
		Security.Id,
		lots.ToString(CultureInfo.InvariantCulture));

		File.AppendAllText(FileName, line + Environment.NewLine);
	}
}
