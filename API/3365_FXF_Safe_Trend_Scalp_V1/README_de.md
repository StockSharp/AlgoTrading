# Strategie FXF Safe Trend Scalp V1 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die FXF Safe Trend Scalp V1-Strategie handelt Ausbrüche von ZigZag-basierten Trendlinien und spiegelt das Verhalten des ursprünglichen Expertenberaters MetaTrader 4 wider. Es beobachtet den Abstand zwischen dem aktuellen Preis und dynamischen Widerstands-/Unterstützungslinien, die aus aktuellen ZigZag-Pivots erstellt wurden, und gleicht Trades mit einem Paar einfacher gleitender Durchschnitte aus. Schützender Stop-Loss, Take-Profit und ein Floating-Profit-Ausstieg reproduzieren die Geldverwaltungsregeln aus dem Quellcode.

## Handelslogik

1. **Zickzack-Trendlinien**
   - Ein manueller ZigZag-Detektor sucht anhand der konfigurierbaren Tiefen-, Abweichungs- und Backstep-Parameter nach abwechselnden Schwunghochs und -tiefs.
   - Die letzten vier Swing-Hochs definieren die aktive Widerstandslinie, während die letzten vier Swing-Tiefs die aktive Unterstützungslinie definieren. Die Strategie extrapoliert diese Linien kontinuierlich in den aktuellen Balken.
   - Ein Einstiegssignal wird vorbereitet, wenn sich der Schlusskurs einer Linie innerhalb eines festen Offsets (standardmäßig 10 Punkte) nähert.
2. **Filter für gleitenden Durchschnitt**
   - Ein schneller einfacher gleitender Durchschnitt (Länge 2) und ein langsamer einfacher gleitender Durchschnitt (Länge 50) filtern den Trend.
   - Short-Positionen erfordern einen schnellen MA unterhalb des langsamen MA, während Long-Positionen einen schnellen MA oberhalb des langsamen MA erfordern.
3. **Auftragsausführung**
   - Signale werden gespeichert und bei der nächsten fertigen Kerze aktiviert, entsprechend der „Neuer Balken“-Logik der MetaTrader-Version.
   - Bevor eine Position eröffnet wird, überprüft die Strategie, ob der Spread das konfigurierte Maximum nicht überschreitet und dass derzeit keine Position offen ist.
4. **Risikomanagement**
   - Stop-Loss- und Take-Profit-Distanzen werden in Punkten ausgedrückt und sofort nach Ausführung der Order angewendet.
   - Ein Floating-Profit-Ziel schließt die Position, sobald der nicht realisierte Gewinn (in Preiseinheiten mal Volumen) die konfigurierte Belohnung pro Lot übersteigt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `Candle Type` | Zeitrahmen, der für die Signalgenerierung verwendet wird. |
| `Volume` | Mit jedem Eintrag übermitteltes Handelsvolumen. |
| `ZigZag Depth` | Mindestanzahl von Balken zwischen bestätigten Pivots. |
| `ZigZag Deviation (pts)` | Minimale Preisbewegung in Punkten, bevor sich die Richtung ändert. |
| `ZigZag Backstep` | Vor dem Akzeptieren eines entgegengesetzten Pivots sind Barren erforderlich. |
| `Trend Offset (pts)` | Abstand von der Trendlinie, die ein Signal auslöst. |
| `Fast MA Length` | Länge des schnellen einfachen gleitenden Durchschnitts. |
| `Slow MA Length` | Länge des langsamen einfachen gleitenden Durchschnitts. |
| `Max Spread (pts)` | Maximal zulässiger Spread, ausgedrückt in Punkten. |
| `Stop Loss (pts)` | Abstand des Schutzstopps, gemessen ab dem Einstiegspreis. |
| `Take Profit (pts)` | Gewinnzielentfernung gemessen vom Einstiegspreis. |
| `Profit Target per Lot` | Erforderlicher variabler Gewinn (Preiseinheiten × Volumen), um die Position zu schließen. |

## Notizen

- Es wird jeweils nur eine Position besetzt. Signale werden ignoriert, solange ein Trade offen ist.
- Der Spread-Filter basiert auf den besten Geld-/Briefkursen, daher sollte die Strategie mit einer Datenquelle verbunden sein, die Informationen der Ebene 1 bereitstellt.
- Die Python-Version der Strategie wird wie gewünscht absichtlich weggelassen.

## Dateien

- `CS/FXFSafeTrendScalpV1Strategy.cs` – StockSharp Implementierung des Expert Advisors.
