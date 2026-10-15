# BrainTrend2 V2 Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die BrainTrend2 V2 Duplex-Strategie ist ein High-Level-StockSharp-Port des ursprünglichen MetaTrader 5-Experten `Exp_BrainTrend2_V2_Duplex`. Sie führt zwei unabhängige Instanzen des BrainTrend2 V2-Indikators aus: eine für Long-Chancen und eine für Short-Chancen. Jede Seite kann auf ihrer eigenen Kerzenserie, ATR-Länge und Signalversatz arbeiten, was es der Strategie ermöglicht, Multi-Zeitrahmen-Bestätigungen oder asymmetrische Risikoeinstellungen zu kombinieren.

BrainTrend2 V2 ist ein Trendfolge-Engine, der einen dynamischen "Fluss"-Kanal aufbaut, indem die jüngste wahre Spanne mit einem gewichteten ATR-Durchschnitt verglichen wird. Der Indikator malt Kerzen mit fünf verschiedenen Farben:

- **0** – Bullische Kerze in einem aufsteigenden Trend-Fluss.
- **1** – Bärische Kerze in einem aufsteigenden Trend-Fluss.
- **2** – Neutraler Platzhalter, während der Fluss die Richtung wechselt.
- **3** – Bullische Kerze in einem absteigenden Trend-Fluss.
- **4** – Bärische Kerze in einem absteigenden Trend-Fluss.

Die Duplex-Strategie interpretiert diese Farbübergänge, um Einstiege und Ausstiege auszulösen, und spiegelt dabei eng die im MQL5-Experten fest codierten Regeln wider.

## Handelslogik
### Long-Seite
- Den Indikator auf der Kerze `Long Signal Bar` Schritte zurück auswerten (Standard 1 = der vorherige abgeschlossene Balken).
- Eine Long-Position eröffnen, wenn:
  - Die Farbe auf Balken `SignalBar + 1` (zwei Balken zurück) **kleiner als 2** war (grüne Töne eines aufsteigenden Trend-Flusses), **und**
  - Die Farbe auf Balken `SignalBar` **größer als 1** ist (Übergang weg vom reinen bullischen Zustand).
- Eine bestehende Long-Position schließen, wenn die Farbe auf Balken `SignalBar + 1` **größer als 2** ist (Magenta-Töne des absteigenden Trend-Flusses).

### Short-Seite
- Den Indikator auf der Kerze `Short Signal Bar` Schritte zurück auswerten (Standard 1).
- Eine Short-Position eröffnen, wenn:
  - Die Farbe auf Balken `SignalBar + 1` **größer als 2** war (Magenta-Töne), **und**
  - Die Farbe auf Balken `SignalBar` **größer als 0** ist (alles außer einer reinen bullischen Kerze).
- Eine bestehende Short-Position schließen, wenn die Farbe auf Balken `SignalBar + 1` **kleiner als 2** ist (Rückkehr zum aufsteigenden Trend-Fluss).

Wenn eine neue Order erteilt wird, kompensiert die Strategie automatisch jede entgegengesetzte Exposition. Zum Beispiel wird eine Short-Einsstiegsanforderung zuerst die aktuelle Long-Position (falls vorhanden) zurückkaufen und dann die Verkaufsorder für das konfigurierte Short-Volumen senden.

## Risikomanagement
- Beide Seiten können unabhängige Stop-Loss- und Take-Profit-Abstände in Punkten angeben. Ein Wert von `0` deaktiviert das jeweilige Bracket.
- Stops und Ziele werden in absoluten Preisen unter Verwendung des Sicherheitspreisschritts berechnet. Longs überwachen das Kerzentief/-hoch, Shorts überwachen das Kerzenhoch/-tief zur Emulation der intrabar-Ausführung.
- Die Positionsgröße wird direkt in Handelseinheiten ausgedrückt und kann zwischen Long- und Short-Streams variieren.
- Die Strategie aktiviert auch `StartProtection()`, um sich mit allen auf Portfolioebene in StockSharp konfigurierten Sicherheitsmechanismen zu integrieren.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|---------|
| `Long Candle Type` | Kerzendatentyp für den Long-Indikator (Zeitrahmen). | H4-Zeitrahmen |
| `Long ATR Period` | ATR-Rückblick im BrainTrend2 V2-Berechnung für den Long-Stream. | 7 |
| `Long Signal Bar` | Historischer Versatz (in Balken) für Long-Entscheidungen. | 1 |
| `Enable Long Entries` | Erlaubt oder blockiert neue Long-Orders. | true |
| `Enable Long Exits` | Erlaubt oder blockiert indikatorbasierte Long-Ausstiege. | true |
| `Long Volume` | Basis-Ordergröße für Long-Einstiege. | 1 |
| `Long Stop Loss` | Stop-Loss-Abstand in Punkten für Long-Trades (0 = deaktiviert). | 1000 |
| `Long Take Profit` | Take-Profit-Abstand in Punkten für Long-Trades (0 = deaktiviert). | 2000 |
| `Short Candle Type` | Kerzendatentyp für den Short-Indikator. | H4-Zeitrahmen |
| `Short ATR Period` | ATR-Rückblick im BrainTrend2 V2-Berechnung für den Short-Stream. | 7 |
| `Short Signal Bar` | Historischer Versatz (in Balken) für Short-Entscheidungen. | 1 |
| `Enable Short Entries` | Erlaubt oder blockiert neue Short-Orders. | true |
| `Enable Short Exits` | Erlaubt oder blockiert indikatorbasierte Short-Ausstiege. | true |
| `Short Volume` | Basis-Ordergröße für Short-Einstiege. | 1 |
| `Short Stop Loss` | Stop-Loss-Abstand in Punkten für Short-Trades (0 = deaktiviert). | 1000 |
| `Short Take Profit` | Take-Profit-Abstand in Punkten für Short-Trades (0 = deaktiviert). | 2000 |

## Verwendungshinweise
- Größere Signalversätze verwenden, um auf zusätzliche Kerzenbestätigung zu warten, oder verschiedene Zeitrahmen kombinieren, indem verschiedene Kerzentypen Long- und Short-Streams zugewiesen werden.
- Da die Strategie eine benutzerdefinierte BrainTrend2-Implementierung verwendet, hängt sie von keinen externen Indikatordateien ab; alles ist in der C#-Klasse enthalten.
- Stops und Ziele werden bei jeder abgeschlossenen Kerze verwaltet. Bei Live-Daten sollte ein ausreichend kleines Kerzenintervall gewählt werden, wenn eine engere Risikosteuerung erforderlich ist.
- Das Setzen von sowohl Stop- als auch Take-Profit-Abstände auf null hält Positionen offen, bis ein entgegengesetzter Farb-Trigger erscheint.
- Der Indikatorbuffer wird initialisiert, sobald genug Kerzen gleich dem ATR-Zeitraum verarbeitet wurden. Bis zu diesem Zeitpunkt werden keine Handelsentscheidungen getroffen.
