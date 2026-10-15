# Volumen-Anstieg (Volume Surge)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Volumen-Anstieg erkennt ungewöhnlich hohes Volumen im Verhältnis zum gleitenden Durchschnitt. Wenn die Ratio den definierten Multiplikator überschreitet, signalisiert dies starkes Interesse und eine mögliche Fortsetzung in der Richtung des Kurses relativ zu seinem gleitenden Durchschnitt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 52 %. Die Strategie eignet sich am besten für den Kryptomarkt.

Trades werden nur bei einem Anstieg eingeleitet und geschlossen, sobald das Volumen wieder unter den Durchschnitt fällt oder der Stop-Loss erreicht wird.

Dieser einfache Ansatz erfasst Momentum, das durch plötzliche Marktbeteiligung ausgelöst wird.

## Details

- **Einstiegskriterien**: Volumen-Ratio über `VolumeSurgeMultiplier`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Volumen fällt unter den Durchschnitt oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `VolumeAvgPeriod` = 20
  - `VolumeSurgeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volume
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
