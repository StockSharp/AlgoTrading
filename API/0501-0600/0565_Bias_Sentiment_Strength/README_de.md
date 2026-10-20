# Bias Sentiment Strength

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Auf abgeschlossenen 15-Minuten-Kerzen werden MACD(12,26,9), Wilder-RSI(14), Stochastik(21,14,14), Awesome Oscillator(5,34), Alligator und Volumengewichtung kombiniert. Roh-K verwendet 21 Hochs/Tiefs; `Ks` ist sein SMA über 14 Werte, `D` ein weiterer SMA über 14. Alligator nutzt Medianpreis-SMMAs: Kiefer 13 um 8 versetzt, Zähne 8 um 5, Lippen 5 um 3.

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`. Alle Komponenten müssen gebildet sein; standardmäßig mindestens 47 vollständige Kerzen. Ungültige Konfiguration wird ausdrücklich abgelehnt.

Über `BassThreshold` ist das Ziel Long `Volume`, unter `-BassThreshold` Short `Volume`. Standardgrenze ist null. Es gelten Pegel, sodass nach einem Stop kein neuer Nulldurchgang nötig ist. Umkehrorders umfassen `Volume + abs(Position)`; offene Orders blockieren neue. Der native lokale Marktstop schließt die gesamte ausgeführte Position nach `StopLossPercent` Prozent, standardmäßig 1. Kein Gewinnlimit.

Der native lokale Schutz verwendet Kerzen und abonnierte Preisaktualisierungen des Instruments. Abstände beziehen sich auf tatsächliche Einstiegsausführungen. Ausstiege sind Marktorders; der Ausführungspreis hängt vom verfügbaren Markt und Preisraster ab.

## Parameter

| Parameter | Standardwert |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |
