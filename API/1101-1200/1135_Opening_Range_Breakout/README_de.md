# Eröffnungsbereich-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie definiert einen Eröffnungsbereich und handelt Ausbrüche darüber oder darunter. Nach dem Ende des Eröffnungsbereichsfensters werden Stop-Orders an den Bereichsgrenzen vorbereitet, wenn die Breite einen Prozentsatz des Schlusskurses übersteigt. Positionen verwenden einen Stop-Loss und ein Gewinnziel basierend auf der Bereichsgröße. Optional wird nur ein Trade pro Tag eingegangen, und Verlust-Trades können umgekehrt werden. Alle Positionen werden am Ende der Sitzung geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis bricht über das Hoch des Eröffnungsbereichs aus.
  - **Short**: Preis bricht unter das Tief des Eröffnungsbereichs aus.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Stop-Loss oder Gewinnziel basierend auf dem Bereich.
  - Tagesendabschluss.
- **Stops**: Ja.
- **Standardwerte**:
  - `Eröffnungsbereich` = 09:30–10:15.
  - `Tagesende` = 15:45.
  - `MinRangePercent` = 0.35.
  - `RewardRisk` = 1.1.
  - `Retrace` = 0.5.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Preis
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
