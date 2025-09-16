using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that notifies about position changes using own trade events.
/// </summary>
public class PositionsChangeInformerStrategy : Strategy
{
	private readonly StrategyParam<AlertType> _alertType;
	private readonly StrategyParam<string> _soundName;
	private readonly StrategyParam<MessageLanguage> _language;

	private decimal _position;
	private decimal _avgPrice;

	/// <summary>
	/// Type of alert to use.
	/// </summary>
	public enum AlertType
	{
		/// <summary>Write to the log.</summary>
		Alert,

		/// <summary>Play a sound.</summary>
		Sound,

		/// <summary>Send an email.</summary>
		Email
	}

	/// <summary>
	/// Language for notification messages.
	/// </summary>
	public enum MessageLanguage
	{
		/// <summary>English language.</summary>
		English,

		/// <summary>Russian language.</summary>
		Russian
	}

	/// <summary>
	/// Selected alert type.
	/// </summary>
	public AlertType Alert
	{
		get => _alertType.Value;
		set => _alertType.Value = value;
	}

	/// <summary>
	/// Sound file name.
	/// </summary>
	public string SoundName
	{
		get => _soundName.Value;
		set => _soundName.Value = value;
	}

	/// <summary>
	/// Message language.
	/// </summary>
	public MessageLanguage Language
	{
		get => _language.Value;
		set => _language.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PositionsChangeInformerStrategy"/>.
	/// </summary>
	public PositionsChangeInformerStrategy()
	{
		_alertType = Param(AlertType.Alert, nameof(Alert)).SetDisplay("Alert type");
		_soundName = Param("alert.wav", nameof(SoundName)).SetDisplay("Sound filename");
		_language = Param(MessageLanguage.Russian, nameof(Language)).SetDisplay("Language");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
			return;

		var side = trade.Order.Side;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		var oldPosition = _position;
		var signedVolume = side == Sides.Buy ? volume : -volume;
		var newPosition = oldPosition + signedVolume;

		string msg;

		if (oldPosition == 0)
		{
			msg = $"{trade.Security}: {GetSideName(side)} opened at {price:0.####}. Volume: {volume}";
			_avgPrice = price;
		}
		else if (Math.Sign(oldPosition) != Math.Sign(newPosition))
		{
			msg = $"{trade.Security}: reverse {GetSideName(side)} at {price:0.####}. Volume: {volume}";
			_avgPrice = price;
		}
		else if (Math.Abs(newPosition) > Math.Abs(oldPosition))
		{
			msg = $"{trade.Security}: {GetSideName(side)} added at {price:0.####}. Volume: {volume}";
			_avgPrice = (Math.Abs(oldPosition) * _avgPrice + price * volume) / Math.Abs(newPosition);
		}
		else
		{
			var closed = Math.Abs(oldPosition) - Math.Abs(newPosition);
			var profit = side == Sides.Sell
				? (price - _avgPrice) * closed
				: (_avgPrice - price) * closed;

			msg = $"{trade.Security}: closed at {price:0.####}. Result: {profit:0.##}";
			if (newPosition == 0)
				_avgPrice = 0m;
		}

		_position = newPosition;
		Notify(msg);
	}

	private void Notify(string message)
	{
		// Sound and email alerts can be implemented externally.
		AddInfoLog(message);
	}

	private string GetSideName(Sides side)
	{
		return side == Sides.Buy
			? (Language == MessageLanguage.Russian ? "покупка" : "buy")
			: (Language == MessageLanguage.Russian ? "продажа" : "sell");
	}
}

