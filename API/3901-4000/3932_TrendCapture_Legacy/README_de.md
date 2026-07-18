# Trend Capture Legacy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

**Trend Capture Legacy Strategy** portiert den MetaTrader-Experten `TrendCapture.mq4` zum hochrangigen StockSharp API. Die C#-Version behält den ursprünglichen Regelsatz bei, der auf der Parabolic-SAR-Richtung, einem Filter mit niedrigem ADX-Wert und einer einfachen Break-Even-Geldverwaltung basiert.

## Kernideen
- Verarbeiten Sie fertige Kerzen des ausgewählten Zeitrahmens und geben Sie sie an Parabolic SAR (`0.02/0.2`) und den Average Directional Index (`14`) weiter.
- Steigen Sie nur ein, wenn ADX unter `AdxThreshold` liegt, was auf einen ruhigen Markt hinweist, in dem Umkehrungen von SAR zuverlässiger sind.
- Merken Sie sich die Richtung und das Ergebnis des letzten geschlossenen Trades: Wiederholen Sie die gleiche Seite nach einem Gewinner, wechseln Sie zur gegenüberliegenden Seite nach einem Verlierer.
- Wenden Sie Stop-Loss- und Take-Profit-Level mit festem Abstand an (konfiguriert in Preispunkten) und verschieben Sie den Stop auf die Gewinnschwelle, sobald der Trade `BreakEvenGuard` Punkte gewinnt.
- Bestimmen Sie das Auftragsvolumen anhand des verfügbaren Portfoliowerts und `MaximumRisk`; auf die Strategie `Volume` zurückgreifen, wenn keine Portfolioinformationen verfügbar sind.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `SarStep` | 0,02 | Anfänglicher Parabolic SAR Beschleunigungsschritt. |
| `SarMax` | 0,2 | Maximale Parabolic SAR Beschleunigung. |
| `AdxPeriod` | 14 | ADX Mittelungszeitraum. |
| `AdxThreshold` | 20 | Maximaler ADX-Wert, der noch einen neuen Eintrag zulässt. |
| `TakeProfitPoints` | 180 | Take-Profit-Distanz in Preispunkten. |
| `StopLossPoints` | 50 | Stop-Loss-Distanz in Preispunkten. |
| `BreakEvenGuard` | 5 | Erforderlicher Gewinnpuffer (in Punkten), bevor der Stopp auf den Einstieg verschoben wird. |
| `MaximumRisk` | 0,03 | Bruchteil des freien Spielraums, der für die Positionsgröße verwendet wird. |
| `CandleType` | 1 Stunde Kerzen | Zeitrahmen für Indikatorberechnungen und Handelssignale. |

## Auftragsverwaltung
- Long-Einträge erfordern einen Schlusskurs über SAR mit einem Tiefstpreis von ADX; Für Shorts ist der Schlusskurs unter SAR mit demselben ADX-Filter erforderlich.
- Stop-Loss- und Take-Profit-Level werden bei jedem Einstieg neu berechnet und bei jeder abgeschlossenen Kerze ausgewertet.
- Die Break-Even-Aktivierung verschiebt einfach den Stop auf den Einstiegspreis. Wenn kein Stop-Loss konfiguriert ist (Null oder negativer Abstand), wird der Guard ignoriert.

## Indikatoren
- `ParabolicSar` für Richtungsvoreingenommenheit.
- `AverageDirectionalIndex` für den Stärkefilter (nur die Hauptzeile ADX wird verwendet).

## Notizen
- Die Strategie verwendet `BindEx`, um den direkten Pufferzugriff gemäß den Projektrichtlinien zu vermeiden.
- Die Portfolio-basierte Volumenberechnung berücksichtigt die Board-Einschränkungen (`LotStep`, `MinVolume`, `MaxVolume`).
- Der für die Richtungsverzerrung erforderliche Handelsverlauf wird über `OnNewMyTrade` erfasst, sodass Teilfüllungen weiterhin unterstützt werden.
