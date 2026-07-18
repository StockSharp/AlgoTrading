# Einfache Niveaus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Öffnet Trades, wenn der Preis benutzerdefinierte Trendlinien kreuzt. Jede Linie kann Long-, Short- oder beide Richtungen auslösen. Stop-Loss und Take-Profit werden in Preisschritten festgelegt.

## Details

- **Einstiegskriterien**: Preis kreuzt eine konfigurierte Trendlinie
- **Long/Short**: Bestimmt durch Linienrichtung (Buy/Sell/Both)
- **Ausstiegskriterien**: Stop-Loss- oder Take-Profit-Niveaus
- **Stops**: Ja
- **Standardwerte**:
  - `StopLoss` = 300 steps
  - `TakeProfit` = 900 steps
  - `Volume` = 1
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Niveaus
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Verwendung

1. Trendlinien über `AddLine` erstellen und konfigurieren.
2. Strategie starten, um eingehende Kerzen zu überwachen.
3. Wenn der Preis eine aktive Linie in der angegebenen Richtung kreuzt, sendet die Strategie eine Marktorder.
4. Die Position wird geschlossen, wenn Stop-Loss oder Take-Profit erreicht wird.
