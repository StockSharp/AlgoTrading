# WPRSI-Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den WPRSIsignal-Experten aus MetaTrader. Sie kombiniert den Williams Percent Range (WPR) und den Relative Strength Index (RSI), um Kauf- und Verkaufssignale zu generieren.

## Logik
- Ein **Kaufsignal** wird erzeugt, wenn der WPR von unten über -20 kreuzt und der RSI über 50 liegt. Das Signal wird nur bestätigt, wenn der WPR für die nächsten `FilterUp` Balken über -20 bleibt.
- Ein **Verkaufssignal** wird erzeugt, wenn der WPR von oben unter -80 kreuzt und der RSI unter 50 liegt. Das Signal wird nur bestätigt, wenn der WPR für die nächsten `FilterDown` Balken unter -80 bleibt.
- Bei Bestätigung eines Kaufsignals eröffnet die Strategie eine Long-Position, sofern keine aktive Long-Position vorhanden ist. Bei Bestätigung eines Verkaufssignals wird eine Short-Position eröffnet, sofern keine aktive Short-Position vorhanden ist.

## Parameter
- `Period` – Berechnungslänge für WPR und RSI.
- `FilterUp` – Anzahl der Balken, die WPR über -20 halten müssen, um ein Kaufsignal zu bestätigen.
- `FilterDown` – Anzahl der Balken, die WPR unter -80 halten müssen, um ein Verkaufssignal zu bestätigen.
- `CandleType` – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Verwendung
Hängen Sie die Strategie an ein beliebiges Wertpapier. Die Strategie verwendet `SubscribeCandles` und `Bind`, um Kerzendaten und Indikatorwerte zu empfangen. Positionen werden mit Marktaufträgen verwaltet: `BuyMarket` für Long-Einstiege und `SellMarket` für Short-Einstiege. Die Strategie implementiert weder Stop-Loss noch Take-Profit; Positionen werden durch entgegengesetzte Signale geschlossen.
