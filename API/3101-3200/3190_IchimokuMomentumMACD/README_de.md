# Ichimoku Momentum MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- **Typ**: Trendfolge mit Momentum-Bestätigung.
- **Zeitrahmen**: Konfigurierbar (Standard 15-Minuten-Kerzen).
- **Indikatoren**: Ichimoku (Tenkan/Kijun), Linear Weighted Moving Averages, Momentum, MACD.
- **Stops**: Optionaler fester Take-Profit und Stop-Loss in Preispunkten über `StartProtection`.

## Strategiebeschreibung
Diese Strategie re­kreiert den Entscheidungsfluss des MetaTrader-Experten „Ichimoku" (Ordner `MQL/23469`). Sie bewertet die vorherige geschlossene Kerze und eröffnet neue Trades zu Beginn des nächsten Balkens, wenn alle vier Bestätigungen übereinstimmen:

1. **Ichimoku-Ausrichtung** – Tenkan (Konversionslinie) muss bei Long-Trades über Kijun (Basislinie) und bei Shorts darunter liegen.
2. **LWMA-Trendfilter** – Ein schneller linear gewichteter Moving Average muss für Longs über dem langsamen LWMA und für Shorts darunter bleiben. Beide Durchschnitte werden auf demselben Zeitrahmen wie die abonnierten Kerzen berechnet.
3. **Momentum-Stärke** – Die absolute Distanz des Momentum-Oszillators vom Neutralniveau 100 muss bei mindestens einer der letzten drei geschlossenen Kerzen größer als ein konfigurierbarer Schwellenwert sein.
4. **MACD-Bestätigung** – Das MACD-Histogramm muss mit der Richtung übereinstimmen (MACD-Linie über der Signallinie mit gleichem Vorzeichen positioniert).

Wenn alle vier Bedingungen bullisch ausgerichtet sind und die Strategie derzeit nicht Long ist, kauft sie das konfigurierte Volumen plus alle Einheiten, die zum Flatten einer bestehenden Short-Position erforderlich sind. Wenn die Bedingungen auf bärisch umschlagen, spiegelt sie den Prozess auf der Verkaufsseite. Entgegengesetzte Signale schließen immer offene Positionen und bieten einen deterministischen Ausstieg auch ohne Schutzorders.

Das Risikomanagement wird durch StockSharp's `StartProtection` gehandhabt, was feste Take-Profit- und Stop-Loss-Abstände in Instrument-Punkten ermöglicht. Das Setzen eines Parameters auf null deaktiviert das entsprechende Schutzbein.

## Parameterübersicht
| Parameter | Beschreibung |
|-----------|-------------|
| `FastMaPeriod` | Länge des schnellen linear gewichteten Moving Average für den Trendfilter. |
| `SlowMaPeriod` | Länge des langsamen linear gewichteten Moving Average. |
| `MomentumPeriod` | Lookback-Periode des Momentum-Oszillators. |
| `MomentumThreshold` | Mindestabstand von 100, den das Momentum bei mindestens einer der letzten drei Kerzen erreichen muss. |
| `MacdFastPeriod` | Schnelle EMA-Länge des MACD-Filters. |
| `MacdSlowPeriod` | Langsame EMA-Länge des MACD-Filters. |
| `MacdSignalPeriod` | Signal-EMA-Länge des MACD-Filters. |
| `TenkanPeriod` | Ichimoku Tenkan-sen-Länge. |
| `KijunPeriod` | Ichimoku Kijun-sen-Länge. |
| `SenkouSpanBPeriod` | Ichimoku Senkou Span B-Länge. |
| `TakeProfitPoints` | Optionaler Take-Profit-Abstand in Preispunkten (0 deaktiviert). |
| `StopLossPoints` | Optionaler Stop-Loss-Abstand in Preispunkten (0 deaktiviert). |
| `CandleType` | Zeitrahmen für alle Indikatorberechnungen. |

## Verwendungshinweise
- Die Strategie liest nur abgeschlossene Kerzen und speichert die Indikatorwerte des vorherigen Balkens, was der `shift=1`-Logik des MetaTrader-EA entspricht.
- `MomentumThreshold` anpassen, wenn zu Märkten mit anderer Momentum-Skalierung gewechselt wird (z.B. Krypto vs. Forex-Paare).
- Schutzorders werden intern verwaltet; Exchange-Level-Bracket-Orders werden nicht gesendet.
- Charts, wenn verfügbar, zeigen Preiskerzen, beide LWMAs, die Ichimoku-Wolke und ausgeführte Trades.
