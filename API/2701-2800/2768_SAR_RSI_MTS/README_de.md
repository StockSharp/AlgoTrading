# SAR RSI MTS Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **SAR RSI MTS Strategie** ist eine direkte Übersetzung des ursprünglichen MetaTrader 5-Expertenberaters "SAR RSI MTS" in die StockSharp High-Level-API. Das System folgt der Richtung des Parabolic SAR-Indikators und bestätigt Einstiege mit dem Relative Strength Index (RSI). Es arbeitet nur auf abgeschlossenen Kerzen (Standard-Zeitrahmen 1 Stunde) und respektiert ein konfigurierbares Maximum für die Netto-Positionsgröße.

## Indikatoren und Daten

- **Parabolic SAR** (`Acceleration = SarStep`, `AccelerationStep = SarStep`, `AccelerationMax = SarMax`).
- **Relative Strength Index** mit anpassbarer Periode und neutralem Niveau (Standard 50).
- Kerzen werden von `CandleType` geliefert, das standardmäßig stündliche Zeitrahmen-Daten verwendet.

Intern berechnet die Strategie einen Pip-Wert aus den Sicherheits-Metadaten. Wenn das Symbol 3 oder 5 Dezimalstellen hat, multipliziert es den Preisschritt mit 10, was der Pip-Verarbeitung des ursprünglichen MQL-Programms entspricht.

## Einstiegslogik

Ein neuer Trade wird beim Schließen jeder fertigen Kerze ausgewertet, sobald beide Indikatoren gültige Werte produziert haben:

- **Long-Setup**
  1. Der Parabolic SAR-Wert der vorherigen Bar liegt unter dem aktuellen Schlusskurs und der aktuelle SAR hat sich gegenüber dem vorherigen Wert erhöht.
  2. RSI liegt über dem neutralen Schwellenwert und steigt im Vergleich zur vorherigen Ablesung.
  3. Wenn das Konto bereits netto short ist, kauft die Strategie zunächst genug Volumen, um die Position umzukehren, und eröffnet dann einen neuen Long entsprechend dem `Volume`-Parameter unter Einhaltung des `MaxPosition`-Limits.

- **Short-Setup**
  1. Der vorherige Parabolic SAR-Wert liegt über dem aktuellen Schlusskurs und der aktuelle SAR hat sich verringert.
  2. RSI liegt unter dem neutralen Schwellenwert und fällt im Vergleich zum vorherigen Wert.
  3. Bestehende Long-Positionen werden abgebaut, bevor der neue Short eröffnet wird. Weitere Shorts sind erlaubt, bis die absolute Position `MaxPosition` erreicht.

Alle Vergleiche verwenden die Instrumentgenauigkeit, damit Gleichheitstests dem ursprünglichen `CompareDoubles`-Helper aus MQL entsprechen.

## Ausstieg und Risikomanagement

Risikokontrollen werden vor der Prüfung neuer Einstiege bei jeder fertigen Kerze ausgewertet:

- **Fester Stop-Loss** in Pips, umgerechnet in Preiseinheiten und auf den durchschnittlichen Einstiegspreis der aktuellen Netto-Position angewendet.
- **Fester Take-Profit** in Pips, symmetrisch zum Stop-Loss behandelt.
- **Trailing Stop**, der erst aktiv wird, nachdem der unrealisierte Gewinn `TrailingStop + TrailingStep` überschreitet. Der Stop wird in diskreten Schritten verschoben und ahmt die "Trailing"-Routine der MQL-Strategie nach.
- Wenn keines der oben genannten zutrifft, wird der Trailing-Zustand zurückgesetzt, sobald die Position flach wird.

Alle Ausstiege schließen die gesamte Netto-Position (Long oder Short). Wenn eine Schutzregel ausgelöst wird, überspringt die Strategie die Signalauswertung für dieselbe Bar und spiegelt das Verhalten von broker-seitigen Stop-Orders in der ursprünglichen Implementierung wider.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossPips` | Stop-Loss-Abstand in Pips. Ein Wert von `0` deaktiviert den Schutz-Stop. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Deaktiviert wenn auf `0` gesetzt. |
| `TrailingStopPips` | Abstand des Trailing Stops. Deaktiviert wenn auf `0` gesetzt. |
| `TrailingStepPips` | Mindestpreisverbesserung, die erforderlich ist, bevor der Trailing Stop vorgerückt wird. |
| `SarStep` | Beschleunigungsschritt für Parabolic SAR; wird auch als anfänglicher Beschleunigungsfaktor verwendet. |
| `SarMax` | Maximaler Beschleunigungsfaktor für Parabolic SAR. |
| `RsiPeriod` | Lookback-Periode für den RSI-Indikator. |
| `RsiNeutralLevel` | RSI-Schwellenwert zur Trennung von bullischer und bärischer Tendenz (Standard 50). |
| `CandleType` | Für Berechnungen verwendetes Kerzen-Abonnement (Standard 1 Stunde). |
| `MaxPosition` | Maximal erlaubte absolute Netto-Position der Strategie. |

## Zusätzliche Hinweise

- Die Standardkonfiguration reproduziert die ursprünglichen EA-Eingaben: 10 Pip Stop, 40 Pip Ziel, 15/5 Pip Trailing Stop, Parabolic SAR `0.05/0.5` und RSI-Periode `14`.
- Das Volumen wird durch die Basis-`Strategy.Volume`-Eigenschaft gesteuert. Die Positionsskalierung respektiert `MaxPosition` und behandelt Umkehrungen automatisch.
- Indikator-Bindungen und Order-Routing stützen sich vollständig auf die StockSharp High-Level-API ohne manuellen Serienzugriff und stellen die Einhaltung der Projektrichtlinien sicher.
