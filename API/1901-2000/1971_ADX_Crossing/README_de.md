# ADX-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **ADX-Kreuzungs-Strategie** basiert auf dem Average Directional Index (ADX). Sie analysiert die Kreuzung des positiven Richtungsindikators (+DI) und des negativen Richtungsindikators (-DI), um potenzielle Trendwechsel zu identifizieren.

Wenn +DI über -DI kreuzt, betrachtet die Strategie dies als bullishes Signal und kann Long-Positionen eröffnen, während bestehende Short-Positionen optional geschlossen werden. Wenn +DI unter -DI kreuzt, wird dies als bärisches Signal behandelt, das Short-Einstiege und das optionale Schließen von Long-Positionen auslöst. Optionale Stop-Loss- und Take-Profit-Niveaus werden durch integriertes Risikomanagement unterstützt.

## Indikator

Die Strategie verwendet den `AverageDirectionalIndex`-Indikator aus StockSharp. Nur die Richtungslinien werden benötigt; der ADX-Hauptwert wird nicht für Entscheidungen verwendet.

## Parameter

- `ADX Period` – Länge der ADX-Berechnung. Standard ist `50`.
- `Candle Type` – Zeitrahmen für das Kerzen-Abonnement. Standard ist `1 Stunde`.
- `Allow Buy Open` – Long-Positionen eröffnen aktivieren. Standard ist `true`.
- `Allow Sell Open` – Short-Positionen eröffnen aktivieren. Standard ist `true`.
- `Allow Buy Close` – Schließen von Long-Positionen bei Verkaufssignal erlauben. Standard ist `true`.
- `Allow Sell Close` – Schließen von Short-Positionen bei Kaufsignal erlauben. Standard ist `true`.
- `Stop Loss` – Stop-Loss-Abstand in absoluten Preiseinheiten. Standard ist `1000`.
- `Take Profit` – Take-Profit-Abstand in absoluten Preiseinheiten. Standard ist `2000`.

## Handelslogik

1. Kerzen des ausgewählten Zeitrahmens abonnieren und den ADX-Indikator berechnen.
2. Vorherige Werte von +DI und -DI verfolgen, um Kreuzungen zu erkennen.
3. Bei bullischer Kreuzung (+DI kreuzt über -DI):
   - Short-Position schließen, wenn `Allow Sell Close` aktiviert ist.
   - Long-Position eröffnen, wenn `Allow Buy Open` aktiviert ist.
4. Bei bärischer Kreuzung (+DI kreuzt unter -DI):
   - Long-Position schließen, wenn `Allow Buy Close` aktiviert ist.
   - Short-Position eröffnen, wenn `Allow Sell Open` aktiviert ist.
5. Schutzende Stop-Loss- und Take-Profit-Niveaus werden mit `StartProtection` angewendet.

## Hinweise

- Es werden nur abgeschlossene Kerzen (`CandleStates.Finished`) verarbeitet.
- Die Strategie nutzt das integrierte StockSharp-Risikomanagement für Stop-Niveaus.
- Positionen werden durch Senden eines entgegengesetzten Marktauftrags mit dem aktuellen Volumen geschlossen.

Diese Strategie dient Bildungszwecken und erfordert möglicherweise weitere Anpassungen vor dem Einsatz auf Live-Märkten.
