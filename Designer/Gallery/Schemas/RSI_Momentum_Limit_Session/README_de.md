# RSI-Momentum-Limit-Session-Strategiediagramm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm verbindet RSI(14), Momentum(14), einen ganztägigen Working-time-Filter und verwaltete Pending Limits. Überverkauft mit schwachem Momentum setzt einen Kauf unter den Kerzenstart, überkauft mit starkem Momentum einen Verkauf darüber; abgelaufene Signale stornieren ihre Order ausdrücklich.

![schema](schema.svg)

## Strategieübersicht

- Fertige Fünfminutenkerzen speisen RSI, Momentum, den Eröffnungspreis für Einstiege und den Schluss für den Schutz.
- Working time erlaubt 00:00 bis 23:59 und entspricht der praktisch ganztägigen Quellsitzung, lässt den Zeitbaustein aber konfigurierbar sichtbar.
- RSI unter 30, Momentum unter 1 und Position <= 0 erlauben Kauf; RSI über 70, Momentum über 1 und Position >= 0 erlauben Verkauf.
- Einmal-Flags halten höchstens eine Order je Signalphase; veraltete eigene oder entgegengesetzte Orders werden storniert.
- Nach Ausführung gelten absoluter Gewinn 35 und Verlust 8; der fertige Schluss speist den Preiseingang des Schutzes.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: In der Sitzung registrieren RSI < 30, Momentum < 1 und Position <= 0 ein Kauf-Limit über eine Einheit bei OpenPrice − 25; ein aktiver Verkauf wird zuerst storniert.
- **Short-Einstieg**: In der Sitzung registrieren RSI > 70, Momentum > 1 und Position >= 0 ein Verkauf-Limit über eine Einheit bei OpenPrice + 25; ein aktiver Kauf wird zuerst storniert.
- **Ausstieg**: Der Schutz schließt bei absolutem Gewinn 35 oder Verlust 8. Eine Kauforder wird bei ungültigem RSI, Momentum oder Positionszustand storniert; Verkauf ist spiegelbildlich.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiges Kerzenintervall; fünf Minuten sind Replay-Anpassung, C# nutzt standardmäßig fünfzehn. |
| RSI Period | 14 | Anzahl der RelativeStrengthIndex-Werte. |
| Momentum Period | 14 | Anzahl der Momentum-Werte. |
| Session Start | 00:00:00 | Beginn des Working-time-Fensters. |
| Session End | 23:59:00 | Ende des Fensters; 23:59 erhält das ganztägige Verhalten. |
| RSI Buy Threshold | 30 | RSI muss für Kauf darunter liegen. |
| RSI Sell Threshold | 70 | RSI muss für Verkauf darüber liegen. |
| Momentum Threshold | 1 | Momentum muss für Kauf darunter und für Verkauf darüber liegen. |
| Limit Offset, price units | 25 | Absoluter Replay-Abstand von OpenPrice. |
| Order Volume | 1 | Volumen jeder Pending-Limit-Order. |
| Take Profit, price units | 35 | Absoluter günstiger Abstand vom Einstieg. |
| Stop Loss, price units | 8 | Absoluter ungünstiger Abstand vom Einstieg. |

## Diagrammdetails

- C# nutzt standardmäßig 15-Minuten-Kerzen. Das Diagramm verwendet fünf Minuten im Replay für genügend sichtbare Zyklen; beide Perioden bleiben 14, also ist dies eine erklärte Abtastanpassung.
- Die Quelle versetzt um 5 × PriceStep. Ohne separaten Schrittbaustein nutzt das Diagramm 25 absolute Preiseinheiten; dies ist eine Galerie-Einstellung, nicht der Quellstandard.
- Limits werden wie in ProcessCandle exakt aus OpenPrice berechnet. ClosePrice ist getrennt und aktualisiert nur Position protection.
- Quellabstände sind 35 × PriceStep und 8 × PriceStep. Das Diagramm behält 35 und 8 als absolute Einheiten statt sie als Prozente auszugeben.
- Einmal-Flags modellieren die Prüfung aktiver Orders und werden bei ungültigem RSI, Momentum oder Positionszustand zurückgesetzt.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
