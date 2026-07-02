# NTOqF-Multifilter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die NTOqF Multi-Filter-Strategie portiert den MetaTrader 4 Expertenberater „NTOqF“ (Versionen V1–V3) auf den High-Level API von StockSharp. Der ursprüngliche Roboter kombiniert mehrere Oszillatoren und Trendfolgefilter, die jeweils unabhängig aktiviert oder deaktiviert werden können. Diese C#-Version behält die gleiche Konfigurierbarkeit bei, unterstützt separate Zeitrahmen für jeden Indikator und wendet Handelsmanagement durch feste Stopps, Take-Profit-Ziele und einen optionalen Trailing Stop, ausgedrückt in Pips, an.

## Strategielogik
### Indikatorfilter
* **RSI-Filter** – erzeugt ein langes Signal, wenn der Wert RSI (an der konfigurierten Verschiebung) unter `RSI Lower` liegt, und ein kurzes Signal, wenn der Wert über `RSI Upper` liegt. Neutrale Messwerte streichen Einträge.
* **Stochastic-Filter** – vergleicht %K und %D. Wenn `Use Stochastic High/Low` aktiviert ist, muss die Hauptlinie auch über `Stoch High` für Long-Positionen oder unter `Stoch Low` für Short-Positionen liegen; andernfalls werden einfache %K/%D-Kreuzungen verwendet.
* **ADX-Filter** – verwendet +DI gegenüber –DI, um die Richtung zu bestimmen. Wenn die Option `Use ADX Main` aktiviert ist, muss die Hauptzeile ADX größer als `ADX Main` sein, bevor Einträge akzeptiert werden.
* **Parabolic SAR-Filter** – interpretiert den SAR-Wert relativ zum Schlusskurs des ausgewählten Balkens. Werte über dem Preis begünstigen Long-Positionen (was das Verhalten im MQL-Code widerspiegelt), Werte unter dem Preis begünstigen Short-Positionen.
* **Filter für gleitenden Durchschnitt** – vergleicht den ausgewählten gleitenden Durchschnitt (mit optionaler positiver Verschiebung) mit dem Schlusskurs bei der Basisverschiebung. Ein Preis über dem MA begünstigt Long-Positionen; Der Preis unten begünstigt Shorts.

Alle aktivierten Filter müssen sich auf die gleiche Richtung einigen. Wenn ein Filter einen neutralen Zustand zurückgibt (z. B. RSI bleibt zwischen seinen Schwellenwerten), wird keine Position geöffnet.

### Einreisebestimmungen
* Signale werden im primären Handelszeitraum (`Candle Type`) ausgewertet.
* Es ist jeweils nur eine Position zulässig; Die Strategie wartet darauf, dass die vorherige Position geschlossen wird, bevor sie eine neue eingibt.
* Das Bestellvolumen wird von `Trade Volume` (Lots) übernommen.

### Ausgangsregeln
* **Fester Stop-Loss / Take-Profit** – ausgedrückt in Pips und unter Verwendung der Schrittgröße des Instruments in Preis-Offsets umgewandelt. Setzen Sie einen Parameter auf `0`, um die entsprechende Ebene zu deaktivieren.
* **Trailing Stop** – wenn aktiviert, wird der Stop nachgezogen, sobald der nicht realisierte Gewinn die Trailing-Distanz überschreitet und der aktuelle Stop dem Preis um mehr als diese Distanz hinterherhinkt. Long-Positionen bewegen den Stop nach oben, Short-Positionen nach unten.

### Verhalten in mehreren Zeitrahmen
Jeder Indikator kann seinen eigenen Zeitrahmen abonnieren. Ein Zeitrahmenwert von `0` verwendet den primären Handelszeitrahmen wieder, während positive Werte minutenbasierte `TimeFrameCandle`-Abonnements darstellen. Indikatorwerte werden nur für abgeschlossene Kerzen ausgewertet und respektieren den Parameter `Shift`, sodass die Strategie das „Rückblick“-Verhalten des ursprünglichen MetaTrader-Experten widerspiegeln kann.

## Parameter
* **Kerzentyp** – Handelszeitrahmen, der zur Steuerung der Ausführungen verwendet wird.
* **Volumen** – Market-Order-Volumen (Lots).
* **Take Profit (Pips)** – Gewinnziel; `0` deaktiviert.
* **Stop-Loss (Pips)** – Schutzstopp; `0` deaktiviert.
* **Trailing verwenden** / **Trailing Stop (Pips)** – Trailing Stop aktivieren und dimensionieren.
* **Verschiebung** – Anzahl der abgeschlossenen Kerzen nach hinten beim Lesen der Indikatorwerte und des Preises.
* **RSI Parameter** – Umschalten, Zeitraum, obere/untere Schwellenwerte und Zeitrahmen.
* **Stochastic-Parameter** – Umschalten, %K/%D/Verlangsamungslängen, optionale hohe/niedrige Bestätigungsstufen und Zeitrahmen.
* **ADX-Parameter** – Umschalten, Zeitraum, DI-Zeitrahmen, optionaler Hauptlinienschwellenwert und Hauptzeitrahmen.
* **Parabolic SAR Parameter** – Umschalten, Beschleunigungsschritt, maximale Beschleunigung und Zeitrahmen.
* **Parameter für den gleitenden Durchschnitt** – Umschalten, Zeitraum, zusätzliche auf den MA-Puffer angewendete Verschiebung, Mittelungsmethode (SMA/EMA/SMMA/LWMA), angewendeter Preis und Zeitrahmen.

## Notizen
* Indikatorwarteschlangen berücksichtigen den konfigurierten `Shift` und stellen sicher, dass Signale auf die gleiche Weise wie der MQL-Experte auf historischen Werten basieren.
* Die Trailing-Logik wird erst aktiviert, wenn der Trade bereits mehr als die Trailing-Distanz im Gewinn ist und der Stop mehr als diese Distanz vom Preis entfernt ist, was dem ursprünglichen Verhalten von EA entspricht.
* Für dieses Strategiepaket wird keine Python-Version bereitgestellt.
