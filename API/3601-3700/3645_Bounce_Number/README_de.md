# Bounce-Nummern-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Bounce Number Strategy** ist ein StockSharp-Port des MetaTrader-Indikators `BounceNumber_V0.mq4` / `BounceNumber_V1.mq4`. Das ursprüngliche Tool war ein visueller Analysator, der zählte, wie oft der Preis einen symmetrischen Kanal berührte, bevor er aus ihm ausbrach. Diese C#-Strategie erstellt den Absprungzähler mit dem übergeordneten API neu, speichert die Ergebnisse in einer Verteilungstabelle und meldet jeden abgeschlossenen Zyklus über das Strategieprotokoll. Die Implementierung bleibt der Logik von MetaTrader treu und passt sie gleichzeitig an die ereignisgesteuerte Pipeline von StockSharp an.

Im Gegensatz zum ursprünglichen Indikator läuft der Port als Strategiekomponente. Es abonniert fertige Kerzen, überwacht Bandberührungen und verfolgt, wie viele abwechselnde Treffer auftreten, bevor der Preis den Kanal um das Doppelte seiner halben Breite verlässt. Die gesammelten Statistiken können aus der Eigenschaft `BounceDistribution` oder aus den generierten Protokollmeldungen entnommen werden.

## Wie es funktioniert
1. Wenn die Strategie startet, validiert sie, dass das Instrument einen `PriceStep` ungleich Null bereitstellt. Punktbasierte Eingaben basieren auf diesem Wert, um MetaTrader „Punkte“ in dezimale Preisabstände umzuwandeln.
2. Ein aus `CandleType` erstelltes Kerzenabonnement versorgt den Bounce-Analysator nur mit abgeschlossenen Balken.
3. Die erste eingehende Kerze definiert die Kanalmitte (ihren Schlusskurs). Um dieses Zentrum herum wird ein symmetrisches Band mit einer Halbwertsbreite von `ChannelPoints * PriceStep` erstellt.
4. Jede neue fertige Kerze erhöht den Zykluszähler und wird nach drei Regeln ausgewertet:
   - **Breakout-Erkennung**: Wenn die Spanne der Kerze `center ± 2 * halfWidth` überschreitet, endet der aktuelle Zyklus und die Anzahl der Absprünge wird aufgezeichnet.
   - **Berührung des unteren Bandes**: Wenn die Kerze das untere Band überspannt und die vorherige Berührung nicht auch eine Berührung des unteren Bandes war, erhöht sich der Sprungzähler um eins und die Richtung wechselt auf „unten“.
   - **Berührung des oberen Bandes**: Symmetrische Regel für das obere Band.
5. Wenn ein Zyklus mehr Kerzen als `MaxHistoryCandles` dauert (und der Parameter positiv ist), wird der Kanal zwangsweise zurückgesetzt, um sicherzustellen, dass das Histogramm auch dann aktualisiert wird, wenn der Preis für immer seitwärts driftet.
6. Bei jedem Zurücksetzen des Zyklus wird das Verteilungswörterbuch aktualisiert und ein Informationsprotokoll erstellt, das das Verhalten der ursprünglichen Schnittstellenzähler widerspiegelt.

Die Strategie erteilt absichtlich keine Aufträge. Es sollte zusammen mit anderen Komponenten (Dashboards, Benutzeroberfläche, Datenexporteure) gehostet werden, die die `BounceDistribution`-Statistiken nutzen.

## Parameter
| Name | Typ | Standard | MetaTrader analog | Beschreibung |
| --- | --- | --- | --- | --- |
| `MaxHistoryCandles` | `int` | `10000` | `maxbar` Eingabe | Maximal zulässige Anzahl von Kerzen innerhalb eines Zyklus vor einem erzwungenen Reset. Auf `0` einstellen, um den Sicherheitsreset zu deaktivieren. |
| `ChannelPoints` | `int` | `300` | `BPoints` Eingabe | Halbe Breite des Absprungkanals, ausgedrückt in Preispunkten (`PriceStep` Vielfache). |
| `CandleType` | `DataType` | `M1` Zeitrahmen | `TF` Eingabe | Für die Absprungberechnungen verwendete Kerzenserie. |

## Unterschiede zum MetaTrader-Code
- Das Histogramm wird als Wörterbuch statt als Textobjekte im Diagramm gespeichert. Dadurch lassen sich die Informationen einfacher exportieren oder in StockSharp-Dashboards visualisieren.
- UI-spezifische Eingaben vom Indikator (Farben, Schriftarten, Schaltflächen) werden entfernt, da sie kosmetischer Natur waren und keinen Einfluss auf die Analyselogik haben.
- Das erzwungene Zurücksetzen durch `MaxHistoryCandles` ist jetzt optional (`0` deaktiviert es) und funktioniert bei Live-Datenströmen, während MetaTrader einen endlichen historischen Block verarbeitet hat.
- Alle informativen Nachrichten werden bis `AddInfoLog` auf Englisch verfasst, was der Anforderung für nur englischsprachige Codekommentare/Protokolle entspricht.

## Anwendungstipps
- Stellen Sie sicher, dass die ausgewählte Sicherheit `PriceStep` definiert; Andernfalls löst die Strategie beim Start eine Ausnahme aus, da punktbasierte Offsets nicht berechnet werden können.
- Kombinieren Sie die Strategie mit benutzerdefinierten UI-Widgets oder Skripten, die `BounceDistribution` lauten, um das Zählraster MetaTrader zu replizieren.
- Verwenden Sie kleinere Werte für `ChannelPoints`, wenn Sie Intraday-Rauschen analysieren, und größere Werte für längere Zeitrahmen oder volatile Instrumente.
- Um den historischen Scan der Version MQL zu emulieren, starten Sie die Strategie mit aktiviertem `HistoryBuildMode` in Ihrem Connector und lassen Sie ihn den angeforderten historischen Bereich verarbeiten. Die Verteilung wird gefüllt, sobald die nachgefüllten Kerzen geliefert werden.
