# OCO-Orderausführungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert ein "One Cancels the Other"-Order-Ticket, das ursprünglich für MetaTrader geschrieben wurde. Sie ermöglicht dem Trader, bis zu vier unabhängige Preis-Trigger zu definieren:

- **Buy Limit Price**
- **Sell Limit Price**
- **Buy Stop Price**
- **Sell Stop Price**

Die Strategie abonniert Level1-Daten, um kontinuierlich das beste Angebot und die beste Nachfrage zu überwachen. Wenn ein Trigger-Preis erreicht wird, sendet sie eine Marktorder in der entsprechenden Richtung. Nach Ausführung einer Order werden Stop-Loss- und Take-Profit-Schutzmaßnahmen mit Abständen in Pips angewendet. Diese Abstände werden automatisch basierend auf dem `PriceStep` des Instruments in absolute Preise umgewandelt.

Wenn der **OCO-Modus** aktiviert ist, deaktiviert das Auslösen eines Triggers automatisch alle anderen Trigger und implementiert damit das klassische "einer-annulliert-den-anderen"-Verhalten. Wenn der OCO-Modus deaktiviert ist, bleiben andere Trigger aktiv und können zusätzliche Positionen eröffnen, wenn der Preis weiter steigt oder fällt.

## Details

- **Einstiegskriterien**:
  - Long, wenn `Ask <= BuyLimitPrice` (Buy-Limit-Trigger).
  - Long, wenn `Ask >= BuyStopPrice` (Buy-Stop-Trigger).
  - Short, wenn `Bid >= SellLimitPrice` (Sell-Limit-Trigger).
  - Short, wenn `Bid <= SellStopPrice` (Sell-Stop-Trigger).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Positionen werden automatisch durch vordefinierte Stop-Loss- oder Take-Profit-Niveaus geschlossen.
- **Stops**: Ja, Stop-Loss und Take-Profit in Pips.
- **Standardwerte**:
  - `StopLossPips` = 300.
  - `TakeProfitPips` = 300.
  - `OCO Mode` = aktiviert.
- **Filter**:
  - Kategorie: Orderausführung.
  - Richtung: Beide.
  - Indikatoren: Keine.
  - Stops: Ja.
  - Komplexität: Einfach.
  - Zeitrahmen: Tick-basiert.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
