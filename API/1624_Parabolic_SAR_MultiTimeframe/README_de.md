# Parabolic SAR Multi-Zeitrahmen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Parabolic SAR Multi-Zeitrahmen verwendet vier verschiedene Parabolic SAR-Indikatoren aus höheren Zeitrahmen,
um einen Trend zu bestätigen, bevor in einen Handel eingestiegen wird. Die Strategie verarbeitet 15-Minuten-Kerzen und prüft den
Zustand des SAR auf 30-Minuten-, 1-Stunden- und 4-Stunden-Charts. Eine Long-Position wird nur eröffnet, wenn der Preis
über allen SAR-Werten liegt; eine Short-Position wird eröffnet, wenn der Preis unter allen SARs liegt.

Die Methode versucht, Rauschen herauszufiltern, indem eine Ausrichtung über mehrere Zeitrahmen erforderlich ist. Die Position
wird geschlossen, wenn die entgegengesetzte Bedingung erscheint.

## Details

- **Einstiegskriterien**: Preis relativ zum Parabolic SAR auf 15m/30m/1h/4h-Zeitrahmen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal von allen SAR-Indikatoren.
- **Stops**: Verwendet `StartProtection` für Grundschutz, keine expliziten Stop-Werte.
- **Standardwerte**:
  - `Step15` = 0.062
  - `Step30` = 0.058
  - `Step60` = 0.058
  - `Step240` = 0.058
  - `MaxStep` = 0.1
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (15m-Basis mit höheren Bestätigungen)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Verwendung

1. Hängen Sie die Strategie an ein Wertpapier an.
2. Passen Sie bei Bedarf die SAR-Schrittparameter an.
3. Starten Sie die Strategie; sie abonniert automatisch 15m-, 30m-, 1h- und 4h-Kerzen.
