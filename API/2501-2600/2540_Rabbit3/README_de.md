# Rabbit3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des ursprünglichen MetaTrader 5-Expertenberaters `Rabbit3 (barabashkakvn's Edition)`.
- Implementiert die Logik in der High-Level-API von StockSharp mit Kerzenabonnements und Indikatorbindungen.
- Konzentriert sich auf eine doppelte Bestätigung zwischen Williams %R und dem Commodity Channel Index (CCI), bevor Positionen gestapelt werden.
- Fügt dynamische Positionsgrößenbestimmung hinzu: Gewinne über einem Bargeld-Schwellenwert erhöhen das Ordervolumen für das nächste Signal.

## Handelslogik
### Einstiegsbedingungen
1. **Long**
   - Aktuelle und vorherige abgeschlossene Kerzen melden Williams %R unterhalb von `WilliamsOversold` (Standard `-80`).
   - CCI-Wert liegt unter `CciBuyLevel` (Standard `-80`).
   - Die aktuelle Nettoposition ist nicht negativ und das Hinzufügen einer weiteren Position hält das Exposure innerhalb von `MaxPositions` × `BaseVolume` (erhöhtes Volumen wird verwendet, wenn aktiv).
2. **Short**
   - Aktuelle und vorherige abgeschlossene Kerzen melden Williams %R oberhalb von `WilliamsOverbought` (Standard `-20`).
   - CCI-Wert liegt über `CciSellLevel` (Standard `80`).
   - Die aktuelle Nettoposition ist nicht positiv und die neue Order bleibt innerhalb des konfigurierten Stapellimits.

### Ausstieg und Risikokontrolle
- Schutz-Stop-Loss- und Take-Profit-Orders werden automatisch über `StartProtection` registriert.
- Die Abstände werden in „angepassten Punkten" ausgedrückt: Wenn das Instrument 3 oder 5 Dezimalstellen verwendet, multipliziert die Strategie den Kursschritt mit 10, um die MetaTrader-Pip-Arithmetik zu emulieren, bevor `StopLossPips` und `TakeProfitPips` angewendet werden.
- Keine zusätzlichen manuellen Ausstiegsregeln erforderlich; Schutzorders schließen die Trades.

### Positionsgrößenbestimmung
- `BaseVolume` definiert die anfängliche Handelsgröße (Standard `0.01`).
- Nach jedem Schließen eines Trades wird das realisierte PnL-Delta mit `ProfitThreshold` (Standard `4` Geldeinheiten) verglichen.
- Wenn das Delta strikt größer ist, verwendet das nächste Signal `BaseVolume × VolumeMultiplier` (Standard `1.6`). Andernfalls wird die Größe auf `BaseVolume` zurückgesetzt.
- Das aktuelle Volumen wird auch über die `Volume`-Eigenschaft der Strategie für UI-Feedback freigegeben.

### Indikatoren und Visualisierung
- Williams %R, CCI, eine schnelle EMA (`FastEmaPeriod`) und eine langsame EMA (`SlowEmaPeriod`) sind an den Kerzenfeed für Überwachung und Charting gebunden.
- Ein Diagrammbereich wird automatisch erstellt, der Kerzen, Indikatoren und ausgeführte Trades darstellt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | `1h`-Zeitrahmen | Datentyp für das Kerzenabonnement. |
| `CciPeriod` | `15` | Länge des Commodity Channel Index. |
| `CciBuyLevel` | `-80` | CCI-Schwellenwert, der Long-Einstiege erlaubt. |
| `CciSellLevel` | `80` | CCI-Schwellenwert, der Short-Einstiege erlaubt. |
| `WilliamsPeriod` | `62` | Lookback-Periode für Williams %R. |
| `WilliamsOversold` | `-80` | Überverkauft-Schwellenwert für Long-Bestätigung. |
| `WilliamsOverbought` | `-20` | Überkauft-Schwellenwert für Short-Bestätigung. |
| `FastEmaPeriod` | `17` | Schnelle EMA für Trendkontext. |
| `SlowEmaPeriod` | `30` | Langsame EMA für Trendkontext. |
| `MaxPositions` | `2` | Maximale Anzahl gestapelter Einstiege pro Richtung. |
| `ProfitThreshold` | `4` | Realisierter Gewinn zum Boosten der nächsten Ordergröße (Geldeinheiten). |
| `BaseVolume` | `0.01` | Basis-Ordervolumen. |
| `VolumeMultiplier` | `1.6` | Multiplikator bei erfüllter Boost-Bedingung. |
| `StopLossPips` | `45` | Stop-Loss-Abstand in angepassten Punkten. |
| `TakeProfitPips` | `110` | Take-Profit-Abstand in angepassten Punkten. |

## Hinweise
- Die Strategie operiert auf Nettopositionen. Im Gegensatz zur hedging-freundlichen MQL-Version werden Longs und Shorts nicht gleichzeitig gehalten; Signale in entgegengesetzter Richtung werden ignoriert, bis das aktuelle Exposure durch Schutzorders geschlossen wird.
- `MaxPositions` wirkt als Obergrenze für die aggregierte Position (Basisvolumen multipliziert mit dem Stapelfaktor). Passen Sie es sorgfältig an, wenn Sie die Basis- oder Boost-Volumina ändern.
- Die Volumentoleranz verwendet die Hälfte des Instrument-Volumenschritts, um geringfügige Rundungsunterschiede bei der Prüfung des Stapel-Limits zu absorbieren.
- Eine Python-Übersetzung ist noch nicht enthalten und kann bei Bedarf später hinzugefügt werden.
