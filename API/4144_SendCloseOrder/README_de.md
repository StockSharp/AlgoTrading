# Close-Order-Strategie senden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Send Close Order ist eine Portierung des 2009 MetaTrader 4 Expertenberaters „SendCloseOrder“ von Vladimir Hlystov. Das ursprüngliche Skript zeichnet vier manuelle Trendlinien basierend auf Bill Williams-Fraktalen und öffnet oder schließt Marktaufträge, wann immer der Preis eines dieser prognostizierten Niveaus berührt. Die StockSharp-Version repliziert die Entscheidungslogik mit vollautomatischem Linienmanagement und funktioniert auf allen von der Plattform bereitgestellten Kerzenserien.

## Handelslogik

1. **Fraktale Erkennung** – jede fertige Kerze aktualisiert ein Schiebefenster mit fünf Balken. Sobald das Fenster voll ist, wird die Kerze in der Mitte mit den fraktalen Bedingungen von Bill Williams verglichen. Bestätigte Höchst- und Tiefstwerte werden chronologisch gespeichert.
2. **Trendlinienrekonstruktion**
   - *Verkaufslinie* verbindet die letzten beiden Aufwärtsfraktale, die durch ein Abwärtsfraktal getrennt sind, und bildet eine Widerstandssteigung.
   - *Close #1* ist die Verkaufslinie, die um `15` Preisschritte (15 × `Security.PriceStep`) nach oben verschoben wird und als lange Ausstiegsschiene fungiert.
   - *Kauflinie* verbindet die letzten beiden Abwärtsfraktale, die durch ein Aufwärtsfraktal getrennt sind, und bildet eine Unterstützungsneigung.
   - *Close #2* ist die um `15` Preisschritte nach unten verschobene Kauflinie und fungiert als Short-Exit-Schiene.
3. **Signalauswertung** – die vier Zeilen werden auf den Zeitstempel der fertigen Kerze hochgerechnet. Liegt der prognostizierte Preis innerhalb des Hoch-/Tief-Bereichs der Kerze (mit einer kleinen Toleranz von zwei Preisschritten), wird die entsprechende Aktion ausgelöst.
4. **Auftragsverwaltung**
   - Durch Berühren von „Schließen Nr. 1“ oder „Schließen Nr. 2“ wird die gesamte Position sofort über `ClosePosition()` geschlossen.
   - Durch Berühren der Verkaufs- oder Kauflinie wird eine Marktorder mit einem Volumen von `TradeVolume` geöffnet, sofern die resultierende absolute Position `MaxOrders × TradeVolume` nicht überschreitet. Wenn eine Gegenposition vorhanden ist, gleicht die Order diese zunächst aus und stapelt dann einen neuen Eintrag, was dem Verhalten von Sicherungskonten entspricht.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `EnableSellLine` | `true` | Erlauben Sie Trades, wenn die prognostizierte Widerstandslinie erreicht wird. |
| `EnableBuyLine` | `true` | Erlauben Sie Trades, wenn die prognostizierte Unterstützungslinie erreicht wird. |
| `EnableCloseLongLine` | `true` | Ermöglichen Sie das Schließen von Long-Positionen auf der verschobenen Widerstandslinie (Schluss Nr. 1). |
| `EnableCloseShortLine` | `true` | Erlauben Sie das Schließen von Short-Positionen auf der verschobenen Unterstützungslinie (Schluss #2). |
| `MaxOrders` | `1` | Maximale Anzahl gestapelter Einträge in der aktuellen Richtung. |
| `TradeVolume` | `0.1` | Volumen jeder einzelnen Market Order. |
| `CandleType` | `1h` Zeitrahmen | Für fraktale Berechnungen verwendete Kerzenserie. |

## Unterschiede zur MetaTrader-Version

- Der Port StockSharp berechnet die vier Linien jedes Mal neu, wenn ein neues Fraktal erscheint. In MetaTrader musste der Benutzer Trendlinien manuell löschen und neu zeichnen.
- Die Ausführung basiert auf aggregierten Nettopositionen; Gleichzeitige Long- und Short-Baskets werden vom Standardportfoliomodell von StockSharp nicht unterstützt.
- Die Berührungserkennung verwendet das Hoch/Tief der fertigen Kerze mit einer Preisschritttoleranz anstelle der momentanen Bid/Ask-Kurse von Ticks.
- Diagrammobjekte (Trendlinien und Beschriftungen) werden nicht erstellt; Der Fokus liegt auf Handelssignalen.

## Nutzungshinweise

- Die Strategie kann auf jedem Instrument ausgeführt werden, das Kerzen und einen gültigen `PriceStep` bereitstellt. Wenn `Security.PriceStep` Null ist, fällt der Code auf `0.0001` zurück.
- Erhöhen Sie `MaxOrders`, um das Stapelverhalten des ursprünglichen EA zu emulieren. Halten Sie `TradeVolume` an der Losgröße des Instruments ausgerichtet, um Rundungen zu vermeiden.
- Der Linienversatz ist auf den historischen Wert von 15 Punkten festgelegt. Passen Sie den Quellcode an, wenn die Eingabe MetaTrader geändert wird.

Es wird nur die C#-Implementierung bereitgestellt. Eine Python-Übersetzung wird bei Bedarf separat hinzugefügt.
