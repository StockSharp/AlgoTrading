using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Displays performance statistics grouped by magic numbers.
/// </summary>
public class ShurikenLiteStrategy : Strategy
{
	private readonly StrategyParam<string> _magicNumbers;
	private readonly StrategyParam<bool> _showScores;

	private readonly int[] _trades = new int[10];
	private readonly int[] _wins = new int[10];
	private readonly int[] _losses = new int[10];
	private readonly decimal[] _pips = new decimal[10];
	private readonly int[] _magicNums = new int[10];

	public string MagicNumbers { get => _magicNumbers.Value; set => _magicNumbers.Value = value; }
	public bool ShowScores { get => _showScores.Value; set => _showScores.Value = value; }

	public ShurikenLiteStrategy()
	{
		_magicNumbers = Param(nameof(MagicNumbers), "1,2,3,4,5,6,7,8,9,10")
			.SetDisplay("Magic Numbers", "Comma separated identifiers", "General");

		_showScores = Param(nameof(ShowScores), true)
			.SetDisplay("Show Scores", "Log statistics", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		ParseMagicNumbers();
	}

	private void ParseMagicNumbers()
	{
		var parts = MagicNumbers.Split(',');
		for (var i = 0; i < _magicNums.Length; i++)
		{
			if (i < parts.Length && int.TryParse(parts[i].Trim(), out var num))
				_magicNums[i] = num;
			else
				_magicNums[i] = i + 1;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (!ShowScores)
			return;

		var order = trade.Order;
		if (order is null)
			return;

		var comment = order.Comment;
		if (string.IsNullOrEmpty(comment))
			return;

		if (!int.TryParse(comment, out var magic))
			return;

		for (var i = 0; i < _magicNums.Length; i++)
		{
			if (_magicNums[i] != magic)
				continue;

			_trades[i]++;

			var step = order.Security?.Step ?? 1m;
			var diff = (trade.Trade.Price - order.Price) / step;
			if (order.Direction == Sides.Sell)
				diff = -diff;

			_pips[i] += diff;
			if (diff >= 0)
				_wins[i]++;
			else
				_losses[i]++;
			break;
		}

		LogInfo(GetSummary());
	}

	private string GetSummary()
	{
		int totalTrades = 0;
		int totalWins = 0;
		int totalLosses = 0;
		decimal totalPips = 0m;

		for (var i = 0; i < _trades.Length; i++)
		{
			totalTrades += _trades[i];
			totalWins += _wins[i];
			totalLosses += _losses[i];
			totalPips += _pips[i];
		}

		decimal winRate = 0m;
		if (totalTrades > 0)
			winRate = (decimal)totalWins / totalTrades * 100m;

		decimal profitFactor = 0m;
		if (totalLosses > 0)
			profitFactor = (decimal)totalWins / totalLosses;

		return $"Trades:{totalTrades} Wins:{totalWins} Losses:{totalLosses} Win%:{winRate:F1} Pips:{totalPips:F0} PF:{profitFactor:F2}";
	}
}
