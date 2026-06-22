# PChannel-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das **PChannel-System** verwendet einen Preiskanal-Ausbruch mit verzögerter Bestätigung. Es verfolgt das höchste Hoch und das niedrigste Tief über einen konfigurierbaren Zeitraum. Wenn der Preis den Kanal durchbricht und dann wieder schließt, steigt die Strategie in Richtung des Ausbruchs ein und schließt dabei gegenteilige Positionen. Optionale Stop-Loss- und Take-Profit-Levels steuern das Risiko.

## Parameter
- `Period` – Rückblicklänge für den Kanal.
- `Shift` – Anzahl der Bars zur Verzögerung der Kanalwerte.
- `StopLoss` – absoluter Preisabstand für den Schutz-Stop.
- `TakeProfit` – absoluter Preisabstand für das Gewinnziel.
- `CandleType` – Kerzenserie für Berechnungen.

## Handelslogik
1. Kanalgrenzen aus den letzten `Period` Kerzen mit optionalem `Shift` berechnen.
2. Wenn die vorherige Kerze außerhalb des Kanals schloss und die aktuelle Kerze wieder zurückkehrt, eine Position in Ausbruchsrichtung eröffnen.
3. Die entgegengesetzte Position, falls vorhanden, vor dem Öffnen einer neuen schließen.
4. Aktive Trades überwachen und bei Erreichen von `StopLoss` oder `TakeProfit` aussteigen.

Diese Strategie hat noch keine Python-Implementierung.
