# ORB 15m – Erster 15-Minuten-Ausbruch (Long/Short)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie steigt beim Schlusskurs der ersten 15-Minuten-Kerze nach der Sitzungseröffnung in Stockholmer Zeit ein. Eine bullische erste Kerze löst einen Long-Trade aus, eine bärische Kerze einen Short. Die Positionsgröße wird aus dem Risikoprozentsatz und dem Abstand zum Stop berechnet.

## Details

- **Einstiegskriterien**: Handel auf der ersten 15-Minuten-Kerze nach der Sitzungseröffnung; Long, wenn die Kerze oberhalb ihres Eröffnungskurses schließt, Short, wenn unterhalb.
- **Ausstiegskriterien**: Stop-Loss am entgegengesetzten Extrem der Referenzkerze; optionaler Take-Profit bei `RMultiple`-fachem Risiko oder andernfalls am Sitzungsende.
- **Long/Short**: Beide.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskPct = 1`
  - `TpTenR = true`
  - `RMultiple = 10`
  - `SessionOpenHour = 15`
  - `SessionOpenMinute = 30`
  - `SessionEndHour = 22`
  - `SessionEndMinute = 0`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
