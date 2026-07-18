# Drei Soldaten Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader-Experten `Expert_ABC_WS_Stoch.mq5`, der klassische Drei-Kerzen-Umkehrmuster mit einer Stochastic-Oszillatorbestätigung kombiniert. Ein Long-Signal erfordert die bullische „Three White Soldiers“-Formation zusammen mit einer überverkauften Stochastic-Signallinie, während ein Short-Signal auf der bärischen „Three Black Crows“-Formation basiert, die durch eine überkaufte Stochastic bestätigt wird. Die Ausgangslogik überwacht Überkreuzungen der Signalleitung über konfigurierbare Bänder bis hin zu Schließpositionen.

## Handelslogik

1. **Mustererkennung**
   - Verfolgen Sie die letzten drei abgeschlossenen Kerzen.
   - Identifizieren Sie *drei weiße Soldaten*, wenn alle drei Kerzen bullisch sind und jeder Schlusskurs höher ist als der vorherige.
   - Identifizieren Sie *Three Black Crows*, wenn alle drei Kerzen bärisch sind und jeder Schlusskurs niedriger ist als der vorherige.
2. **Oszillatorbestätigung**
   - Berechnen Sie einen Stochastic-Oszillator mit `%K`-, `%D`- und `Slowing`-Perioden, die mit denen des ursprünglichen Experten identisch sind (standardmäßig 47, 9, 13).
   - Verwenden Sie die Signalleitung (`%D`) als Bestätigung:
     - Geben Sie „long“ ein, wenn der Wert der vorherigen Signallinie unter dem Überverkaufsschwellenwert liegt (Standard: `30`).
     - Geben Sie Short ein, wenn der vorherige Signallinienwert über dem Überkauft-Schwellenwert liegt (Standard: `70`).
3. **Ausstiegsbedingungen**
   - Schließen Sie einen Long-Trade, wenn die Signallinie die untere oder obere Ausstiegsschwelle überschreitet (Standard: `20` und `80`).
   - Schließen Sie einen Short-Trade, wenn die Signallinie wieder unter diese Schwellenwerte fällt.
   - Beide Ausgangsprüfungen stützen sich auf die vorherigen und vorherigen Signalleitungswerte, um echte Überkreuzungen zu erkennen.

## Parameter

| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandleType` | `1h` Zeitrahmen | Zeitrahmen für das Kerzenabonnement. |
| `StochKPeriod` | `47` | Lookback-Zeitraum für `%K`. |
| `StochDPeriod` | `9` | Gleitende durchschnittliche Länge der Signalleitung. |
| `StochSlowing` | `13` | Zusätzliche Glättung wurde auf `%K` angewendet. |
| `OversoldLevel` | `30` | Signalleitungspegel zur Bestätigung einer langen Eingabe erforderlich. |
| `OverboughtLevel` | `70` | Signalleitungspegel zur Bestätigung einer kurzen Eingabe erforderlich. |
| `ExitLowerLevel` | `20` | Untere Grenze, die für lange Ausgangs-Crossovers verwendet wird. |
| `ExitUpperLevel` | `80` | Obergrenze für kurze Ausgangskreuzungen. |

Alle numerischen Parameter unterstützen Optimierungsbereiche, die der Vorlage MetaTrader entsprechen, sodass das Verhalten über den Strategie-Designer fein abgestimmt werden kann.

## Orderverwaltung

- Die Strategie kehrt die Positionen um, wenn ein entgegengesetztes Signal auftritt, indem die absolute Größe der aktuellen Position zum konfigurierten `Volume` addiert wird.
- `StartProtection()` ist für die Integration in die Risikokontrollen der Plattform aktiviert, obwohl standardmäßig keine expliziten Stop-Loss- oder Take-Profit-Werte erzwungen werden.

## Visualisierung

Bei der Ausführung im Strategie-Designer zeichnet sich die Strategie durch Folgendes aus:

- Preiskerzen für das ausgewählte Symbol und den ausgewählten Zeitrahmen.
- Der konfigurierte Stochastic-Oszillator.
- Handelsmarkierungen zur Markierung von Ein- und Ausgängen.

## Nutzungshinweise

- Stellen Sie sicher, dass das Instrument genügend Historie liefert, damit sich der Stochastic-Oszillator aufwärmen kann, bevor Signale erwartet werden.
- Erwägen Sie, die Strategie bei der Live-Bereitstellung mit zusätzlichen Risikofiltern (Volatilität, Sitzungsfilter usw.) zu kombinieren.
- Die Schwellenwerte werden als Parameter bereitgestellt und ermöglichen ein schnelles Experimentieren mit verschiedenen Bestätigungsbändern, ohne Code bearbeiten zu müssen.
