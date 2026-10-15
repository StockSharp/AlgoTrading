# Tunnelmethode EMA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Tunnel-Methode-EMA-Strategie** repliziert den ursprünglichen Expertenratgeber MetaTrader „Tunnel-Methode“ auf der StockSharp-High-Level-Strategie API. Es arbeitet mit stündlichen Kerzen und vergleicht drei exponentielle gleitende Durchschnitte (EMAs), die auf Schlusskursen basieren:

- **Fast EMA (12 Perioden)** erfasst unmittelbare Impulsverschiebungen.
- **Medium EMA (144 Perioden)** spiegelt das „Tunnel“-Zentrum wider, das zur Validierung kurzer Signale verwendet wird.
- **Slow EMA (169 Perioden)** bietet den langfristigen Richtungsfilter für lange Trades.

Die Strategie sorgt dafür, dass sich Positionen gegenseitig ausschließen (entweder Long, Short oder Flat) und verwaltet das Risiko dynamisch durch explizite Stop-Loss-, Take-Profit- und Trailing-Stop-Kontrollen.

## Signallogik
### Lange Einträge
1. Warten Sie auf eine abgeschlossene Kerze (keine Intrabar-Entscheidungen).
2. Erkennen Sie einen bullischen Crossover, bei dem sich der schnelle EMA (12) von unten nach oben über den langsamen EMA (169) bewegt.
3. Bestätigen Sie, dass derzeit keine Position offen ist, und senden Sie eine Marktkauforder für das konfigurierte Volumen.

### Kurze Einträge
1. Warten Sie auf eine fertige Kerze.
2. Erkennen Sie einen bearischen Crossover, bei dem sich der schnelle EMA (12) von oben nach unter den mittleren EMA (144) bewegt.
3. Bestätigen Sie, dass derzeit keine Position offen ist, und erteilen Sie einen Marktverkaufsauftrag.

### Positionsmanagement
- **Stop-Loss**: Schließt den Handel, wenn sich der Preis um `StopLossPoints` gegen die Position bewegt (umgerechnet in einen absoluten Preis mithilfe der Sicherheitspreisstufe).
- **Take-Profit**: Gewinne werden gesichert, sobald der Preis um `TakeProfitPoints` gegenüber dem Einstiegspreis steigt.
- **Trailing Stop**: Nachdem der Trade einen Gewinn von mindestens `TrailingTriggerPoints` erzielt hat, verfolgt die Strategie den Preis mit `TrailingStopPoints`. Bei Long-Trades folgt es dem höchsten Hoch seit dem Einstieg; Bei Short-Trades folgt es dem tiefsten Tief seit dem Einstieg. Eine Umkehr zum Schlussniveau schließt die Position.
- **Zustandsrücksetzung**: Nach jedem Exit (manuell oder schützend) wird der interne Trailing-Status zurückgesetzt, um Störungen bei nachfolgenden Trades zu vermeiden.

## Standardparameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Stündliche Kerzen, die für EMA-Berechnungen verwendet werden. |
| `FastLength` | 12 | Länge des schnellen EMA, der auf die jüngste Preisaktion reagiert. |
| `MediumLength` | 144 | Länge des Tunnelzentrums EMA für kurze Validierung. |
| `SlowLength` | 169 | Länge der Tunnelgrenze EMA für lange Validierung. |
| `StopLossPoints` | 25 | Schutzanschlagabstand in Instrumentenpunkten. |
| `TakeProfitPoints` | 230 | Gewinnen Sie die Zielentfernung in Instrumentenpunkten. |
| `TrailingStopPoints` | 35 | Abstand, der vom Trailing Stop eingehalten wird, sobald er aktiv ist. |
| `TrailingTriggerPoints` | 20 | Gewinnschwelle erforderlich, bevor das Trailing beginnt. |

## Filter und Eigenschaften
- **Kategorie**: Trendfolgendes Crossover.
- **Instrumente**: Funktioniert auf jedem Instrument, das stündliche Kerzen und einen zuverlässigen Preisschritt bietet.
- **Richtung**: Handelt sowohl Long- als auch Short-Positionen und hält niemals gleichzeitige Positionen.
- **Zeitrahmen**: Standardmäßig 1-stündige Kerzen (konfigurierbar über `CandleType`).
- **Risikokontrollen**: Harter Stop-Loss, Take-Profit und Trailing-Stop in der Strategielogik implementiert.
- **Datenanforderungen**: Verlässt sich ausschließlich auf Kerzenschlusskurse; Es sind keine zusätzlichen Indikatoren oder Markttiefe erforderlich.

## Notizen
- Alle Indikatorwerte stammen aus den EMA-Implementierungen von StockSharp, um die Konsistenz mit den übergeordneten API-Richtlinien sicherzustellen.
- Die Strategie ignoriert unvollendete Kerzen, um Doppelzählungen von Signalen oder das Handeln auf Teildaten zu vermeiden.
- Trailing-Stop-Anpassungen berücksichtigen den `PriceStep` des Wertpapiers über `ShrinkPrice` und halten die Ausstiegsniveaus an gültigen Tick-Inkrementen ausgerichtet.
- Die Standardparameter spiegeln die ursprünglichen MQL-Einstellungen wider, können jedoch mit den Parameteroptimierungstools von StockSharp optimiert werden.
