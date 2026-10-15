# Trend Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
Die Trend Line-Strategie repliziert die Kerntrade-Management-Logik des ursprünglichen MetaTrader-Experten durch Kombination eines schnellen und langsamen linear gewichteten gleitenden Durchschnitts, eines Momentum-Filters und einer MACD-Bestätigung. Die Konvertierung konzentriert sich auf High-Level-StockSharp-Komponenten und behält den gleichen systematischen Ansatz bei, der auf Momentum-Ausbrüche in Trendrichtung wartet, bevor er einsteigt. Schutzstops, Gewinnziele und ein optionaler Trailing Stop in Preisschritten bieten ein ähnliches Risikomanagement wie der Quell-Experte.

## Handelslogik
1. Abonnement der konfigurierten Kerzenserie und Berechnung folgender Indikatoren:
   - Schneller linear gewichteter gleitender Durchschnitt (LWMA) mit dem konfigurierbaren `FastMaPeriod`.
   - Langsame LWMA mit dem konfigurierbaren `SlowMaPeriod`.
   - Momentum-Indikator mit Periode `MomentumPeriod`. Die jüngsten drei Momentum-Werte werden verfolgt, um die Multi-Bar-Momentum-Prüfung der MQL-Version zu emulieren.
   - MACD-Indikator (Moving Average Convergence Divergence) mit Standard-Schnell-/Langsam-/Signal-Längen. Die Strategie speichert die MACD- und Signalwerte für die spätere Verwendung.
2. Long einsteigen wenn:
   - Die schnelle LWMA über der langsamen LWMA liegt.
   - Mindestens einer der letzten drei Momentum-Werte größer oder gleich `MomentumBuyThreshold` ist.
   - Die MACD-Hauptlinie über der MACD-Signallinie liegt.
   - Keine offene Short-Position existiert (Short-Exposition wird geflacht, bevor eine Long-Position eröffnet wird).
3. Short einsteigen wenn:
   - Die schnelle LWMA unter der langsamen LWMA liegt.
   - Mindestens einer der letzten drei Momentum-Werte kleiner oder gleich `MomentumSellThreshold` ist (Schwellenwert sollte negativ sein, um Abwärtsbeschleunigung zu erkennen).
   - Die MACD-Hauptlinie unter der MACD-Signallinie liegt.
   - Keine offene Long-Position existiert (Long-Exposition wird geflacht, bevor eine Short-Position eröffnet wird).
4. Nach jedem Einstieg platziert die Strategie Schutz-Stop-Loss- und Take-Profit-Orders nach Distanz in Preisschritten. Beide Orders werden neu berechnet, wenn sich die Position ändert.
5. Ein Trailing Stop kann mit `TrailingStopSteps` und `TrailingTriggerSteps` aktiviert werden. Sobald die offene Position mindestens die Auslösedistanz gewinnt, wird der Stop-Loss auf `TrailingStopSteps` vom aktuellen Schlusskurs der verarbeiteten Kerze verschoben.

## Parameter
- `CandleType` – Datentyp für die Kerzenserie für jeden Indikator (Standard 1-Stunden-Zeitrahmen).
- `FastMaPeriod` – Periode der schnellen LWMA (Standard 6).
- `SlowMaPeriod` – Periode der langsamen LWMA (Standard 85).
- `MomentumPeriod` – Kerzenanzahl für die Momentum-Berechnung (Standard 14).
- `MomentumBuyThreshold` – Minimales positives Momentum für neue Long-Positionen (Standard 0.3).
- `MomentumSellThreshold` – Maximales (negatives) Momentum vor dem Öffnen neuer Short-Positionen (Standard -0.3).
- `MacdFastLength` – Schnelle EMA-Länge des MACD (Standard 12).
- `MacdSlowLength` – Langsame EMA-Länge des MACD (Standard 26).
- `MacdSignalLength` – Signal-EMA-Länge des MACD (Standard 9).
- `StopLossSteps` – Schutz-Stop-Abstand in Instrumentenschritten (Standard 20).
- `TakeProfitSteps` – Schutz-Gewinnziel-Abstand in Schritten (Standard 50).
- `TrailingStopSteps` – Trailing-Stop-Abstand in Schritten (Standard 40, deaktiviert bei null).
- `TrailingTriggerSteps` – Gewinn in Schritten vor Aktivierung des Trailing Stops (Standard 40).

## Hinweise
- Indikator-Bindungen basieren nur auf abgeschlossenen Kerzen; unfertige Daten werden ignoriert, um verfrühte Signale zu vermeiden.
- `SetStopLoss` und `SetTakeProfit` arbeiten mit Abständen in Preisschritten, was das Verhalten auf Instrumenten mit unterschiedlichen Tick-Größen konsistent hält.
- Wenn `MomentumSellThreshold` positiv gehalten wird, erwartet der Standardvergleich (`<= threshold`), dass dieser Wert negativ ist. Passen Sie das Vorzeichen bei der Optimierung der Strategie an.
- Der Trailing Stop arbeitet im End-of-Bar-Modus, da er bei Verarbeitung jeder abgeschlossenen Kerze aktualisiert wird, was das Originalskript widerspiegelt.
- Die Konvertierung lässt bewusst manuelles Trendlinien-Zeichnen und eigenkapitalbasierte Liquidierungsregeln aus, da diese auf interaktiven Terminal-Funktionen basierten, die in StockSharp nicht verfügbar sind. Alle anderen Kernein- und Risikoregeln bleiben erhalten.
