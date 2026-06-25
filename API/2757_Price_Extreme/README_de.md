# Price-Extreme-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Price-Extreme-Strategie** repliziert den MetaTrader-Expertenberater `Price_Extreme_Strategy` mithilfe der High-Level-API von StockSharp. Das System überwacht einen gleitenden Kanal, der aus dem höchsten Hoch und dem niedrigsten Tief über eine konfigurierbare Anzahl abgeschlossener Kerzen abgeleitet wird. Ausbruchssignale werden erzeugt, wenn die ausgewählte Referenzkerze oberhalb der oberen Grenze oder unterhalb der unteren Grenze schließt. Die Logik kann optional invertiert werden, um Ausbruchsbedingungen in Gegentrendeinträge umzuwandeln.

Diese Umsetzung hält den Trading-Workflow ereignisgesteuert. Aufträge werden unmittelbar nach dem Schließen jeder abgeschlossenen Kerze eingereicht, was dem Verhalten des ursprünglichen MQL-Algorithmus entspricht, der auf dem Eröffnungstick der nächsten Bar reagierte.

## Indikatorlogik

Der Price-Extreme-Kanal wird bei jeder abgeschlossenen Kerze mithilfe der StockSharp-Indikatoren `Highest` und `Lowest` neu berechnet:

- `Highest` verfolgt das maximale Hoch über die letzten *N* Kerzen.
- `Lowest` verfolgt das minimale Tief über die letzten *N* Kerzen.

Diese Puffer emulieren die benutzerdefinierte Studie `Price_Extreme_Indicator`, die mit dem ursprünglichen Expertenberater gebündelt ist. Die Indikatorlänge wird über den Parameter **Level Length** zugänglich gemacht.

Ein separater Parameter **Signal Shift** legt fest, welche geschlossene Kerze zur Auswertung der Ausbruchsbedingung verwendet wird. Ein Shift von `1` bedeutet „die gerade geschlossene Kerze verwenden" (Standard). Größere Werte ermöglichen das Warten auf zusätzliche Bestätigung durch Referenzierung älterer Bars.

## Handelsregeln

1. Obere und untere Kanalwerte für jede abgeschlossene Kerze neu berechnen.
2. Die durch **Signal Shift** angegebene Kerze aus dem internen Verlaufspuffer abrufen.
3. Richtungsabsichten generieren:
   - **Ausbruch nach oben**: der Schlusskurs der Kerze liegt über dem oberen Kanalwert.
   - **Ausbruch nach unten**: der Schlusskurs der Kerze liegt unter dem unteren Kanalwert.
4. Optionale Invertierung mit **Reverse Signals** anwenden:
   - Wenn deaktiviert, in Ausbruchsrichtung handeln (kaufen bei Ausbruch nach oben, verkaufen bei Ausbruch nach unten).
   - Wenn aktiviert, die Reaktionen tauschen (verkaufen bei Ausbruch nach oben, kaufen bei Ausbruch nach unten).
5. **Enable Long**- und **Enable Short**-Berechtigungen vor der Auftragsübermittlung respektieren.
6. Automatisch jede entgegengesetzte Position schließen, bevor ein neuer Trade eröffnet wird, sodass zu jedem Zeitpunkt nur eine Nettoposition existiert.

## Risikomanagement

Die Strategie bietet Stop-Loss- und Take-Profit-Handling, das die punktbasierten Kontrollen der MQL-Version widerspiegelt:

- **Stop Loss** und **Take Profit** werden in Preisschritten (`Security.PriceStep`) ausgedrückt.
- Zielpreise werden neu berechnet, wenn sich die Nettopositionsgröße ändert.
- Wenn eine abgeschlossene Kerze die Schutzlevels überschreitet (Tief unter dem Stop für Longs, Hoch über dem Stop für Shorts usw.), wird die Position per Marktauftrag geschlossen und die Schutzziele werden gelöscht.
- `StartProtection()` wird während `OnStarted` aktiviert, um die integrierten StockSharp-Sicherheitsmechanismen zu nutzen.

## Parameter

| Parameter | Beschreibung | Standard | Gruppe |
|-----------|--------------|----------|--------|
| `LevelLength` | Anzahl der abgeschlossenen Kerzen, die bei der Berechnung des Extremkanals berücksichtigt werden. | 5 | Indicator |
| `SignalShift` | Index der geschlossenen Kerze für die Ausbruchsvalidierung (1 = letzte geschlossene Kerze). | 1 | Indicator |
| `EnableLong` | Erlaubt Käufe wenn `true`. | `true` | Trading |
| `EnableShort` | Erlaubt Verkäufe wenn `true`. | `true` | Trading |
| `ReverseSignals` | Invertiert Ausbruchsreaktionen (kaufen bei Kursrückgang, verkaufen bei Ausbruch). | `false` | Trading |
| `OrderVolume` | Volumen, das mit jedem Marktauftrag gesendet wird. Muss größer als null sein. | 1 | Trading |
| `StopLossPoints` | Stop-Loss-Abstand gemessen in Preisschritten. Ein Wert von `0` deaktiviert den Stop. | 0 | Risk |
| `TakeProfitPoints` | Take-Profit-Abstand gemessen in Preisschritten. Ein Wert von `0` deaktiviert das Ziel. | 0 | Risk |
| `CandleType` | Primärer Zeitrahmen für das Datenabonnement. | 5-Minuten-Kerzen | Data |

Alle Parameter verwenden `StrategyParam<T>` mit UI-Metadaten, damit sie im Designer optimiert oder geändert werden können.

## Verwendungshinweise

1. Die Strategie einem Instrument zuweisen und den **Candle Type** passend zum Zeitrahmen der ursprünglichen MetaTrader-Einrichtung setzen.
2. **Level Length** anpassen, wenn ein breiterer oder engerer Price-Extreme-Kanal gewünscht ist.
3. **Signal Shift** konfigurieren, um zu steuern, wie viele geschlossene Kerzen vor der Ausbruchsauswertung gewartet werden soll.
4. Gewünschte Handelsrichtungen über **Enable Long**, **Enable Short** und **Reverse Signals** auswählen.
5. **Order Volume**, **Stop Loss** und **Take Profit** entsprechend den Risikopräferenzen festlegen. Beachten Sie, dass beide Schutzwerte in Preisschritten arbeiten.
6. Strategie starten. Kerzen, Indikatorbänder und ausgeführte Trades werden automatisch gezeichnet, wenn ein Diagrammbereich verfügbar ist.

## Zusätzliche Hinweise

- Die Strategie arbeitet absichtlich auf einer einzigen Nettoposition und spiegelt die Hedging-Logik des MQL-Experten wider, indem die Gegenseite vor dem Einstieg in einen neuen Trade abgeflacht wird.
- Schutz-Stops und -Ziele werden auf abgeschlossenen Kerzen ausgewertet. Im Live-Trading approximiert dies die serverseitigen Schutzaufträge des Originalskripts.
- Kein Python-Port enthalten, wie gewünscht.
