# Farhad Hill Version 2-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters „Farhad Hill Version 2“.
Es kombiniert mehrere Indikatorfilter, um Trendumkehrungen bei Forex-Symbolen zu handeln. Die
Die konvertierte Logik behält den ursprünglichen Indikatorstapel bei (MACD, Stochastic, Parabolic SAR,
Momentum und optionaler Crossover-Durchschnitt) und sein Money-Management plus Trailing
Verhalten.

Die Strategie funktioniert in einem einzigen Zeitrahmen (Standard 30-Minuten-Kerzen) und öffnet nur eine
Position auf einmal. Es gibt schützende Stop-Loss-, Take-Profit- und drei Trailing-Stop-Stile
unterstützt, um die MQL-Version zu spiegeln. Alle Kommentare im Code werden in englischer Sprache bereitgestellt
angefordert.

## Handelslogik
- **MACD-Filter** – wenn aktiviert, erfordern Longs eine MACD Hauptlinie unterhalb der Signallinie;
Kurzschlüsse erfordern MACD Hauptleitung über der Signalleitung.
- **Stochastic Levelfilter** – Long-Nachfrage %K unter dem unteren Schwellenwert, Short-Nachfrage
%K über dem oberen Schwellenwert. Wenn der optionale Kreuzfilter aktiviert ist, ist dies bullisch
Für Long-Positionen ist ein %K/%D-Kreuz (von unten nach oben) und für Shorts ein rückläufiges Kreuz erforderlich.
- **Parabolic SAR-Filter** – Long-Positionen erfordern SAR unter dem Preis mit einem Abwärtsschritt
(vorheriger SAR höher als aktuell); Shorts erfordern SAR über dem Preis mit einer Aufwärtsbewegung
Schritt. Bei der Umrechnung werden geschlossene Kerzenpreise als Referenz verwendet.
- **Momentum-Filter** – berechnet auf Grundlage der Kerzeneröffnungspreise, passend zu den MQL-Einstellungen.
Long-Positionen benötigen Schwung unterhalb der unteren Schwelle, Short-Positionen benötigen Schwung oberhalb der Obergrenze
Schwelle.
- **Gleitender Durchschnitt (optional)** – konfigurierbarer MA-Typ, angewendeter Preis und Zeiträume.
Long-Positionen benötigen den schnellen MA über dem langsamen MA; Shorts brauchen die umgekehrte Beziehung.

Die Strategie wertet nur Signale bei abgeschlossenen Kerzen aus und überspringt neue Einträge, wenn ein
offene Stelle vorhanden. Die Eingabe erfolgt mit Marktaufträgen anhand des berechneten Lots
Größe.

## Positionsmanagement
- **Stop-Loss / Take-Profit** – angegeben in Pips. Ein Pip wird vom Instrument abgeleitet
`PriceStep`, bei Nichtverfügbarkeit auf `0.0001` zurückgreifen.
- **Trailing-Stop-Typen**
  1. Sofort – sobald sich der Preis über die Stop-Distanz hinaus bewegt, folgt der Stop dem Preis.
  2. Verzögert – wartet darauf, dass sich der Preis um die Nachlaufdistanz vom vorherigen Einstieg bewegt
nachlaufend mit einem festen Offset.
  3. Dreistufig – reproduziert die ursprüngliche dreistufige Logik mit zwei Break-Even-Schritten
und eine letzte Nachlaufdistanz.
- Schutzaufträge werden mit `SellStop`/`BuyStop` (für Stop-Loss) und platziert
`SellLimit`/`BuyLimit` (für Take-Profit), damit sie an der Börse sichtbar bleiben.

## Money-Management
- **Fixes Lot** – handelt mit dem konfigurierten festen Volumen. Wenn `AccountIsMini` aktiviert ist, viele
werden mit einem Minimum von 0,1 auf Minilosgrößen umgerechnet.
- **Prozentuales Risiko** – reproduziert die ursprüngliche Formel
`floor(FreeMargin * percent / 10000) / 10`, durch das Limit `MaxLots` begrenzt und angepasst
für Minikonten bei Bedarf. Wenn der Portfoliowert nicht verfügbar ist, die Strategie
fällt auf das feste Los zurück.

## Parameter
Alle Parameter werden durch `StrategyParam<T>`-Objekte verfügbar gemacht und können optimiert oder optimiert werden
von der Benutzeroberfläche geändert. Schlüsselgruppen:

| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Allgemein | `CandleType` | Zeitrahmen der für Signale verwendeten Kerzen |
| Geldmanagement | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| Risiko | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| Indikatoren | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## Anmerkungen und Annahmen
- Parabolic SAR-Vergleiche verwenden den Schlusskurs der Kerze, um Bid/Ask-Prüfungen annähernd zu ermitteln
von MT4. Dadurch bleibt die Logik für historische Daten deterministisch.
- Das Geldmanagement erfordert ein verbundenes Portfolio, um aktuelles Eigenkapital zu erhalten. sonst
Es wird das Festvolumen verwendet.
- Indikatorkombinationen werden nur für abgeschlossene Kerzen verarbeitet, um vorzeitige Ergebnisse zu vermeiden
Signale auf Teildaten.

## Dateien
- `CS/FarhadHillVersion2Strategy.cs` – C#-Implementierung der Strategie.
- `README.md` – Dieses Dokument.
- `README_ru.md` – Russische Übersetzung.
- `README_zh.md` – Chinesische Übersetzung.
