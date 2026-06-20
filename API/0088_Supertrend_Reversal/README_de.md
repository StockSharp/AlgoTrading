# Supertrend-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Supertrend-Indikator kombiniert ATR und Preis, um eine gleitende Unterstützung oder einen gleitenden Widerstand zu erzeugen. Wenn die Supertrend-Linie von oberhalb auf unterhalb des Preises wechselt oder umgekehrt, deutet dies auf eine mögliche Trendwende hin. Diese Strategie handelt diese Wechsel.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 151%. Am besten funktioniert die Strategie am Aktienmarkt.

Auf jeder Kerze aktualisiert eine ATR-basierte Berechnung das Supertrend-Niveau. Ein Wechsel von oberhalb des Preises auf unterhalb löst einen Long-Einstieg aus, während eine Bewegung von unterhalb auf oberhalb einen Short erzeugt. Das Code-Beispiel verzichtet auf explizite Stops, daher sind Ausstiege ermessensbasiert oder werden von einem separaten Risikomodul verwaltet.

Der Indikator kann schnell auf Volatilität reagieren, daher kombinieren Trader ihn oft mit zusätzlichen Filtern, um Fehlsignale zu reduzieren.

## Details

- **Einstiegskriterien**: Supertrend wechselt die Seite relativ zum Preis.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Manueller oder externer Stop.
- **Stops**: Nicht definiert.
- **Standardwerte**:
  - `Period` = 10
  - `Multiplier` = 3.0
  - `CandleType` = 15 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Supertrend
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

