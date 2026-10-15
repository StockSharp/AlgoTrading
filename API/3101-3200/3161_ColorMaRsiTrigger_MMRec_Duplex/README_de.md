# ColorMaRsi Trigger MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp High-Level-API-Port des MetaTrader-Experten **Exp_ColorMaRsi-Trigger_MMRec_Duplex.mq5**. Sie führt zwei unabhängige MaRsi-Trigger-Blöcke aus – einen für Long-Chancen und einen für Short-Chancen. Jeder Block bewertet ein zusammengesetztes Signal, das durch den Vergleich eines schnellen und langsamen gleitenden Durchschnitts zusammen mit einem schnellen und langsamen RSI generiert wird. Der zusammengesetzte Wert wird auf den Bereich `[-1, 1]` begrenzt, was das Verhalten des ursprünglichen Indikators reproduziert: `+1` markiert bullische Ausrichtung, `-1` bearische Ausrichtung und `0` zeigt gemischte Bedingungen an.

Ein Geldverwaltungs-"MMRec"-Modul überwacht die letzten Trades für jede Richtung. Wenn eine konfigurierbare Anzahl von Verlusten innerhalb eines gleitenden Fensters erscheint, wechselt der nächste Trade auf ein reduziertes Volumen bis sich die Performance erholt. Dies reproduziert die adaptive Positionsgrößenlogik der MetaTrader-Bibliothek `TradeAlgorithms.mqh` des Experten.

## Handelslogik

1. **Indikator-Pipeline** (pro Block):
   - Schnellen gleitenden Durchschnitt (`MA_fast`) und langsamen (`MA_slow`) auf dem gewählten angewendeten Preis und Zeitrahmen berechnen.
   - Schnellen RSI (`RSI_fast`) und langsamen RSI (`RSI_slow`) auf möglicherweise unterschiedlichen angewendeten Preisen berechnen.
   - Farbwertung aufbauen: bei `0` starten, `+1` addieren wenn `MA_fast > MA_slow` oder `-1` sonst, dann `+1` addieren wenn `RSI_fast > RSI_slow` oder `-1` sonst. Ergebnis auf `[-1, 1]` begrenzen.
   - Wertungshistorie speichern und mit dem konfigurierten `SignalBar`-Versatz lesen (der Standardwert entspricht der MetaTrader-Implementierung).

2. **Long-Block**:
   - **Einstieg**: erlaubt wenn keine Long-Position offen ist (Shorts werden zuerst gedeckt). Die vorherige Farbe (`SignalBar + 1`) muss `+1` sein während die aktuelle Farbe (`SignalBar`) `≤ 0` ist, was zeigt dass der bullische Block gerade neutralisiert wurde.
   - **Ausstieg**: wenn die vorherige Farbe negativ (`-1`) wird und Ausstiege aktiviert sind.

3. **Short-Block**:
   - **Einstieg**: erlaubt wenn keine Short-Position offen ist (Longs werden zuerst geschlossen). Die vorherige Farbe muss `-1` sein während die aktuelle Farbe `≥ 0` ist, was einen frischen bearischen-zu-neutralen Übergang signalisiert.
   - **Ausstieg**: wenn die vorherige Farbe positiv wird und Ausstiege aktiviert sind.

4. **Stops und Ziele**: optionale Stop-Loss- und Take-Profit-Abstände werden in Kursschritten ausgedrückt und auf jeder abgeschlossenen Kerze neu bewertet. Das Überschreiten einer Grenze schließt die jeweilige Position sofort.

5. **Geldverwaltung**: die Strategie speichert das Ergebnis jedes abgeschlossenen Trades (pro Richtung) und zählt die Anzahl der Verluste in den letzten `HistoryDepth` Trades. Wenn die Verlustzahl `LossTrigger` erreicht, verwendet die nächste Order das reduzierte Volumen. Andernfalls wird das normale Volumen verwendet.

## Parameter

