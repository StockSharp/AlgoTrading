# Gap-Fill-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Gap-Fill-Strategie nutzt Preislücken zwischen aufeinanderfolgenden 15-Minuten-Kerzen aus.
Wenn eine neue Kerze über dem Hoch der vorherigen Kerze um mehr als einen konfigurierbaren Schwellenwert öffnet, verkauft die Strategie und platziert ein Kauflimit am vorherigen Hoch, mit dem Ziel, dass die Lücke geschlossen wird.
Wenn eine Kerze unter dem vorherigen Tief um mehr als den Schwellenwert öffnet, kauft sie und platziert ein Verkaufslimit am vorherigen Tief.
Der Schwellenwert wird als `MinGapSize` Preisschritte plus dem aktuellen Spread zwischen bestem Geld- und Briefkurs berechnet.

## Details

- **Einstiegskriterien**: Lücke zwischen aktuellem Eröffnungskurs und vorherigem Hoch/Tief überschreitet `MinGapSize` plus Spread.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Limitorder am Extrempunkt der vorherigen Kerze.
- **Stops**: Nein.
- **Standardwerte**:
  - `MinGapSize` = 1
  - `Volume` = 0.1
  - `CandleType` = 15 Minuten
- **Filter**:
  - Kategorie: Gap
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
