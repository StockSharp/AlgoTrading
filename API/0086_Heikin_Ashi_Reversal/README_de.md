# Heikin-Ashi-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi-Kerzen glätten das Rauschen und heben die Trendrichtung hervor. Ein Wechsel von einer Serie bärischer HA-Kerzen zu einer bullischen oder umgekehrt kann einen Schwungwechsel anzeigen. Diese Strategie handelt diese Farbwechsel und nutzt einen prozentualen Stop für den Schutz.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 145%. Am besten funktioniert die Strategie auf dem Kryptomarkt.

Die Logik berechnet Heikin-Ashi-Werte aus regulären Kerzen. Wenn der HA-Schlusskurs nach einer bärischen Sequenz den HA-Eröffnungskurs überschreitet, wird Long gegangen. Ein Kreuz darunter nach einem bullischen Lauf öffnet eine Short-Position. Der Stop wird in einem festen Prozentsatz vom Einsteig entfernt platziert.

Die Methode ist einfach, aber effektiv bei unruhigen Schwankungen, wenn traditionelle Kerzendiagramme rauschreich sind.

## Details

- **Einstiegskriterien**: Heikin-Ashi-Kerze ändert die Farbe.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Heikin-Ashi
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