| Gruppe | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Long-Block | `LongCandleType` | Zeitrahmen der den Long-MaRsi-Trigger-Block speist. | `H4` |
|  | `LongAllowOpen` / `LongAllowClose` | Long-Positionen öffnen / schließen aktivieren. | `true` |
|  | `LongStopLossPoints` / `LongTakeProfitPoints` | Schutzabstände in Instrument-Punkten. Auf `0` setzen zum Deaktivieren. | `1000` / `2000` |
|  | `LongSignalBar` | Anzahl abgeschlossener Kerzen beim Abtasten der Indikatorpuffer zu verschieben. | `1` |
|  | `LongRsiPeriod` / `LongRsiLongPeriod` | Schnelle und langsame RSI-Längen. | `3` / `13` |
|  | `LongMaPeriod` / `LongMaLongPeriod` | Schnelle und langsame gleitende Durchschnitt-Längen. | `5` / `10` |
|  | `LongRsiPrice` / `LongRsiLongPrice` | Angewendeter Preis für schnellen / langsamen RSI (Close, Open, High, Low, Median, Typical, Weighted). | `Weighted` / `Median` |
|  | `LongMaPrice` / `LongMaLongPrice` | Angewendeter Preis für schnellen / langsamen MA. | `Close` / `Close` |
|  | `LongMaType` / `LongMaLongType` | Algorithmen für gleitende Durchschnitte (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Geldverwaltung | `LongNormalVolume` / `LongReducedVolume` | Standard- und reduziertes Long-Trade-Volumen. | `0.1` / `0.01` |
|  | `LongHistoryDepth` | Anzahl jüngster Long-Trades vom Geldverwaltungsfilter beobachtet. | `5` |
|  | `LongLossTrigger` | Mindest-Verlustzahl innerhalb des Fensters um auf reduziertes Long-Volumen zu wechseln. | `3` |

| Gruppe | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Short-Block | `ShortCandleType` | Zeitrahmen der den Short-MaRsi-Trigger-Block speist. | `H4` |
|  | `ShortAllowOpen` / `ShortAllowClose` | Short-Positionen öffnen / schließen aktivieren. | `true` |
|  | `ShortStopLossPoints` / `ShortTakeProfitPoints` | Schutzabstände in Instrument-Punkten. Auf `0` setzen zum Deaktivieren. | `1000` / `2000` |
|  | `ShortSignalBar` | Anzahl abgeschlossener Kerzen beim Abtasten der Indikatorpuffer zu verschieben. | `1` |
|  | `ShortRsiPeriod` / `ShortRsiLongPeriod` | Schnelle und langsame RSI-Längen. | `3` / `13` |
|  | `ShortMaPeriod` / `ShortMaLongPeriod` | Schnelle und langsame gleitende Durchschnitt-Längen. | `5` / `10` |
|  | `ShortRsiPrice` / `ShortRsiLongPrice` | Angewendeter Preis für schnellen / langsamen RSI. | `Weighted` / `Median` |
|  | `ShortMaPrice` / `ShortMaLongPrice` | Angewendeter Preis für schnellen / langsamen MA. | `Close` / `Close` |
|  | `ShortMaType` / `ShortMaLongType` | Algorithmen für gleitende Durchschnitte (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Geldverwaltung | `ShortNormalVolume` / `ShortReducedVolume` | Standard- und reduziertes Short-Trade-Volumen. | `0.1` / `0.01` |
|  | `ShortHistoryDepth` | Anzahl jüngster Short-Trades vom Geldverwaltungsfilter beobachtet. | `5` |
|  | `ShortLossTrigger` | Mindest-Verlustzahl innerhalb des Fensters um auf reduziertes Short-Volumen zu wechseln. | `3` |

## Hinweise

- Angewendete Preisoptionen folgen MetaTrader-Semantik. Zum Beispiel entspricht `Weighted` `(High + Low + 2 * Close) / 4` und `Typical` `(High + Low + Close) / 3`.
- Wenn Long- und Short-Blöcke denselben Zeitrahmen teilen (Standard), speist eine einzelne Kerzen-Abonnement beide Rechner.
- Den Verlust-Trigger auf `0` setzen erzwingt sofort das reduzierte Volumen und spiegelt das Verhalten des ursprünglichen Geldverwaltungs-Helpers wider.
- Die Strategie verwendet Marktorders; der MetaTrader-`Deviation`-Parameter ist daher nicht erforderlich.
