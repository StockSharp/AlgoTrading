# Virtual TradePad Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert die Multi-Indikator-Dashboard-Logik des MetaTrader VirtualTradePad-Tools. Sie verfolgt zwölf Signale –
trendbasiert, auf Momentum und Kanälen basierend – und handelt nur, wenn eine konfigurierbare Anzahl von Indikatoren übereinstimmt. Das Ziel ist,
die visuelle Sentiment-Matrix des Originalpanels nachzubilden und in eine vollautomatisierte StockSharp-Strategie umzuwandeln.

## Funktionsweise

- **Daten**: handelt ein einzelnes Instrument auf dem ausgewählten Kerzentyp (Standard: 15 Minuten).
- **Indikatoren**:
  - Schnelle/langsame einfache gleitende Durchschnitte für die Kreuzungsrichtung.
  - MACD-Linie und Signal-Kreuzung.
  - Stochastik %K Überverkauft/Überkauft-Ausstiege (Level 20/80).
  - RSI 30/70 Schwellenwert-Umkehrungen.
  - CCI -100/+100 Umkehrungen.
  - Williams %R -80/-20 Umkehrungen.
  - Bollinger-Bänder Ausbruch zurück in den Kanal.
  - Gleitender Durchschnitt Envelope Ausbruch zurück in den Kanal.
  - Bill Williams Alligator Kiefer/Zähne/Lippen-Ausrichtung.
  - Kaufman Adaptive Moving Average Steigung (steigend/fallend).
  - Awesome Oscillator Nulllinien-Kreuzungen.
  - Ichimoku Tenkan-Kijun-Kreuzung.
- Jeder Indikator erzeugt eine Kauf- (+1), Verkauf- (-1) oder neutrale (0) Stimme. Wenn die Anzahl der Kaufstimmen (oder Verkaufsstimmen)
  den Parameter **MinimumConfirmations** erreicht und die Gegenseite übertrifft, eröffnet die Strategie eine Position in dieser Richtung.
- Die optionale **CloseOnOpposite**-Option schließt die Position, wenn die gegenteilige Stimmenzahl den Schwellenwert erreicht.
- **Risikomanagement**: optionaler Take-Profit und Stop-Loss, definiert in Preisschritten des Instruments.

## Parameter

- `FastMaLength`, `SlowMaLength` – Längen der gleitenden Durchschnitte für die Kreuzung.
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD-Konfiguration.
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` – Stochastik-Oszillator-Einstellungen.
- `RsiLength`, `CciLength`, `WilliamsLength` – Lookbacks der Oszillatoren.
- `BollingerLength`, `BollingerDeviation` – Bollinger-Bänder.
- `EnvelopeLength`, `EnvelopeDeviation` – prozentuale Envelopes um den SMA.
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` – Alligator SMMAs.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – Kaufman AMA-Konfiguration.
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` – Ichimoku-Linien.
- `AoShortPeriod`, `AoLongPeriod` – Awesome Oscillator-Fenster.
- `MinimumConfirmations` – Anzahl der übereinstimmenden Signale, die für den Einstieg erforderlich sind.
- `AllowLong`, `AllowShort` – Long/Short-Seiten aktivieren.
- `CloseOnOpposite` – Ausstieg, wenn die gegenteilige Stimmenzahl den Schwellenwert erfüllt.
- `TakeProfitPips`, `StopLossPips` – optionale Risikoziele in Preisschritten (0 deaktiviert).
- `CandleType` – Zeitrahmen/Datentyp für die Analyse.

## Zusammenfassung der Handelslogik

1. Alle Indikatoren aktualisieren, wenn eine Kerze schließt.
2. Bullische und bärische Stimmen der Indikatoren zählen.
3. Long/Short einsteigen, wenn Stimmen den Bestätigungsschwellenwert erreichen und die Gegenseite übertreffen.
4. Optional glätten, wenn die Gegenseite den Schwellenwert erreicht.
5. Optionalen Take-Profit/Stop-Loss in Preisschritten anwenden.

Die Strategie ist für Ermessenstrader konzipiert, denen das VirtualTradePad-Sentiment-Board gefiel, aber eine automatisierte
Implementierung innerhalb des StockSharp-Frameworks wünschen.
