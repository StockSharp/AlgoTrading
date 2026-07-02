# MA2CCI klassische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MA2CCI-Strategie portiert den klassischen MetaTrader-Expertenberater, der auf dem Zusammenspiel zweier einfacher gleitender Durchschnitte (SMA) und des Commodity Channel Index (CCI) basiert. Es filtert Trades anhand der Nulllinie CCI und wendet Schutzstopps an, die vom Average True Range (ATR) abgeleitet sind. Das System ist für trendfolgende Einstiege mit schneller Reaktion auf Umkehrungen konzipiert.

Die StockSharp-Version behält die ursprüngliche Handelslogik bei und passt das Risikomanagement an die .NET-Umgebung an. Die Positionsgröße folgt einer Risiko-pro-Tausend-Regel mit einem zusätzlichen Verringerungsfaktor, der die Handelsgröße nach aufeinanderfolgenden Verlusten reduziert. Jeder Eintrag fügt einen volatilitätsgesteuerten Stopp hinzu, der die in der MQL-Implementierung verwendete Distanz ATR widerspiegelt.

## Handelslogik

- **Indikatoren**
  - Schnell SMA mit Standardlänge 4.
  - Langsam SMA mit Standardlänge 8.
  - CCI-Filter mit 4-Perioden-Lookback.
  - ATR mit Punkt 4 für Stop-Placement.
- **Eintrittsbedingungen**
  - **Lang**: Der schnelle SMA kreuzt den langsamen SMA und der vorherige fertige Balken zeigt, dass CCI durch Null ansteigt (von negativ nach positiv).
  - **Kurz**: Der schnelle SMA kreuzt den langsamen SMA und der vorherige Balken zeigt, dass CCI durch Null fällt (von positiv nach negativ).
- **Exit Conditions**
  - Gegenüberliegender SMA-Crossover schließt offene Positionen, auch wenn kein neuer Handel initiiert wird.
  - ATR Stop: Long-Positionen werden geschlossen, wenn der Preis auf `entry - ATR` fällt; Short-Positionen werden geschlossen, wenn der Preis auf `entry + ATR` steigt.

## Risikomanagement

- Das Grundauftragsvolumen ist konfigurierbar. standardmäßig 0,1 Lots (oder Umtauschäquivalent).
- Die optionale dynamische Größenanpassung skaliert das Volumen auf `free capital * MaxRiskPerThousand / 1000`, wenn Portfoliodaten verfügbar sind.
- Nach mehr als einem aufeinanderfolgenden Verlust wird die Positionsgröße linear um `losses / DecreaseFactor` des berechneten Volumens reduziert.
- Volatilitätsstopps basieren auf der zuletzt abgeschlossenen Kerze. Intrabar-Spitzen über die Stop-Levels hinaus lösen beim nächsten Strategie-Tick einen Marktausstieg aus.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Arbeitszeitrahmen für alle Indikatoren. | 1 Stunde Kerzen |
| `OrderVolume` | Mindesthandelsgröße, wenn eine risikobasierte Größenbestimmung nicht verfügbar ist. | 0,1 |
| `FastMaPeriod` | Zeitraum des Fastens SMA. | 4 |
| `SlowMaPeriod` | Zeitraum der langsamen SMA. | 8 |
| `CciPeriod` | Zeitraum des Filters CCI. | 4 |
| `AtrPeriod` | ATR Länge für Stoppberechnung. | 4 |
| `MaxRiskPerThousand` | Anteil des pro Trade zugewiesenen freien Kapitals (pro 1000 Einheiten). | 0,02 |
| `DecreaseFactor` | Divisor wurde verwendet, um das Volumen nach Verluststrähnen zu verkleinern. | 3 |

## Notizen

1. Die Strategie verarbeitet nur fertige Kerzen und gewährleistet so eine Entscheidung pro Balken, ähnlich wie beim ursprünglichen EA, bei dem `Volume[0] > 1` als Gate verwendet wurde.
2. Stop-Levels werden intern simuliert, anstatt Börsen-Stop-Orders zu registrieren; Dies entspricht dem Verhalten der MetaTrader-Version, die sich auf Marktschließungen stützte, als die Schwellenwerte von ATR erreicht wurden.
3. Aktivieren Sie die Diagrammerstellung in StockSharp Designer, um SMA, CCI und ausgeführte Trades mithilfe der integrierten Zeichenhilfen zu visualisieren.
