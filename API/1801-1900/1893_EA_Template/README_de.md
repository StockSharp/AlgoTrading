# EA Template-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie stammt aus einer MetaTrader-EA-Vorlage. Sie analysiert die vorherige abgeschlossene Kerze und eröffnet eine Position in Richtung des Kerzenkörpers. Eine bullische Kerze löst einen Long-Trade aus, eine bärische Kerze einen Short-Trade. Der Umkehrmodus kehrt die Interpretation der Kerze um, sodass die Strategie gegen die Kerzenfarbe handelt.

Die Strategie unterstützt eine feste Positionsgröße oder eine kapitalbasierte Berechnung. Stop-Loss- und Take-Profit-Niveaus werden in Punkten vom Einstiegspreis festgelegt. Der Handel wird übersprungen, wenn der Spread den erlaubten Schwellenwert überschreitet.

## Details

- **Einstiegskriterien**:
  - **Long**: vorheriger Kerzenschluss > Eröffnung und `ReverseTrade` deaktiviert.
  - **Short**: vorheriger Kerzenschluss < Eröffnung und `ReverseTrade` deaktiviert.
  - Wenn `ReverseTrade` aktiviert ist, werden die Signale umgekehrt.
  - Der Spread muss unter `SpreadLimit` Punkten liegen.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kerzenfarbe oder Stop-Loss/Take-Profit ausgelöst.
- **Positionsgröße**:
  - Feste Größe `Lots` oder kapitalbasierte Größe mit `RiskPercent`, wenn `UseMoneyManagement` true ist.
- **Stops**:
  - `StopLoss` und `TakeProfit` in Punkten relativ zum Einstiegspreis.
- **Long/Short**: Beide Richtungen.
- **Indikatoren**: Keine.
- **Risikolevel**: Mittel.

Parameter erlauben die Anpassung von Kerzentyp, Umkehrmodus, Money-Management-Regeln und Risikolimits.
