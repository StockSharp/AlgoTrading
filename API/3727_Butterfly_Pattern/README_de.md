# Schmetterlingsmuster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Butterfly-Pattern-Strategie** wandelt die ursprüngliche harmonische Musterlogik von MetaTrader „Cypher EA“ in die hohe Ebene API von StockSharp um. Die Strategie durchsucht eine konfigurierbare Kerzenreihe nach bullischen und bärischen Schmetterlingsformationen, validiert die harmonischen Verhältnisse und eröffnet Marktpositionen mit drei abgestuften Take-Profit-Zielen. Optionale Risikomanagementfunktionen spiegeln den MetaTrader-Experten wider: Break-Even-Sperre und Trailing-Stop-Updates sind nach Teilausstiegen verfügbar.

## Wie es funktioniert

1. Kerzen werden gepuffert, bis ein Pivotpunkt mithilfe des Fensters `PivotLeft`/`PivotRight` bestätigt werden kann.
2. Wenn fünf alternierende Pivots verfügbar sind, prüft die Strategie die Fibonacci-Verhältnisse, die für ein Schmetterlingsmuster erforderlich sind.
3. Qualifizierte Setups werden erneut validiert (optional) und anhand eines harmonischen Qualitätsfaktors (`MinPatternQuality`) bewertet.
4. Sobald ein Muster bei einer geschlossenen Kerze bestätigt wird:
   - Eine Marktorder wird entweder mit festem Volumen oder mit risikobasierter Größenbestimmung platziert.
   - Das Positionsvolumen ist auf drei Take-Profit-Level (`TP1/TP2/TP3`) aufgeteilt.
   - Aus der Musterstruktur wird ein geometrischer Stop-Loss abgeleitet.
5. Während der Lebensdauer der Position überwacht die Strategie Kerzen, um Teilausstiege, Break-even-Sperrungen und nachlaufende Anpassungen gemäß den konfigurierten Schwellenwerten auszulösen.

> **Tipp:** Die MetaTrader-Version funktioniert mit mehreren Zeitrahmen gleichzeitig. Um dieses Verhalten in StockSharp zu reproduzieren, starten Sie mehrere Instanzen der Strategie mit unterschiedlichen `CandleType`-Werten.

## Schlüsselparameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen zur Erkennung von Pivots und Mustern. |
| `PivotLeft` / `PivotRight` | Anzahl der Kerzen links/rechts, die erforderlich sind, um ein Pivot-Hoch/Tief zu bestätigen. |
| `Tolerance` | Maximal zulässige Abweichung des harmonischen Verhältnisses bei der Validierung des Schmetterlingsmusters. |
| `AllowTrading` | Aktiviert oder deaktiviert die Auftragsgenerierung nach einer Musterbestätigung. |
| `UseFixedVolume` / `FixedVolume` | Erzwingt ein konstantes Handelsvolumen. Wenn die Strategie deaktiviert ist, werden die Positionen über `RiskPercent` dimensioniert. |
| `RiskPercent` | Prozentsatz des pro Trade riskierten Portfoliowerts (wird nur verwendet, wenn `UseFixedVolume` falsch ist). |
| `AdjustLotsForTakeProfits` | Normalisiert die Teilvolumina, um sicherzustellen, dass die Summe der Eintragsgröße entspricht. |
| `Tp1Percent` / `Tp2Percent` / `Tp3Percent` | Verteilung des Gesamtvolumens auf die drei Take-Profit-Ebenen. |
| `MinPatternQuality` | Mindestharmonische Punktzahl (0–1), die erforderlich ist, um ein erkanntes Muster zu akzeptieren. |
| `UseSessionFilter`, `SessionStartHour`, `SessionEndHour` | Beschränken Sie den Handel auf ein bestimmtes Börsensitzungsfenster. |
| `RevalidatePattern` | Erzwingt eine zweite Preisprüfung vor der Eröffnung einer Position. |
| `UseBreakEven`, `BreakEvenAfterTp`, `BreakEvenTrigger`, `BreakEvenProfit` | Steuert die Break-Even-Aktivierung nach dem angegebenen Take-Profit-Level und dem zusätzlichen Gewinnpuffer. |
| `UseTrailingStop`, `TrailAfterTp`, `TrailStart`, `TrailStep` | Ermöglicht Trailing Stops, sobald ein Take-Profit-Level erreicht wurde und die minimale günstige Abweichung erreicht ist. |

## Risikomanagement

- Stop-Loss-, Break-Even- und Trailing-Levels werden intern verwaltet, ohne dass zusätzliche Aufträge erstellt werden. Teilausstiege und Stop-Closings werden mit Marktaufträgen ausgelöst, um die MetaTrader-Logik zu emulieren.
- Wenn `UseFixedVolume` deaktiviert ist, wird die Positionsgröße aus der Stoppdistanz, dem Tick-Wert des Instruments und der Einstellung `RiskPercent` berechnet.

## Nutzungshinweise

- Stellen Sie sicher, dass das angeschlossene Instrument die konfigurierte `CandleType`- und Preisstufe unterstützt, andernfalls kann die Validierungslogik Signale aufgrund von Mindestabstandsprüfungen zurückweisen.
- Für Break-Even- und Trailing-Funktionen müssen die jeweiligen Take-Profit-Levels gefüllt sein (`BreakEvenAfterTp` und `TrailAfterTp`).
- Mehrere Strategieinstanzen können gleichzeitig auf verschiedenen Wertpapieren oder Zeitrahmen ausgeführt werden, um das Scannen mehrerer Zeitrahmen des ursprünglichen EA zu reproduzieren.
