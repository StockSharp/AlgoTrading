using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Logging;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trade copier.
/// Works in master or slave mode and synchronizes orders via CSV file.
/// </summary>
public class SimpleCopierStrategy : Strategy
{
	private readonly StrategyParam<CopierMode> _mode;
	private readonly StrategyParam<int> _slippage;
	private readonly StrategyParam<decimal> _multiplier;

	private Timer _timer;
	private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "C4F.csv");

	/// <summary>
	/// Working mode of the copier.
	/// </summary>
	public CopierMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Allowed price slippage in pips.
	/// </summary>
	public int Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	/// <summary>
	/// Volume multiplier for slave mode.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleCopierStrategy"/>.
	/// </summary>
	public SimpleCopierStrategy()
	{
		_mode = Param(nameof(Mode), CopierMode.Master)
			.SetDisplay("Mode", "Working mode of the copier", "General");

		_slippage = Param(nameof(Slippage), 10)
			.SetGreaterThanZero()
			.SetDisplay("Slippage", "Allowed slippage in pips", "General");

		_multiplier = Param(nameof(Multiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Volume multiplier for slave mode", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timer = new Timer(_ => Process(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		base.OnStopped();
	}

	private void Process()
	{
		if (Mode == CopierMode.Master)
			ProcessMaster();
		else
			ProcessSlave();
	}

	private void ProcessMaster()
	{
		try
		{
			using var writer = new StreamWriter(_filePath, false, Encoding.UTF8);
			foreach (var order in Orders.Where(o => o.State == OrderStates.Active && (o.Comment is null || !o.Comment.StartsWith("C4F"))))
			{
				writer.WriteLine(string.Join(",",
					order.Id,
					order.Security?.Id,
					(int)order.Type,
					order.Price ?? 0m,
					order.Volume,
					order.StopPrice ?? 0m,
					order.TakeProfit ?? 0m));
			}
		}
		catch (Exception ex)
		{
			this.AddWarningLog($"Master mode error: {ex.Message}");
		}
	}

	private void ProcessSlave()
	{
		try
		{
			if (!File.Exists(_filePath))
				return;

			var known = new HashSet<long>();

			foreach (var line in File.ReadAllLines(_filePath, Encoding.UTF8))
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				var parts = line.Split(',');
				if (parts.Length < 5)
					continue;

				var ticket = long.Parse(parts[0], CultureInfo.InvariantCulture);
				var type = (OrderTypes)int.Parse(parts[2], CultureInfo.InvariantCulture);
				var price = decimal.Parse(parts[3], CultureInfo.InvariantCulture);
				var volume = decimal.Parse(parts[4], CultureInfo.InvariantCulture) * Multiplier;
				known.Add(ticket);
				var comment = $"C4F{ticket}";

				if (Orders.Any(o => o.Comment == comment && o.State == OrderStates.Active))
					continue;

				var order = new Order
				{
					Security = Security,
					Portfolio = Portfolio,
					Volume = volume,
					Comment = comment,
					Type = type,
					Price = type == OrderTypes.Market ? null : price,
					Side = type == OrderTypes.Sell || type == OrderTypes.SellLimit || type == OrderTypes.SellStop ? Sides.Sell : Sides.Buy
				};

				RegisterOrder(order);
			}

			foreach (var order in Orders.Where(o => o.State == OrderStates.Active && o.Comment is not null && o.Comment.StartsWith("C4F")))
			{
				var ticket = long.Parse(order.Comment.Substring(3), CultureInfo.InvariantCulture);
				if (!known.Contains(ticket))
					CancelOrder(order);
			}
		}
		catch (Exception ex)
		{
			this.AddWarningLog($"Slave mode error: {ex.Message}");
		}
	}
}

/// <summary>
/// Mode of trade copier.
/// </summary>
public enum CopierMode
{
	/// <summary>
	/// Write active orders to file.
	/// </summary>
	Master,

	/// <summary>
	/// Read file and replicate orders.
	/// </summary>
	Slave,
}
