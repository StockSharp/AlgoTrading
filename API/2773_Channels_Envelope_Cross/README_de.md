# Channels Envelope Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Channels Envelope Kreuzungs-Strategie ist ein direkter Port des MetaTrader-Expertenberaters "Channels". Das System handelt stündliche Kerzen und überwacht einen schnellen zweiperiodigen exponentiellen gleitenden Durchschnitt (EMA) relativ zu drei EMA-basierten Envelopes (0.3%, 0.7% und 1.0% Abweichungen), die aus einem langsamen 220-Perioden-EMA berechnet werden. Ausbrüche des schnellen EMA durch diese Envelopes generieren gerichtete Einstiege, während ein optionaler Zeitfilter den Handel auf bestimmte Stunden beschränkt.

## Handelslogik

1. **Indikator-Stack**
   - Schneller EMA (Länge 2) berechnet auf Kerzen-Schlusspreisen.
   - Schneller EMA (Länge 2) berechnet auf Kerzen-Eröffnungspreisen.
   - Langsamer EMA (Länge 220) berechnet auf Kerzen-Schlusspreisen.
   - Drei Envelope-Niveaus abgeleitet vom langsamen EMA mit 0.3%, 0.7% und 1.0% Abweichungen.
2. **Long-Setup**
   - Ausgelöst wenn der schnelle Schluss-EMA über das untere 1.0%- oder 0.7%-Envelope kreuzt, für zwei aufeinanderfolgende Bars unter dem unteren 0.3%-Envelope bleibt, über den langsamen EMA kreuzt, oder durch das obere 0.3%- oder 0.7%-Envelope bricht. Jede dieser Bedingungen kann einen Long-Einstieg auslösen, wenn keine Position offen ist.
3. **Short-Setup**
   - Ausgelöst wenn der schnelle Öffnungs-EMA unter eines der oberen Envelopes kreuzt, unter den langsamen EMA fällt, oder die unteren Envelopes von oben durchdringt. Jede dieser Bedingungen kann einen Short-Einstieg auslösen, wenn keine Position offen ist.
4. **Risikomanagement**
   - Feste Stop-Loss- und Take-Profit-Niveaus (pro Seite) werden in Pips ausgedrückt und unter Verwendung der Instrumenten-Tick-Größe in Preisabstand umgerechnet. Wenn die Eingaben auf null gesetzt sind, wird das jeweilige Niveau nicht angewendet.
   - Unabhängige Trailing Stops für Long- und Short-Positionen bewegen den Schutz-Stop näher an den Marktpreis, wenn der Gewinn die Trailing-Distanz plus ein konfigurierbares Schrittinkrement überschreitet.
5. **Zeitfilter**
   - Wenn aktiviert, verarbeitet die Strategie nur Einstiege während des konfigurierten inklusiven Stundenbereichs. Positionen werden weiterhin verwaltet, wenn der Filter aktiv ist.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Für Markteinstiege verwendete Ordergröße (Lots oder Kontrakte je nach Wertpapier). |
| `UseTradeHours` | Aktiviert den Zeitfilter für Einstiege. |
| `FromHour` / `ToHour` | Inklusive Start- und Endstunden für das Handelsfenster (unterstützt Nachtbereiche). |
| `StopLossBuyPips` / `StopLossSellPips` | Stop-Loss-Abstand für Long/Short-Trades in Pips. |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Take-Profit-Abstand für Long/Short-Trades in Pips. |
| `TrailingStopBuyPips` / `TrailingStopSellPips` | Trailing-Stop-Abstand in Pips für Long/Short-Trades. |
| `TrailingStepPips` | Mindestinkrement (in Pips) das zum Bewegen eines Trailing Stops erforderlich ist. |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe (Standard ist 1-Stunden-Zeitrahmen). |

## Positionsverwaltung

- Beim Einstieg speichert die Strategie den Ausführungspreis, berechnet Stop-Loss- und Take-Profit-Ziele in absoluten Preiseinheiten und setzt Trailing-Niveaus zurück.
- Während eine Long-Position offen ist, wird der Stop-Loss aufwärts getrailed, wenn der Gewinn `TrailingStopBuyPips + TrailingStepPips` überschreitet. Die Strategie tritt beim Stop-Loss oder Take-Profit aus, je nachdem was zuerst getroffen wird.
- Während eine Short-Position offen ist, wird der Stop-Loss mit den Short-Seiten-Trailing-Parametern abwärts getrailed und Ausstiege werden symmetrisch ausgeführt.

## Hinweise

- Die Pip-Größe wird aus der Instrument-Tick-Größe abgeleitet. Für Drei- oder Fünf-Dezimal-Instrumente wird der Pip mit zehn multipliziert, um die MetaTrader-Logik zu emulieren.
- Die Strategie arbeitet mit einer einzigen Position gleichzeitig. Ein neuer Einstieg wird erst platziert, nachdem die bestehende Position geschlossen wurde.
- Aktivieren Sie `StartProtection` in der Basisklasse zum Schutz vor unerwarteten offenen Positionen nach Neustarts (bereits in der Implementierung aufgerufen).
