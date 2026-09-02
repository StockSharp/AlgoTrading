# Diagramm der Strategie mit linearem Regressionskanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Durch die letzten fünfzig Schlusskurse wird eine Ausgleichsgerade gelegt und um sie herum ein Kanal in Vielfachen des Standardfehlers der Regression gezogen. Ein Kurs außerhalb des Kanals gilt als überdehnte Bewegung, und die Strategie holt ihn zur Geraden zurück, solange die Steigung des Kanals auf ihrer Seite ist.

![schema](schema.svg)

## Strategieübersicht

- LinearReg liefert den Wert der Geraden auf der aktuellen Kerze, LinearRegSlope ihre Richtung und StandardError die übliche Streuung der Schlusskurse um sie herum.
- Die Bänder sind die Gerade plus und minus dem Abweichungsfaktor mal Standardfehler, sodass sich der Kanal von selbst mit dem Markt weitet und verengt.
- Die Steigung wirkt als Filter: Ein Rücksetzer wird nur im steigenden Kanal gekauft, eine Spitze nur im fallenden verkauft.
- Ziel ist die Regressionsgerade selbst; Stop-Loss und Take-Profit gibt es nicht, genau wie in der Ausgangsstrategie.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Die Steigung der Regression liegt über null, der Schlusskurs unter dem unteren Band und die Position ist neutral. Die Kauforder eröffnet einen Long über ein Lot.
- **Short-Einstieg**: Die Steigung der Regression liegt unter null, der Schlusskurs über dem oberen Band und die Position ist neutral. Die Verkaufsorder eröffnet einen Short über ein Lot.
- **Ausstieg**: Ein Long wird geschlossen, sobald der Schlusskurs die Gerade von unten erreicht, ein Short, sobald er sie von oben erreicht. Beide Ausstiegsbausteine arbeiten im Schließmodus und bleiben ohne Position untätig.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| LinearReg Length | 50 | Anzahl der Kerzen, über die die Regressionsgerade gelegt wird. |
| LinearRegSlope Length | 50 | Anzahl der Kerzen für die Steigung; gleich der Länge der Geraden halten. |
| StandardError Length | 50 | Anzahl der Kerzen für den Standardfehler; gleich der Länge der Geraden halten. |
| Channel Deviation | 1.5 | Halbe Kanalbreite in Standardfehlern der Regression. |
| Volume | 1 | Ordervolumen in Lots. |
| Candles | 00:05:00 | Zeiteinheit der Kerzen, mit der das gesamte Diagramm arbeitet. |

## Diagrammdetails

- Ein Kerzenbaustein speist drei Indikatorbausteine und einen Konverter für den Schlusskurs, sodass alle Werte aus derselben abgeschlossenen Kerze stammen.
- Zwei Formelbausteine bilden die Bänder aus Gerade, Standardfehler und einer gemeinsamen, optimierbaren Abweichungskonstante.
- Sechs Vergleichsbausteine machen daraus Signale: zwei für die Steigung, zwei für die Bänder und zwei für die Rückkehr zur Geraden.
- Jeder Einstieg ist ein logisches UND aus Steigung, Band und neutraler Position; die Ausstiege führen direkt vom Vergleich zum Schließbaustein.
- Die Originalstrategie wartet zwanzig Bars zwischen den Trades und berechnet die Streuung über das ganze Fenster, während StandardError durch Fenster minus zwei teilt und den Kanal so etwa zwei Prozent breiter macht; für die ursprüngliche Breite die Abweichung auf rund 1,47 senken.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
