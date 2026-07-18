# ADX Expert Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ADX Expert Strategie** ist eine direkte Konvertierung des ursprünglichen MetaTrader 4 Expert Advisors "ADX Expert" (MQL-Skript 20315). Der Expert sucht nach Kreuzungen zwischen den positiven und negativen Directional Index (DI)-Linien, während der Average Directional Index (ADX) unter einem angegebenen Schwellenwert bleibt, was darauf hinweist, dass der Markt seitwärts tendiert. Es kann jeweils nur eine Position offen sein, genau wie im Quell-Expert.

## Handelslogik
1. Die Strategie abonniert die ausgewählte Kerzenreihe (standardmäßig 15-Minuten-Kerzen) und berechnet den Average Directional Index mit dem konfigurierten Zeitraum.
2. Eine Kauforder wird platziert, wenn:
   - Die +DI-Linie über die -DI-Linie kreuzt.
   - Der ADX-Wert unter dem definierten Schwellenwert (Standard 20) bleibt und einen schwachen Trend signalisiert.
   - Der aktuelle Spread unter dem `MaxSpreadPoints`-Filter liegt.
   - Derzeit keine Position offen ist.
3. Eine Verkaufsorder wird platziert, wenn:
   - Die +DI-Linie unter die -DI-Linie kreuzt.
   - Der ADX-Wert noch unter dem zulässigen Schwellenwert liegt.
   - Die Spread-Anforderung und die Flat-Position-Bedingung erfüllt sind.
4. Schutz-Stop-Loss- und Take-Profit-Niveaus werden über `StartProtection` zugewiesen und spiegeln den festen Stop und das Ziel aus der MQL-Version wider. Sie werden in Preispunkten (Preisschritten) ausgedrückt und können durch Setzen der Werte auf null deaktiviert werden.

Die Strategie basiert auf einem Einzelpositions-Workflow: Neue Signale werden ignoriert, bis die aktuelle Position durch ihre Schutzorders geschlossen wird.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Ordergröße für jede Marktorder. | 0.1 |
| `AdxPeriod` | Periode für die ADX-Berechnung. | 14 |
| `AdxThreshold` | Maximaler ADX-Wert, der noch einen Trade erlaubt. | 20 |
| `MaxSpreadPoints` | Maximal erlaubter Spread in Preispunkten. Auf 0 setzen, um den Filter zu deaktivieren. | 20 |
| `StopLossPoints` | Stop-Loss-Abstand in Preispunkten. | 200 |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | 400 |
| `CandleType` | Kerzentyp für Indikatorberechnungen (standardmäßig 15-Minuten-Kerzen). | 15-Minuten-Zeitrahmen |

## Zusätzliche Hinweise
- Der Spread-Filter erfordert Orderbuch-Updates zum Lesen der besten Geld- und Briefkurse. Stellen Sie sicher, dass Ihr Datenanbieter diese Informationen liefert.
- Alle Kommentare und Protokolle sind aus Gründen der Klarheit auf Englisch verfasst und entsprechen den Repository-Richtlinien.
- Die Strategie ist für Bildungszwecke gedacht. Testen Sie sie gründlich in einer simulierten Umgebung, bevor Sie sie im Live-Handel einsetzen.
