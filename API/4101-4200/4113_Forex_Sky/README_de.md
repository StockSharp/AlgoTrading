# Forex Sky-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Forex Sky Strategy** ist eine direkte Portierung des MetaTrader-Expertenberaters `Forex_SKY.mq4`. Es handelt mit MACD Momentumschwankungen und beschränkt sich strikt auf eine einzige Position pro Handelstag. Die StockSharp-Implementierung behält die ursprünglichen MACD-Schwellenwerte und die Sicherheitsprüfung bei, die mehr als eine Bestellung pro Kerze verhindert.

Die Strategie abonniert den durch `CandleType` definierten Zeitrahmen (standardmäßig 15-Minuten-Kerzen) und wertet den klassischen MACD (26.12.9) am Ende jeder abgeschlossenen Kerze aus.

## Handelslogik
- **Langer Einstieg** – Platzieren Sie einen Marktkauf, wenn:
  - Die aktuelle MACD-Hauptzeile liegt über Null;
  - Es überschreitet auch `+0.00009`, um die Dynamik zu bestätigen;
  - Mindestens einer der vorherigen drei MACD-Werte war kleiner oder gleich Null (was einen Aufwärtstrend aus dem negativen Bereich darstellt).
- **Short-Einstieg** – Platzieren Sie einen Marktverkauf, wenn eine der folgenden Bedingungen zutrifft:
  - Die Hauptlinie MACD liegt unter Null, fällt unter `-0.0004`, mindestens einer der letzten drei Messwerte war nicht negativ und der Wert vor vier Balken betrug mindestens `+0.001`.
  - **Oder** der Wert von vor vier Balken war `≥ +0.003`, was einen Short-Trade sofort autorisiert, genau wie im ursprünglichen MetaTrader-Code.
- **Positionsverwaltung** – Der Algorithmus öffnet nie mehr als eine Order pro Kerze (`Time0` Guard) und handelt nie mehr als einmal pro Kalendertag (`CheckTodaysOrders` Guard). Schutzausstiegsbefehle werden vom Helfer StockSharp `StartProtection` verarbeitet, sodass alle Stopps und Ziele mit dem aktuellen Volumen synchronisiert bleiben.

Über die Schutzanordnungen hinaus gibt es keine autonome Flatting-Logik – es wird erwartet, dass Positionen durch Take-Profit, Stop-Loss oder manuelle Intervention geschlossen werden, was das Verhalten des ursprünglichen Expertenberaters widerspiegelt.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `FastPeriod` | 12 | Schnelle EMA-Länge des MACD-Indikators. |
| `SlowPeriod` | 26 | Langsame EMA-Länge des MACD-Indikators. |
| `SignalPeriod` | 9 | Signallänge EMA des Indikators MACD. |
| `TakeProfitPoints` | 100 | Abstand zur Take-Profit-Order, ausgedrückt in Instrumentenpunkten. Durch Multiplikation mit der Wertpapierpreisstufe in einen Preis umgerechnet. |
| `StopLossPoints` | 3000 | Abstand zur Stop-Loss-Order in Instrumentenpunkten. |
| `TradeVolume` | 0,1 | Basis-Market-Order-Größe (Lots). |
| `CandleType` | 15-minütiger Zeitrahmen | Zeitrahmen, der in die Berechnungen und Handelsentscheidungen von MACD einfließt. |

### Berechnung des Instrumentenpunkts
`TakeProfitPoints` und `StopLossPoints` werden genau wie die Version MetaTrader angegeben – `Point` in MQL4 entspricht `Security.PriceStep` in StockSharp. Für ein fünfstelliges Forex-Paar (`PriceStep = 0.00001`) lauten die Standardeinstellungen wie folgt:
- Take-Profit: `100 × 0.00001 = 0.001` Preiseinheiten.
- Stop-Loss: `3000 × 0.00001 = 0.03` Preiseinheiten.

## Risikomanagement
`StartProtection` installiert automatisch die Take-Profit- und Stop-Loss-Orders, nachdem ein Eintrag ausgeführt wurde. Sie sind mit der Handelsrichtung verknüpft und verwenden bei Auslösung Marktaufträge, die dem MetaTrader-Verhalten entsprechen. Setzen Sie einen der Parameter auf `0`, um die entsprechende Schutzanordnung zu deaktivieren.

## Migrationshinweise
- Der Verlaufspuffer MACD speichert die letzten vier abgeschlossenen Werte in Klassenfeldern, sodass keine Indikatoraufrufe mit verschobenen Indizes erforderlich sind.
- Die tägliche Handelsdrosselung und die Beschränkung auf einen einzelnen Handel pro Balken replizieren `CheckTodaysOrders()` und `Time0` aus der Originalquelle.
- Alle Kommentare wurden in Englisch umgeschrieben und die Logik basiert auf StockSharp High-Level-Bindungen (`Bind`) für die Indikatorverarbeitung.

## Nutzungstipps
- Passen Sie `CandleType` an den Diagrammzeitraum an, den Sie emulieren möchten. Das ursprüngliche Skript übernimmt den Zeitrahmen des Diagramms automatisch.
- Da pro Tag nur ein Handel zulässig ist, wählen Sie Märkte mit bedeutenden Intraday-Schwankungen aus oder erwägen Sie eine Anhebung der MACD-Schwellenwerte, wenn Sie Instrumente mit höherer Volatilität verwenden.
- Überwachen Sie die Uhr/Zeitzone der Plattform, um sicherzustellen, dass die Tagesgrenze mit Ihrer Handelssitzung übereinstimmt, da der Limitzähler basierend auf dem Öffnungsdatum der Kerze zurückgesetzt wird.
