# Center-of-Gravity-OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den **Center of Gravity OSMA**-Oszillator, um potenzielle Trendumkehrungen zu erkennen.
Der Oszillator multipliziert einfache und gewichtete gleitende Durchschnitte, glättet das Ergebnis zweimal und verfolgt
Richtungsänderungen. Wenn der Indikator ein lokales Minimum bildet und nach oben dreht, schließt die Strategie
Short-Positionen und kann eine neue Long-Position eröffnen. Wenn ein lokales Maximum nach unten dreht,
werden Long-Positionen geschlossen und optional Shorts eröffnet.

## Funktionsweise
1. Der Schlusskurs wird als Eingabe für den benutzerdefinierten Indikator verwendet.
2. Der Indikator berechnet:
   - Einfachen gleitenden Durchschnitt (`SMA`) mit der Länge `Period`.
   - Gewichteten gleitenden Durchschnitt (`WMA`) mit derselben Länge.
   - Produkt dieser beiden Durchschnitte.
   - Zwei zusätzliche Glättungsschritte mit den Längen `SmoothPeriod1` und `SmoothPeriod2`.
3. Handelsregeln:
   - Wenn der vorherige Wert kleiner als der Wert davor war und der aktuelle Wert größer als der vorherige ist, drehte der Oszillator nach oben. Jede Short-Position wird geschlossen und eine Long-Position kann eröffnet werden.
   - Wenn der vorherige Wert größer als der Wert davor war und der aktuelle Wert kleiner als der vorherige ist, drehte der Oszillator nach unten. Jede Long-Position wird geschlossen und eine Short-Position kann eröffnet werden.
   - Optionale Stop-Loss- und Take-Profit-Werte in Preiseinheiten schützen offene Positionen.

## Parameter
- `Period` – Basisperiode für SMA und WMA.
- `SmoothPeriod1` – Länge der ersten Glättungsstufe.
- `SmoothPeriod2` – Länge der zweiten Glättungsstufe.
- `StopLoss` – Stop-Loss-Abstand in Preiseinheiten (0 zum Deaktivieren).
- `TakeProfit` – Take-Profit-Abstand in Preiseinheiten (0 zum Deaktivieren).
- `BuyPosOpen` – Eröffnung von Long-Positionen erlauben.
- `SellPosOpen` – Eröffnung von Short-Positionen erlauben.
- `BuyPosClose` – Long-Positionen bei einem Verkaufssignal schließen erlauben.
- `SellPosClose` – Short-Positionen bei einem Kaufsignal schließen erlauben.
- `CandleType` – Kerzentyp (Zeitrahmen) für Berechnungen.

## Hinweise
- Nur die C#-Version wird bereitgestellt. Der Python-Ordner ist absichtlich nicht vorhanden.
- Verwenden Sie Tabulatoren für Einrückungen beim Ändern des Codes.
