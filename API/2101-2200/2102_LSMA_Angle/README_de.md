# LSMA-Winkel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Winkel des Least Squares Moving Average (LSMA), um die Trendrichtung zu erkennen. Der Winkel wird durch die Differenz zwischen zwei LSMA-Werten angenähert, die durch eine konfigurierbare Anzahl von Balken getrennt sind.

- **Long-Einstieg**: LSMA-Winkel steigt über den positiven Schwellenwert.
- **Long-Ausstieg**: Winkel kehrt unter den positiven Schwellenwert zurück.
- **Short-Einstieg**: LSMA-Winkel fällt unter den negativen Schwellenwert.
- **Short-Ausstieg**: Winkel kehrt über den negativen Schwellenwert zurück.

## Parameter
- `LSMA Period`: Länge für die LSMA-Berechnung.
- `Angle Threshold`: Absolutwert, der die neutrale Zone um Null definiert.
- `Start Shift`: Älterer Balken für die Winkelberechnung.
- `End Shift`: Neuerer Balken für die Winkelberechnung.
- `Candle Type`: Kerzen-Datentyp für die Berechnung.

## Hinweise
- Winkelwerte werden je nach Wertpapier in Punkte skaliert (1000 für JPY-Paare, sonst 100000).
- Funktioniert nur auf abgeschlossenen Kerzen.
