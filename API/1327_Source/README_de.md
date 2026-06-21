# Source-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Source eröffnet Long-Positionen, wenn die Kerze über ihrem Eröffnungskurs schließt, und Short-Positionen, wenn sie darunter schließt. Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Prozentsätze verwalten die offene Position.

## Details

- **Einstiegskriterien**: Long wenn Schlusskurs > Eröffnungskurs, Short wenn Schlusskurs < Eröffnungskurs
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal oder ausgelöste Stop-Verwaltung
- **Stops**: Optionaler Stop-Loss, Take-Profit, Trailing Stop
- **Standardwerte**:
  - `SL %` = 1
  - `TP %` = 3
  - `Trail Points %` = 3
  - `Trail Offset %` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
