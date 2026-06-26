# Neuro Nirvaman EA 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Neuro Nirvaman EA 2 ist eine Multi-Layer-Perceptron-Strategie, die ursprünglich für MetaTrader 5 geschrieben wurde. Die Logik kombiniert vier Laguerre-geglättete +DI-Ströme mit zwei SilverTrend-Ausbruchsdetektoren. Jede Bar bewertet die Strategie drei Perceptronen, deren Gewichte durch die X-Parameter gesteuert werden. Ein Supervisor-Modul wählt aus, welche Perceptron-Ausgabe basierend auf dem ausgewählten Pass-Modus gehandelt werden soll. Das Handeln ist nur innerhalb des konfigurierten Sitzungsfensters erlaubt und alle Positionen werden geplättet, sobald das Fenster geschlossen wird.

## Indikatoren und Signale
- **Laguerre +DI-Filter** – Jeder Laguerre-Block glättet den +DI-Wert eines ADX-Indikators (gamma = 0.764). Der resultierende Wert oszilliert zwischen 0 und 1 und wird mit einer 0.5-Mittellinie mit benutzerdefinierten Abstandsschwellen verglichen.
- **SilverTrend-Ausbruch** – Zwei SilverTrend-Detektoren berechnen dynamische Support-/Resistance-Hüllen anhand der letzten neun Bars. Die Risikoeinstellung modifiziert die Hüllenbreite (`K = 33 - risk`). Ein Übergang von bärisch zu bullisch (oder umgekehrt) erzeugt ±1-Signale, die die Perceptronen speisen.

## Handelslogik
1. **Perceptron #1** verwendet Laguerre #1 für die Spannungskomponente und SilverTrend #1 für die Ausbruchskomponente. Gewichte `X11` und `X12` versetzen die Beiträge relativ zu 100.
2. **Perceptron #2** spiegelt das erste Perceptron, basiert aber auf Laguerre #2 und SilverTrend #2 mit Gewichten `X21` und `X22`.
3. **Perceptron #3** kombiniert die Spannungsausgaben von Laguerre #3 und Laguerre #4, gewichtet durch `X31` und `X32`.
4. **Supervisor-Modi (`Pass`)**
   - `1` – Perceptron #1 handeln (`< 0` öffnet Short, andernfalls Long).
   - `2` – Perceptron #2 handeln (`> 0` öffnet Long, andernfalls Short).
   - `3` – Eine Long-Position eröffnen, wenn sowohl Perceptron #3 als auch #2 positiv sind. Ein Short eröffnen, wenn Perceptron #3 nicht-positiv und Perceptron #1 negativ ist.
   - `4` – Handel deaktivieren (entspricht dem Standardverhalten des ursprünglichen EA).

Jeder Einstieg platziert eine Marktorder mit festem Volumen und zeichnet Stop-Loss-/Take-Profit-Niveaus in Preisschritten auf. Positionen werden bei jeder fertigen Kerze überwacht: wenn das Hoch/Tief die aufgezeichneten Ziele durchdringt, tritt die Strategie sofort aus. Das Verlassen des Handelsfensters erzwingt ebenfalls einen Ausstieg.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Risk1`, `Risk2` | SilverTrend-Risikoeinstellungen. Höhere Werte schrumpfen die Hülle und erzeugen häufigere Signale. |
| `LaguerreXPeriod` | ADX-Länge, die den Laguerre-Glätter speist (für jeden der vier Ströme). |
| `LaguerreXDistance` | Prozentabstand um die 0.5-Mittellinie, der bullische/bärische Spannung definiert. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | Perceptron-Gewichte (Werte werden in der Formel um 100 versetzt, genau wie in der MQL-Version). |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | Gewinnziel- und Schutz-Stop-Abstände in Preisschritten für die jeweiligen Perceptron-Signale. |
| `Pass` | Supervisor-Modus-Selektor (1–4). |
| `TradeVolume` | Basis-Ordergröße für Marktausstiege. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Handelssitzungsgrenzen. Wenn die aktuelle Uhrzeit außerhalb dieses Fensters liegt, werden alle Positionen geschlossen und keine neuen Trades erlaubt. |
| `CandleType` | Kerzenabonnement zum Antrieb der High-Level-Strategie. |

## Risikomanagement
Die Strategie verlässt sich auf die festen Stop-Loss- und Take-Profit-Abstände, die durch das Perceptron definiert sind, das den Einstieg ausgelöst hat. Es wird keine Pyramidierung oder Mittelung durchgeführt. Da die Logik nur handelt, wenn keine Position offen ist, ist die Exposition auf eine einzelne aktive Position begrenzt und alle Trades werden zwangsgeschlossen, sobald das Sitzungsfenster endet.

## Hinweise
- Gamma für den Laguerre-Glätter ist bei 0.764 fixiert, um die MQL-Implementierung zu imitieren.
- Pass-Wert `4` hält die Strategie inaktiv, was dem Sicherheitsstandard des ursprünglichen EA entspricht.
- SilverTrend-Berechnungen verwenden Indikator-Primitive (Highest, Lowest, Simple Moving Average) anstatt benutzerdefinierter Puffer, um den StockSharp-Richtlinien zu entsprechen.
