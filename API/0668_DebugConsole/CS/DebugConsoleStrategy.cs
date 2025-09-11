namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

/// <summary>
/// Demonstrates a console that queues messages for debugging.
/// </summary>
public class DebugConsoleStrategy : Strategy
{
	private readonly StrategyParam<int> _size;
	private readonly StrategyParam<bool> _isVisible;
	private readonly StrategyParam<bool> _intrabar;
	private readonly StrategyParam<DataType> _candleType;

	private DebugConsole _console;

	/// <summary>
	/// Number of messages to keep.
	/// </summary>
	public int Size
	{
		get => _size.Value;
		set => _size.Value = value;
	}

	/// <summary>
	/// Enable console output.
	/// </summary>
	public bool IsVisible
	{
		get => _isVisible.Value;
		set => _isVisible.Value = value;
	}

	/// <summary>
	/// Allow intrabar messages.
	/// </summary>
	public bool Intrabar
	{
		get => _intrabar.Value;
		set => _intrabar.Value = value;
	}

	/// <summary>
	/// Candle type used for logging.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DebugConsoleStrategy"/>.
	/// </summary>
	public DebugConsoleStrategy()
	{
		_size = Param(nameof(Size), 20)
			.SetGreaterThanZero()
			.SetDisplay("Console Size", "Number of messages to keep", "Console");

		_isVisible = Param(nameof(IsVisible), true)
			.SetDisplay("Visible", "Enable console output", "Console");

		_intrabar = Param(nameof(Intrabar), false)
			.SetDisplay("Intrabar", "Allow intrabar messages", "Console");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to log", "General");
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
		_console = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_console = new DebugConsole(Size, Intrabar);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_console.Queue($"{candle.OpenTime:O} Close: {candle.ClosePrice}");
		_console.QueueOne("only one");
		_console.QueueOneIntrabar($"intrabar: {candle.ClosePrice}", candle.OpenTime);

		if (IsVisible)
			_console.Update(m => LogInfo(m));
	}

	private class DebugConsole
	{
		private readonly int _size;
		private readonly bool _intrabar;
		private readonly List<string> _entries = new();
		private DateTimeOffset _currentBar;
		private string _intrabarMessage;

		public DebugConsole(int size, bool intrabar)
		{
			_size = size;
			_intrabar = intrabar;
		}

		public void Queue(string message)
		{
			Add(message);
		}

		public void QueueOne(string message)
		{
			if (_entries.Contains(message))
				return;

			Add(message);
		}

		public void QueueOneIntrabar(string message, DateTimeOffset barTime)
		{
			if (!_intrabar)
				return;

			if (_currentBar != barTime)
			{
				_intrabarMessage = null;
				_currentBar = barTime;
			}

			if (_intrabarMessage != null)
				return;

			_intrabarMessage = message;
			Add(message);
		}

		public void Update(Action<string> output)
		{
			output(string.Join(Environment.NewLine, _entries));
		}

		private void Add(string message)
		{
			_entries.Add(message);

			if (_entries.Count > _size)
				_entries.RemoveAt(0);
		}
	}
}

