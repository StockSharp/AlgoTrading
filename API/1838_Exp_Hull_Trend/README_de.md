# Exp Hull-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Exp Hull-Trend-Strategie basiert auf dem Hull Moving Average (HMA)-Indikator. Der Algorithmus vergleicht eine Zwischen-Hull-Berechnung mit einem geglätteten Hull-Gleitenden Durchschnitt. Wenn die schnellere Hull-Linie die langsamere geglättete Linie von unten nach oben kreuzt, eröffnet die Strategie eine Long-Position. Wenn die schnelle Linie die geglättete Linie von oben nach unten kreuzt, eröffnet die Strategie eine Short-Position.

## Strategielogik

1. Einen gewichteten gleitenden Durchschnitt (WMA) des Schlusskurses mit Periode **Length / 2** berechnen.
2. Einen weiteren WMA des Schlusskurses mit Periode **Length** berechnen.
3. Den Zwischen-Hull-Wert konstruieren: `fast = 2 * WMA(Length/2) - WMA(Length)`.
4. Den Zwischenwert mit einem WMA der Periode `sqrt(Length)` glätten, um den endgültigen Hull-Wert `slow` zu erhalten.
5. Signale generieren:
   - **Long-Einstieg** – wenn `fast` über `slow` kreuzt.
   - **Short-Einstieg** – wenn `fast` unter `slow` kreuzt.
6. Positionen werden bei entgegengesetzten Signalen umgekehrt. Schutzorders werden über `StartProtection` verwaltet.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `Hull Length` | Basisperiode für die Hull-Berechnung. Bestimmt die Empfindlichkeit beider WMAs. |
| `Candle Type` | Zeitrahmen der Kerzen für Indikatorberechnungen. |

## Hinweise

- Die Strategie arbeitet ausschließlich auf abgeschlossenen Kerzen.
- Indikatorwerte werden über die High-Level-API gebunden, um manuelle Datensammlungen zu vermeiden.
- Das Volumen wird aus den Strategieeinstellungen entnommen; wenn sich die Signalrichtung ändert, wird die Position umgekehrt.
