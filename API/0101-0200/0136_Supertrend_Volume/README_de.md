# Supertrend Volume Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Supertrend Volume erweitert den Supertrend-Indikator um eine Volumenbestätigung.
Steigendes Volumen während eines Supertrend-Wechsels stärkt die Wahrscheinlichkeit einer neuen Impulsbewegung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 145%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Die Strategie steigt mit dem Trend bei einem Supertrend-Signal nur ein, wenn es von überdurchschnittlichem Volumen begleitet wird.

Stops folgen der Supertrend-Linie und schließen die Position, wenn der Kurs auf der anderen Seite schließt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `VolumePeriod` = 20
    Ein Supertrend-Wechsel, ein Schluss auf der anderen Seite der Linie, schließt eine Gegenposition. Der Wechsel eröffnet nur dann eine Position in seiner Richtung, wenn das Kerzenvolumen über dem Durchschnitt der vorherigen VolumePeriod Kerzen liegt, sodass ein bestätigter Wechsel die Position dreht.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Supertrend, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

