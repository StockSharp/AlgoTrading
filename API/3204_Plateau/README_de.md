# Plateau-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Plateau-Strategie ist eine Konvertierung des ursprünglichen MetaTrader 5 Expert Advisors. Sie kombiniert ein Paar linear gewichteter gleitender Durchschnitte mit Bollinger Bands, um potenzielle Umkehrungen zu erkennen, wenn der Preis in der Nähe des unteren Bandes handelt.

## Trading-Idee

* Schnelle und langsame gleitende Durchschnitte mit der ausgewählten Glättungsmethode und Preisquelle berechnen.
* Bollinger Bands um dieselbe Preisserie aufbauen.
* Wenn der schnelle Durchschnitt über den langsamen kreuzt, während die vorherige Kerze unter dem unteren Band schloss, eine Long-Position eröffnen.
* Wenn der schnelle Durchschnitt unter den langsamen kreuzt, während die vorherige Kerze über dem unteren Band schloss, eine Short-Position eröffnen.
* Optional Signale umkehren, wenn der `Reverse`-Schalter aktiviert ist.

## Orderverwaltung

* Positionen können entweder mit einem festen Lot oder durch Riskieren eines Prozentsatzes des Portfolio-Wertes pro Trade dimensioniert werden.
* Stop-Loss- und Take-Profit-Niveaus werden in Pips ausgedrückt und unmittelbar nach dem Füllen der Marktorder angehängt.
* Ein Trailing-Stop kann aktiviert werden, wenn sowohl die Trailing-Distanz als auch der Schritt positiv sind.
* Wenn `Close Opposite` aktiviert ist, schließt die Strategie automatisch die entgegengesetzte Position, bevor ein neuer Trade eingegangen wird.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| Stop Loss | Stop-Loss-Abstand in Pips. |
| Take Profit | Take-Profit-Abstand in Pips. |
| Trailing Stop | Trailing-Stop-Abstand in Pips. |
| Trailing Step | Minimaler Schritt (in Pips), der erforderlich ist, um den Trailing-Stop zu bewegen. |
| Money Mode | Wählen zwischen festem Lot und risikoprozentbasierer Dimensionierung. |
| Lot / Risk | Entweder die feste Lot-Größe oder der Risikoanteil je nach ausgewähltem Geldmodus. |
| Fast MA / Slow MA | Perioden für das gleitende Durchschnittspaar. |
| MA Shift | Horizontale Verschiebung, die auf beide gleitenden Durchschnitte angewendet wird. |
| MA Method | Glättungsalgorithmus für gleitende Durchschnitte. |
| MA Price | Preisquelle für die Berechnung der gleitenden Durchschnitte. |
| Bands Period | Mittelungsperiode für Bollinger Bands. |
| Bands Shift | Horizontale Verschiebung der Bollinger-Band-Werte. |
| Bands Deviation | Standardabweichungsmultiplikator für Bollinger Bands. |
| Bands Price | Preisquelle für die Berechnung der Bollinger Bands. |
| Reverse | Long- und Short-Signallogik umkehren. |
| Close Opposite | Eine bestehende Position in der entgegengesetzten Richtung schließen, bevor eine neue eröffnet wird. |
| Verbose Log | Detaillierte Ausführungsinformationen in das Log drucken. |
| Candle Type | Kerzendatenserie für Indikatorberechnungen. |

## Hinweise

* Die Pip-Größe wird automatisch an Instrumente mit drei oder fünf Dezimalstellen angepasst, um dem ursprünglichen Expert-Verhalten zu entsprechen.
* Wenn der Trailing-Stop aktiviert ist, muss der Trailing-Schritt strikt positiv sein; andernfalls wirft die Strategie beim Start einen Fehler.
* Die risikobasierte Positionsgröße erfordert sowohl eine gültige Stop-Loss-Distanz als auch Portfolio-Bewertungsdaten. Wenn nicht verfügbar, fällt die Strategie auf das Standardvolumen zurück.
