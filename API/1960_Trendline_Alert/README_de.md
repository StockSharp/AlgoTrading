# Trendlinien-Alert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht zwei benutzerdefinierte Trendlinien und reagiert, wenn der Preis diese bricht. Die obere und untere Linie stellen Widerstands- und Unterstützungsniveaus dar. Wenn der Schlusskurs über die obere Linie kreuzt, wird eine Long-Position eröffnet. Wenn der Preis unter die untere Linie fällt, wird eine Short-Position eröffnet. Optionale Trailing-Stop-Logik schützt geöffnete Positionen, indem das Stop-Niveau in Handelsrichtung verschoben wird.

## Parameter

- `Breakout Points` – zusätzliche Punkte, die zu den Trendlinienniveaus addiert werden, um den Ausbruchs-Schwellenwert zu definieren.
- `Upper Line` – Preisniveau für den bullischen Ausbruch.
- `Lower Line` – Preisniveau für den bärischen Ausbruch.
- `Start Hour` – Handelsstartzeit in Stunden.
- `End Hour` – Handelsendzeit in Stunden.
- `Use Trailing Stop` – aktiviert die Trailing-Stop-Verwaltung.
- `Trailing Stop Points` – Abstand in Punkten für den Trailing Stop.
- `Candle Type` – Kerzen-Zeitrahmen für die Analyse.

## Funktionsweise

1. Die Strategie abonniert die ausgewählte Kerzenserie.
2. Für jede abgeschlossene Kerze wird überprüft, ob die Zeit innerhalb des angegebenen Handelsfensters liegt.
3. Ein Ausbruch wird erkannt, wenn der Kerzenschlusskurs die obere Linie nach oben oder die untere Linie nach unten kreuzt, angepasst um den Ausbruchs-Punkte-Schwellenwert.
4. Wenn ein Ausbruch eintritt und keine bestehende Position vorhanden ist, wird eine Marktorder in der Ausbruchsrichtung gesendet.
5. Wenn der Trailing Stop aktiviert ist, folgt das Stop-Niveau dem Preis, bis es ausgelöst wird.

## Hinweise

- Die Strategie ist eine vereinfachte Konvertierung des ursprünglichen MetaTrader TrendlineAlert-Experten. Das manuelle Zeichnen von Trendlinien wird durch feste Preisniveaus ersetzt, die durch Parameter definiert werden.
- Außerhalb der angegebenen Handelszeiten werden keine Orders platziert.
