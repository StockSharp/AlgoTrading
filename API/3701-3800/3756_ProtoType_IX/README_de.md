# Prototyp-IX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
ProtoType IX ist eine Trendfolgestrategie mit mehreren Filtern, die vom ursprünglichen MetaTrader 4 Expert Advisor übernommen wurde. Der Algorithmus beobachtet Williams %R-Schwankungen, um neue impulsive Bewegungen zu erkennen und validiert sie mit der Average True Range (ATR)-Erweiterung. Trades werden nur eröffnet, wenn das prognostizierte Chance-Risiko-Verhältnis attraktiv genug ist und der Ausbruch bestätigt wird.

## Indikatoren und Signale
- **Williams %R (Zeitraum konfigurierbar)** – überwacht überkaufte/überverkaufte Rotationen. Die Strategie zeichnet die beiden letzten Swing-Hochs und Swing-Tiefs auf, die auftreten, wenn der Indikator seine Extremzonen verlässt.
- **Average True Range (ATR)** – misst die aktuelle Volatilität. Ausbrüche gelten als gültig, wenn der Abstand zwischen dem letzten und dem vorherigen Schwung `ATR × multiplier` überschreitet.

## Teilnahmebedingungen
1. Warten Sie, bis die jüngsten Swing-Hochs und -Tiefs aufgezeichnet sind.
2. Bestimmen Sie die Williams %R-Richtung. Wenn der Indikator über dem oberen Schwellenwert liegt, wird die bullische Tendenz gespeichert; Liegt er unter dem unteren Schwellenwert, wird die bärische Tendenz gespeichert.
3. Bestätigen Sie die Swing-Struktur mit ATR:
   - Aufwärtstrend – das letzte Swing-Hoch muss das vorherige Swing-Hoch um mindestens `ATR × multiplier` übersteigen und das letzte Swing-Tief muss höher sein als das vorherige Swing-Tief.
   - Abwärtstrend – das letzte Swing-Tief muss um mindestens `ATR × multiplier` unter das vorherige Swing-Tief fallen und das letzte Swing-Hoch muss niedriger sein als das vorherige Swing-Hoch.
4. Bewerten Sie das Chance-Risiko-Verhältnis anhand des aktuellen Schlusskurses:
   - **Long**: Ziel = max(letztes Swing-Hoch, vorheriges Swing-Hoch); stop = max(letztes Swing-Tief, vorheriges Swing-Tief).
   - **Short**: Ziel = min(letztes Swing-Tief, vorheriges Swing-Tief); stop = min(letzter Schwung hoch, vorheriger Schwung hoch).
5. Eröffnen Sie eine Position nur, wenn `take profit distance / stop loss distance ≥ TP/SL criteria` und die Zielentfernung größer als die minimale Spread-Anforderung ist.

## Ausgangsregeln
- Erste Schutzanordnungen werden unmittelbar nach der Einreise erteilt. Stop-Loss- und Take-Profit-Level werden in Preisschritte umgewandelt, um StockSharp Schutzaufträge zu verwenden.
- Nach Ablauf der konfigurierten `Zero Bar`-Verzögerung wird der Stop-Loss mithilfe eines ATR-basierten Trailing-Modells verschärft:
  - Long-Positionen liegen hinter dem Stop bei `max(previous stop, close − 2 × ATR)`.
  - Short-Positionen liegen hinter dem Stop bei `min(previous stop, close + 2 × ATR)`.

## Positionsgrößen
Die Losgröße wird aus dem Portfoliowert und dem Parameter `Risk %` geschätzt. Die Stop-Loss-Distanz in Preisschritten wird verwendet, um das zulässige monetäre Risiko in Volumen umzuwandeln. Die Lautstärken werden auf den Instrumentenlautstärkeschritt normalisiert und durch `Max Order Size` begrenzt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| Williams %R Zeitraum | Länge des Williams %R-Indikators. |
| Kriterien WPR | Absoluter Schwellenwert, der überkaufte/überverkaufte Zonen definiert. |
| ATR Zeitraum | Länge des Average True Range-Indikators. |
| ATR Multiplikator | Multiplikator zur Breakout-Validierung auf ATR angewendet. |
| Nullbalken | Anzahl der Balken vor der Aktivierung des ATR-Trailings. |
| Min. Zielstreuung | Minimale akzeptable Zielentfernung, ausgedrückt in Spread-Vielfachen. |
| TP/SL-Kriterien | Für den Abschluss eines Handels ist ein minimales Take-Profit-/Stop-Loss-Verhältnis erforderlich. |
| Maximale Bestellungen | Maximal gleichzeitig geöffnete Bestellungen. |
| Maximale Bestellgröße | Obergrenze für das Bestellvolumen nach Größenbestimmung. |
| Risiko % | Risikoprozentsatz, der für die Positionsgrößenbestimmung verwendet wird. |
| Kerzentyp | Kerzendatentyp für Berechnungen. |

## Notizen
- Die Strategie konzentriert sich auf ein einzelnes Wertpapier, behält aber die Multifilterlogik des ursprünglichen EA bei.
- Schutzaufträge basieren auf der Preisstufe des Instruments; Stellen Sie sicher, dass die Instrumentenmetadaten konfiguriert sind, bevor Sie die Strategie ausführen.
- Nullwerte für Volumenschritt oder Schrittpreis werden durch angemessene Standardwerte ersetzt, um die Größenbestimmungsroutine stabil zu halten.
