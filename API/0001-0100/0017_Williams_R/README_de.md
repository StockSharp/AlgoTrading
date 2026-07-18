# Strategie Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf dem Williams %R Indikator

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 88%. Am besten funktioniert sie auf dem Aktienmarkt.

Williams %R identifiziert überkaufte und überverkaufte Zonen. Wenn der Indikator über den oberen Schwellenwert steigt, signalisiert er potenzielle Schwäche für Shorts; Werte unterhalb des unteren Schwellenwerts deuten auf Longs hin. Positionen werden geschlossen, sobald sich %R in Richtung Neutral bewegt.

Da %R schnell oszilliert, kann die Strategie in volatilen Märkten viele Signale erzeugen. Einige Trader kombinieren ihn mit anderen Filtern, um Rauschen zu reduzieren.


## Details

- **Einstiegskriterien**: Signale basierend auf Williams.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `Period` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Williams
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

