# DCA Simulation für CryptoCommunity-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie simuliert das Dollar-Cost-Averaging mit optionalen Sicherheitsorders und einem Trailing-Take-Profit. Sie beginnt mit einer Basisorder und kann periodisch zusätzliches Kapital investieren oder nach Kursrückgängen verbilligen.

## Details

- **Einstiegskriterien**:
  - Wenn keine Position offen ist und das Datum im konfigurierten Bereich liegt, wird ein Basisbetrag gekauft.
  - Optionale periodische DCA-Orders alle N Kerzen.
  - Optionale Sicherheitsorders, wenn der Preis um einen bestimmten Prozentsatz vom jüngsten Hoch fällt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Take-Profit bei einem Zielprozentsatz, optional mit Trailing-Stop.
- **Stops**: Take-Profit / Trailing-Stop.
- **Standardwerte**:
  - Basisorder = 100 USD.
  - DCA-Betrag = 10 USD alle 30 Kerzen.
  - Sicherheitsorder-Betrag = 100 USD bei 15% Preisabweichung.
  - Take-Profit = 1000%, Trailing = 25%.
  - Startdatum = 2021-11-01, Enddatum = 9999-01-01.
- **Filter**:
  - Kategorie: Akkumulation.
  - Richtung: Long.
  - Indikatoren: Keine.
  - Stops: Ja.
  - Komplexität: Moderat.
  - Zeitrahmen: Beliebig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
