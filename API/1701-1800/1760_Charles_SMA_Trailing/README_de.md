# Charles SMA Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mithilfe des Kreuzens zweier einfacher gleitender Durchschnitte und optionalem Trailing-Stop-Management. Wenn der schnelle SMA den langsamen SMA von unten kreuzt, wird eine Long-Position eröffnet. Eine Short-Position wird eröffnet, wenn der schnelle SMA den langsamen SMA von oben kreuzt. Die Strategie unterstützt festen Stop-Loss, Take-Profit und einen Trailing-Stop, der nach einem vordefinierten Gewinnschwellenwert aktiviert wird.

## Details

- **Einstiegskriterien**:
  - Schneller SMA kreuzt langsamen SMA von unten → Long eröffnen.
  - Schneller SMA kreuzt langsamen SMA von oben → Short eröffnen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Umgekehrtes Kreuzen.
  - Stop-Loss oder Take-Profit erreicht.
  - Trailing-Stop ausgelöst, wenn der Gewinn `TrailStart` erreicht und um `TrailingAmount` nachzieht.
- **Stops**:
  - `StopLoss` definiert einen festen Schutz-Stop in Preiseinheiten.
  - `TakeProfit` definiert ein festes Gewinnziel.
  - `TrailStart` und `TrailingAmount` steuern den Trailing-Stop.
- **Standardwerte**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `StopLoss` = 0
  - `TakeProfit` = 25
  - `TrailStart` = 25
  - `TrailingAmount` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
