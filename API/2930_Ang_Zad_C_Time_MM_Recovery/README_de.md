# Ang Zad C Time MM Recovery-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Ang Zad C Time MM Recovery-Strategie ist ein C#-Port des MetaTrader 5-Expertenberaters `Exp_Ang_Zad_C_Tm_MMRec`. Die Strategie kombiniert den benutzerdefinierten Ang_Zad_C-Kanal-Indikator mit einem konfigurierbaren Handelssitzungsfilter und einem adaptiven Positionsgrößenmodell, das das Risiko nach einer konfigurierbaren Anzahl von Verlust-Trades reduziert.

## Indikatorlogik
Der Ang_Zad_C-Indikator baut zwei adaptive Hüllen um den Preis auf. Jede Hülle wird aktualisiert, indem der gewählte angewendete Preis der aktuellen und vorherigen Kerze verglichen wird und sich mit dem Glättungsfaktor **Ki** in Richtung des neuen Preises bewegt. Die obere und untere Linie werden auf historischen Balken ausgewertet, die durch **Signal Bar** definiert sind, um nicht auf unfertigen Kerzen zu handeln.

## Trading-Regeln
* **Long-Einstieg** – Wenn die obere Linie auf dem vorherigen Referenzbalken über der unteren Linie war und auf dem aktuellsten Referenzbalken darunter kreuzt oder die untere Linie berührt. Wenn dies passiert, wird jede offene Short-Position geschlossen, bevor eine neue Long-Position geöffnet wird (wenn aktiviert).
* **Short-Einstieg** – Wenn die obere Linie auf dem vorherigen Referenzbalken unter der unteren Linie war und auf dem aktuellsten Referenzbalken darüber kreuzt oder die untere Linie berührt. Jede offene Long-Position wird geschlossen, bevor eine neue Short-Position geöffnet wird (wenn aktiviert).
* **Long-Ausstieg** – Wenn die obere Linie auf dem vorherigen Referenzbalken unter der unteren Linie liegt. Der Ausstieg kann über **Enable Long Exit** deaktiviert werden.
* **Short-Ausstieg** – Wenn die obere Linie auf dem vorherigen Referenzbalken über der unteren Linie liegt. Der Ausstieg kann über **Enable Short Exit** deaktiviert werden.

## Money-Management und Schutzmaßnahmen
* Handel ist nur innerhalb des konfigurierten Zeitfensters erlaubt, wenn **Use Time Filter** aktiviert ist. Zuvor geöffnete Positionen werden geschlossen, sobald die Sitzung endet.
* Das Trade-Volumen wird zwischen **Normal Volume** und **Small Volume** ausgewählt, abhängig davon, wie viele Verlust-Trades für jede Seite aufgetreten sind. Nach **Buy Loss Trigger** verlierenden Long-Trades (oder **Sell Loss Trigger** verlierenden Short-Trades) wird das reduzierte Volumen verwendet, bis ein profitabler Trade den Zähler zurücksetzt.
* Optionale Stop-Loss- und Take-Profit-Levels werden mit Preisschrittabständen registriert, die durch **Stop Loss Steps** und **Take Profit Steps** definiert werden.

## Parameter
| Name | Beschreibung |
| ---- | ------------ |
| Candle Type | Zeitrahmen der vom Indikator und Signalen verwendeten Kerzen. |
| Ki | Glättungskoeffizient der Ang_Zad_C-Hüllen. |
| Applied Price | Welcher Kerzenkurs in den Indikator eingespeist wird. |
| Signal Bar | Wie viele Balken zurück für die Signalauswertung verwendet werden (1 = vorheriger geschlossener Balken). |
| Use Time Filter / Trade Start / Trade End | Sitzungsbasiertes Trading aktivieren und Start- und Endzeit der Sitzung festlegen. |
| Enable Long/Short Entry | Öffnung neuer Long- oder Short-Trades erlauben. |
| Enable Long/Short Exit | Der Strategie erlauben, Positionen bei Indikatorumkehr zu schließen. |
| Buy/Sell Loss Trigger | Anzahl der Verlust-Trades, bevor das reduzierte Volumen angewendet wird. |
| Small Volume / Normal Volume | Ordergrößen für reduziertes und normales Risiko. |
| Stop Loss Steps / Take Profit Steps | Abstand für Schutzorders ausgedrückt in Preisschritten. |

## Konvertierungshinweise
* Die Logik folgt dem ursprünglichen MQL5-Code, einschließlich der direktionalen Kreuzprüfungen und des Zeitfensterverhaltens.
* Das adaptive Money-Management wird implementiert, indem realisierter Gewinn und Verlust pro Richtung verfolgt und nach der konfigurierten Anzahl von Verlusten auf das reduzierte Volumen umgeschaltet wird.
* Indikatorberechnungen vermeiden jeden direkten Pufferzugriff und werden auf fertigen Kerzen unter Verwendung der StockSharp-API auf hoher Ebene verarbeitet.
