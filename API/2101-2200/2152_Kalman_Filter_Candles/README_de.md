# Kalman-Filter-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet den Kalman-Filter auf die Eröffnungs- und Schlusskurse jeder Kerze an. Die resultierenden geglätteten Kerzen werden als bullisch oder bärisch klassifiziert, je nachdem ob der geglättete Schlusskurs über oder unter dem geglätteten Eröffnungskurs liegt. Positionen werden geöffnet, wenn sich die Kerzenfarbe ändert:

- **Bullisch (rosa)** &rarr; öffnet eine Long-Position und schließt jede Short-Position.
- **Bärisch (blau)** &rarr; öffnet eine Short-Position und schließt jede Long-Position.

## Parameter

- `Process Noise` &ndash; Glättungsfaktor für den Kalman-Filter.
- `Candle Type` &ndash; Zeitrahmen der in der Strategie verwendeten Kerzen.

## Funktionsweise

1. Für jede abgeschlossene Kerze werden die Eröffnungs- und Schlusskurse individuell mit separaten Kalman-Filtern geglättet.
2. Ein bullisches Signal wird erzeugt, wenn der geglättete Schlusskurs den geglätteten Eröffnungskurs übersteigt. Ein bärisches Signal tritt auf, wenn der geglättete Schlusskurs unter dem geglätteten Eröffnungskurs liegt.
3. Die Strategie geht bei einem bullischen Signal eine Long-Position ein und bei einem bärischen Signal eine Short-Position. Entgegengesetzte Positionen werden automatisch geschlossen.

Die Strategie ist als Beispiel für die Kombination mehrerer Kalman-Filter zur Bildung eines einfachen Trendfolge-Systems gedacht.
