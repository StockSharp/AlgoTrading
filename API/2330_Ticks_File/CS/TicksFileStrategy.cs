using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that records tick data and related candle information to a CSV file.
/// </summary>
public class TicksFileStrategy : Strategy
{
	private readonly StrategyParam<bool> _discrete;
	private readonly StrategyParam<string> _filler;
	private readonly StrategyParam<bool> _fileEnabled;
	private readonly StrategyParam<DataType> _candleType;

	private StreamWriter? _writer;
	private int _count;
	private ICandleMessage? _prevCandle;
	private ICandleMessage? _currentCandle;
	private DateTimeOffset? _lastLoggedTime;
	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Process only the first tick of each bar.
	/// </summary>
	public bool Discrete
	{
		get => _discrete.Value;
		set => _discrete.Value = value;
	}

	/// <summary>
	/// Separator used in the CSV file.
	/// </summary>
	public string Filler
	{
		get => _filler.Value;
		set => _filler.Value = value;
	}

	/// <summary>
	/// Enable writing to file.
	/// </summary>
	public bool FileEnabled
	{
		get => _fileEnabled.Value;
		set => _fileEnabled.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe for bar data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TicksFileStrategy"/>.
	/// </summary>
	public TicksFileStrategy()
	{
		_discrete = Param(nameof(Discrete), false)
			.SetDisplay("Discrete", "Work only at bar opening", "General");

		_filler = Param(nameof(Filler), ";")
			.SetDisplay("Filler", "Field separator in file", "General");

		_fileEnabled = Param(nameof(FileEnabled), true)
			.SetDisplay("File", "Write data to file", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, DataType.Ticks),
			(Security, DataType.Level1)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_writer = null;
		_count = 0;
		_prevCandle = null;
		_currentCandle = null;
		_lastLoggedTime = null;
		_lastBid = null;
		_lastAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (FileEnabled)
		{
			var tf = CandleType.TimeFrame ?? TimeSpan.FromMinutes(1);
			var name = $"T_{Security.Id}_M{(int)tf.TotalMinutes}_{time.Year}_{time.Month}_{time.Day}_{time.Hour}x{time.Minute}.csv";
			_writer = new StreamWriter(name);

			var sep = Filler == " " ? "\t" : Filler;
			_writer.WriteLine(string.Join(sep, new[]
			{
				"day","mon","year","hour","min","S",
				"close","high","low","open","spread","tick_volume",
				"T","ask","bid","last","volume",
				"N","H","M","close","high","low","open","spread","tick_volume"
			}));
		}

		SubscribeLevel1().Bind(ProcessLevel1).Start();
		var candleSub = SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		SubscribeTrades().Bind(ProcessTrade).Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, candleSub);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Finished)
			_prevCandle = candle;
		else
			_currentCandle = candle;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		var volume = trade.Volume ?? 0m;

		if (_prevCandle == null || _currentCandle == null)
			return;

		if (Discrete && _lastLoggedTime == _prevCandle.OpenTime)
			return;

		if (_writer != null)
		{
			var sep = Filler == " " ? "\t" : Filler;
			var prev = _prevCandle;
			var curr = _currentCandle;

			var line = string.Join(sep, new[]
			{
				prev.OpenTime.Day.ToString(CultureInfo.InvariantCulture),
				prev.OpenTime.Month.ToString(CultureInfo.InvariantCulture),
				prev.OpenTime.Year.ToString(CultureInfo.InvariantCulture),
				prev.OpenTime.Hour.ToString(CultureInfo.InvariantCulture),
				prev.OpenTime.Minute.ToString(CultureInfo.InvariantCulture),
				"I",
				prev.ClosePrice.ToString(CultureInfo.InvariantCulture),
				prev.HighPrice.ToString(CultureInfo.InvariantCulture),
				prev.LowPrice.ToString(CultureInfo.InvariantCulture),
				prev.OpenPrice.ToString(CultureInfo.InvariantCulture),
				(_lastAsk - _lastBid)?.ToString(CultureInfo.InvariantCulture) ?? "0",
				prev.TotalVolume.ToString(CultureInfo.InvariantCulture),
				"T",
				_lastAsk?.ToString(CultureInfo.InvariantCulture) ?? "0",
				_lastBid?.ToString(CultureInfo.InvariantCulture) ?? "0",
				price.ToString(CultureInfo.InvariantCulture),
				volume.ToString(CultureInfo.InvariantCulture),
				"N=",
				curr.OpenTime.Hour.ToString(CultureInfo.InvariantCulture),
				curr.OpenTime.Minute.ToString(CultureInfo.InvariantCulture),
				curr.ClosePrice.ToString(CultureInfo.InvariantCulture),
				curr.HighPrice.ToString(CultureInfo.InvariantCulture),
				curr.LowPrice.ToString(CultureInfo.InvariantCulture),
				curr.OpenPrice.ToString(CultureInfo.InvariantCulture),
				(_lastAsk - _lastBid)?.ToString(CultureInfo.InvariantCulture) ?? "0",
				curr.TotalVolume.ToString(CultureInfo.InvariantCulture)
			});

			_writer.WriteLine(line);
			_count++;
		}

		_lastLoggedTime = _prevCandle.OpenTime;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (_writer != null)
		{
			_writer.Flush();
			_writer.Dispose();
			_writer = null;
			LogInfo($"{_count} records in file.");
		}

		base.OnStopped();
	}
}
