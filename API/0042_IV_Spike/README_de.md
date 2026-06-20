# Implied Volatility Spike
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie beobachtet die implizite Volatilität auf plötzliche Sprünge relativ zum vorherigen Wert. Ein starker Spike kombiniert mit einem Preis, der gegen den gleitenden Durchschnitt handelt, kann eine kurzfristige Umkehr signalisieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 163%. Am besten funktioniert es im Aktienmarkt.

Wenn die implizite Volatilität um den konfigurierten Schwellenwert steigt, tritt das System in die entgegengesetzte Richtung der Preisbewegung ein und erwartet eine Rückkehr der Volatilität.

Positionen werden geschlossen, sobald die Volatilität zu fallen beginnt oder ein Stop-Loss ausgelöst wird.

## Details

- **Einstiegskriterien**: IV Spike über `IVSpikeThreshold` und Preis relativ zum MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: IV fällt oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `IVPeriod` = 20
  - `IVSpikeThreshold` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: IV, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

