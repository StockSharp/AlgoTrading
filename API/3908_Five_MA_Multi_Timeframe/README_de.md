# Fünf MA Multi-Timeframe-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Five MA Multi-Timeframe Strategy** repliziert den ursprünglichen MT4-Expertenberater „5matf“ unter Verwendung des High-Level-API von StockSharp. Die Strategie analysiert fünf einfache gleitende Durchschnitte über drei Zeitrahmen (primärer, höherer und langsamster) und kombiniert die Steigung jedes Durchschnitts mit dem Accelerator-Oszillator, um abgestufte Einstiegssignale zu erzeugen. Wenn in allen Zeitrahmen genügend bullische oder bärische Anzeichen vorliegen, eröffnet oder schließt die Strategie entsprechend Positionen.

## Indikatoren und Daten
- **Einfache gleitende Durchschnitte (SMA)**: Perioden 5, 8, 13, 21 und 34 in allen drei Zeitrahmen.
- **Beschleunigeroszillator (AC)**: Wird auf den primären und tertiären Zeitrahmen angewendet, um die Impulsbeschleunigung zu bewerten.
- **Zeitrahmen**: Standardmäßig auf 15 Minuten (Signal), 60 Minuten (Bestätigung) und 240 Minuten (Trendfilter) eingestellt. Alle Zeitrahmen können über Parameter angepasst werden.

## Signallogik
1. Jede SMA vergleicht ihren aktuellen Wert mit der vorherigen Kerze, um einen Aufwärts- oder Abwärtstrend zu bestimmen.
2. Der Accelerator-Oszillator prüft anhand der letzten vier Werte, ob bullische oder bärische Sequenzen vorliegen.
3. Steigungszahlen und Oszillatorbeiträge werden für jeden Zeitrahmen in Prozentwerten zusammengefasst.
4. Wenn alle drei Zeitrahmen bullische Werte über 50 % aufweisen, wird ein **KAUFEN**-Signal generiert. Werte über 75 % verstärken das Signal.
5. Die gleichen Schwellenwerte, die in die entgegengesetzte Richtung angewendet werden, erzeugen **VERKAUFS-Signale**.
6. Positionen werden geschlossen, wenn ein Gegensignal den konfigurierten Schließpegel überschreitet. Neue Geschäfte werden nur eröffnet, wenn keine Position aktiv ist, was das ursprüngliche Verhalten des Expertenberaters widerspiegelt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 15-Minuten-Kerzen | Primärer Zeitrahmen für Handelssignale. |
| `HigherTimeframe1` | 60-Minuten-Kerzen | Erster höherer Zeitrahmen für die Bestätigung. |
| `HigherTimeframe2` | 240-Minuten-Kerzen | Zweiter höherer Zeitrahmen für langsamen Trendfilter. |
| `FirstPeriod` – `FifthPeriod` | 5, 8, 13, 21, 34 | Auf jeden Zeitrahmen werden SMA Längen angewendet. |
| `OpenLevel` | 0 | Mindestsignalgrad erforderlich, um eine neue Position zu eröffnen. |
| `CloseLevel` | 1 | Gegensignalgrad erforderlich, um eine bestehende Position zu schließen. |

Alle Parameter können in der Strategie-Benutzeroberfläche von StockSharp optimiert oder verfeinert werden.

## Nutzungshinweise
- Die Strategie verwendet Marktaufträge und gibt keine gleichzeitigen Umkehrungen aus; Es wartet immer auf eine flache Position, bevor es in die entgegengesetzte Richtung öffnet.
- Aktivieren Sie Verlaufsdaten-Feeds für alle ausgewählten Zeitrahmen, um synchronisierte Berechnungen sicherzustellen.
- Erwägen Sie eine Optimierung der SMA-Längen oder der Oszillatornutzung, wenn Sie die Strategie auf verschiedene Märkte oder Volatilitätsregime anwenden.

## Konvertierungshinweise
Diese Implementierung behält das Kernverhalten des MT4-Expertenberaters „5matf“ bei und nutzt gleichzeitig das Abonnement- und Indikatorbindungssystem von StockSharp. Die Beschleunigerlogik erfordert vier abgeschlossene Kerzen, bevor Signale aktiv werden, genau wie das ursprüngliche Skript.
