# MA Mirror-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MA Mirror-Strategie ist eine Umsetzung des MetaTrader-Experten *MA_MirrorEA*. Das System vergleicht zwei einfache gleitende Durchschnitte
Berechnet im gleichen Zeitraum, aber unter Verwendung unterschiedlicher Preisquellen: Kerzenschließungen versus Kerzenöffnungen. Wenn der gleitende Durchschnitt von
Die Schlusskurse bleiben über dem gleitenden Durchschnitt der Eröffnungskurse. Die Strategie möchte long bleiben. wenn es unter die Öffnung fällt
Durchschnittlich möchte die Strategie kurz sein. Ein konfigurierbarer Verschiebungsparameter ermöglicht das Ablesen der gleitenden Durchschnitte älterer Kerzen
sodass der StockSharp-Port die visuelle Verschiebung reproduzieren kann, die im ursprünglichen MetaTrader-Indikator angewendet wurde.

Die StockSharp-Implementierung behält das ursprüngliche „Spiegel“-Verhalten bei: Es kann immer nur eine Marktposition existieren, und a
Bei einem Signalwechsel wird zunächst die vorherige Position geschlossen und anschließend eine neue in die entgegengesetzte Richtung geöffnet. Genau wie der MetaTrader
Code beginnt die Strategie mit einem virtuellen Short-Signal, was bedeutet, dass der allererste echte Handel erst nach dem Schlussdurchschnitt stattfindet
bewegt sich über dem offenen Durchschnitt.

## Handelslogik
1. Abonnieren Sie die durch `CandleType` definierte Kerzenserie und verarbeiten Sie nur fertige Kerzen, um vorzeitige Entscheidungen zu vermeiden.
2. Füttere zwei einfache gleitende Durchschnitte mit den Schluss- und Eröffnungskursen der Kerze. Beide Indikatoren haben das gleiche `MovingPeriod`, also ihre
Werte können direkt verglichen werden.
3. Speichern Sie die aktuellen gleitenden Durchschnittswerte in Ringpuffern. Die Puffer ermöglichen das Abrufen des Werts von `MovingShift`
Kerzen vor, wobei der Verschiebungsparameter MetaTrader emuliert wird, ohne verbotene Indikatormethoden aufzurufen.
4. Wenn der verschobene Schlussdurchschnitt über dem verschobenen Eröffnungsdurchschnitt liegt, setzen Sie das gewünschte Signal auf **Kauf**. Wenn es unten ist, stellen Sie das ein
gewünschtes Signal zum **Verkauf**. Wenn beide Mittelwerte gleich sind, bleibt das vorherige Signal erhalten.
5. Wenn dies das erste Signal ist und es nicht bullisch ist, bleiben Sie flach. Andernfalls, wenn sich das gewünschte Signal vom zuletzt ausgeführten unterscheidet
Signal, schließen Sie alle bestehenden Engagements und eröffnen Sie eine neue Marktposition mit `TradeVolume` Lots in der neuen Richtung.
6. Aktualisieren Sie das gespeicherte Signal, damit spätere Kerzen doppelte Anweisungen ignorieren, während die Positionsrichtung unverändert bleibt.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Zeitrahmen von 1 Minute | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. |
| `MovingPeriod` | `int` | `20` | Länge der einfachen gleitenden Durchschnitte, die für Schluss- und Eröffnungskurse verwendet werden. |
| `MovingShift` | `int` | `0` | Anzahl der abgeschlossenen Kerzen, bei denen die gleitenden Durchschnittswerte nach hinten verschoben werden. |
| `TradeVolume` | `decimal` | `1` | Menge, die für jede Marktorder verwendet wird. |

## Unterschiede zum ursprünglichen MetaTrader-Experten
- Die in der Include-Datei MQL enthaltenen Money-Management-Helfer (Stop-Loss, Take-Profit, Trailing-Stop) werden nicht portiert. Die
Die Version StockSharp handelt immer mit einem festen `TradeVolume` und verlässt sich bei Bedarf auf externe Risikokontrollen.
- MetaTrader speichert einzelne Bestellungen, während StockSharp mit Nettopositionen arbeitet. Durch die Konvertierung wird die bestehende Nettoposition geschlossen
bevor Sie ein neues öffnen, damit die resultierende Belichtung dem Einzelticketverhalten von EA entspricht.
- Die Verarbeitung der Indikatoren erfolgt über das Kerzenabonnement API von StockSharp zusammen mit den Indikatoren und `SimpleMovingAverage`
interne Puffer statt `iMA` direkt aufzurufen.

## Anwendungstipps
- Passen Sie `TradeVolume` an die Lotstufe des Instruments an, bevor Sie mit der Strategie beginnen. Der Konstruktor weist auch den gleichen Wert zu
`Strategy.Volume`, sodass Hilfsmethoden Aufträge mit der erwarteten Größe erteilen.
- Erhöhen Sie `MovingShift`, wenn Sie die gleitenden Durchschnitte älterer Kerzen ablesen möchten, um sie beispielsweise an die Entwicklung von MetaTrader anzupassen.
Plattformplots verschobene Indikatoren.
- Fügen Sie die Strategie zu einem Diagramm hinzu, um Kerzen zusammen mit gleitenden Durchschnitten und ausgeführten Trades zu visualisieren, was es einfacher macht
um zu bestätigen, dass Umkehrungen genau dann stattfinden, wenn der Schlussdurchschnitt den Eröffnungsdurchschnitt kreuzt.

## Indikatoren
- Zwei einfache gleitende Durchschnitte (Schlusskurs und Eröffnungskurs) mit identischer Länge und optionaler Rückwärtsverschiebung.
