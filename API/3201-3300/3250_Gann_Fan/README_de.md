# Gann Fan-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie reproduziert den MetaTrader-Experten **GANN_FAN** unter Verwendung der High-Level-API. Sie kombiniert Trendfilter aus linear gewichteten gleitenden Durchschnitten mit Momentum-Bestätigung, einem MACD-Richtungstor und einer fraktalbasierten Rekonstruktion des Gann Fan, um bullische oder bärische Tendenzen zu bestimmen. Das Risikomanagement spiegelt den ursprünglichen Roboter mit gestapelten Martingale-Einstiegen, festen Stops, Trailing-Schutz und optionalen Break-even-Bewegungen wider.

## Handelslogik

1. **Trendfilter** – Zwei linear gewichtete gleitende Durchschnitte (LWMA), die auf dem typischen Preis (H+L+C)/3 basieren, definieren den schnellen und langsamen Trend. Long-Trades erfordern, dass die schnelle LWMA über der langsamen LWMA bleibt; Short-Trades benötigen den umgekehrten Crossover.
2. **Momentum-Bestätigung** – Die Strategie berechnet den klassischen Momentum-Oszillator als `100 * Close / Close(n)` und bewertet die Abweichung vom neutralen Niveau 100 über die letzten drei abgeschlossenen Kerzen. Mindestens eine Abweichung muss den konfigurierten Schwellenwert überschreiten, um die Stärke in Richtung des Trades zu bestätigen.
3. **MACD-Richtung** – Ein konfigurierbares MACD-Signal (schnelle, langsame und Signal-EMA-Perioden) muss mit dem Trend übereinstimmen. Long-Einstiege erfordern, dass die MACD-Linie größer als die Signallinie ist, während Shorts erfordern, dass die MACD-Linie unter der Signallinie bleibt.
4. **Gann Fan-Ausrichtung** – Bestätigte Bill-Williams-Fraktale rekonstruieren die bullischen und bärischen Fan-Strahlen. Die zwei jüngsten Abwärtsfraktale bilden den bullischen Strahl; seine Steigung muss positiv sein, um Longs zu erlauben. Die zwei neuesten Aufwärtsfraktale definieren den bärischen Strahl; seine Steigung muss negativ sein, um Leerverkäufe zu autorisieren.
5. **Positions-Stapelung** – Wenn ein neues Signal eintrifft, kann die Strategie einer bestehenden Position bis zum konfigurierten Maximum hinzufügen. Jede zusätzliche Order erhöht das Volumen durch Multiplikation des Basislots mit dem Lot-Exponenten, was die Martingale-Größenberechnung der MQL-Version nachahmt.

## Risikomanagement

- **Fester Stop-Loss und Take-Profit** – In Instrument-Preisschritten ausgedrückt, automatisch von der Strategie mit `Security.PriceStep` konvertiert.
- **Break-even-Kontrolle** – Wenn aktiviert, wird der Stop nach Erreichen der Auslösedistanz auf Einstieg plus/minus dem konfigurierten Offset vorgeschoben.
- **Trailing Stop** – Aktiviert sich nach Erreichen der Auslösedistanz. Der Stop kann dem Markt entweder durch einen festen Abstand zum Schluss folgen oder den niedrigsten (für Longs) / höchsten (für Shorts) Wert der jüngsten Kerzen plus einem Padding-Faktor einschließen.
- **Force-Exit-Schalter** – Das Setzen von `Force Exit` auf `true` liquidiert sofort jede offene Exposition auf der nächsten abgeschlossenen Kerze.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Volume** | Basisauftragsgröße für den ersten Einstieg. |
| **Fast LWMA / Slow LWMA** | Perioden der linear gewichteten gleitenden Durchschnitte für den Trendfilter. |
| **Momentum Period / Threshold** | Rückblick der Momentum-Berechnung und minimale Abweichung von 100 zum Handeln. |
| **MACD Fast / Slow / Signal** | EMA-Perioden für den MACD-Bestätigungsfilter. |
| **Fractal History** | Maximale Anzahl bestätigter Fraktal-Punkte zum Aufbau der Gann Fan-Strahlen. |
| **Max Trades** | Maximale Anzahl gestapelter Einstiege in eine einzelne Richtung. |
| **Lot Exponent** | Multiplikator für das Basisvolumen bei jedem zusätzlichen Einstieg. |
| **Stop Loss / Take Profit** | Schutzabstände in Preisschritten. |
| **Enable Trailing** | Aktiviert das Trailing-Stop-Management. |
| **Trail Trigger / Distance / Padding** | Gewinnauslöser, Trailing-Distanz und zusätzliches Padding (in Preisschritten) beim Trailing über Kerzenextreme. |
| **Use Candle Trail** | Aktiviert kerzenbasiertes Trailing zusätzlich zum Festabstands-Trail. |
| **Trailing Candles** | Anzahl der letzten abgeschlossenen Kerzen für kerzenbasierte Trailing-Niveaus. |
| **Enable Break-even** | Schaltet die Break-even-Logik ein oder aus. |
| **Break-even Trigger / Offset** | Gewinnauslöser und Offset (in Preisschritten) zum Verschieben des Stops auf Break-even. |
| **Use Gann Filter** | Erzwingt die bullische/bärische Fan-Ausrichtung für Einstiege. |
| **Force Exit** | Zwingt die Strategie, alle Positionen auf der nächsten Kerze zu schließen. |
| **Candle Type** | Kerzenserie für Berechnungen und Ordergenerierung. |

## Hinweise

- Alle Indikatorberechnungen arbeiten ausschließlich auf abgeschlossenen Kerzen von `SubscribeCandles` und `Bind`, gemäß den Best Practices der StockSharp High-Level-API.
- Trailing- und Break-even-Abstände passen sich automatisch an die Tick-Größe des Instruments an. Wenn `PriceStep` nicht verfügbar ist, bleiben Schutzfunktionen inaktiv, bis der Connector sie bereitstellt.
- Die Strategie hält separate Zustände für Long- und Short-Positionen, damit Trailing- und Break-even-Niveaus zurückgesetzt werden, wenn die Exposition die Richtung ändert.
- Um den MetaTrader-Experten eng nachzuahmen, werden Alerts, Benachrichtigungen und explizite Diagrammobjekte durch StockSharp-native Fan-Rekonstruktion mit Fraktalen ersetzt.
