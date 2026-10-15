# MFI-Strategie mit Überverkauft-Zonen-Ausstieg und Mittelung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie wartet darauf, dass der Money Flow Index (MFI) die überverkaufte Zone betritt. Sobald der MFI über das Überverkauft-Niveau steigt, wird eine limitierte Kauforder zu einem festen Prozentsatz unterhalb des aktuellen Schlusskurses platziert. Wird die Order nicht innerhalb einer bestimmten Anzahl von Bars ausgeführt, wird sie storniert. Stop-Loss und Take-Profit werden über StartProtection angewendet.

## Details

- **Einstiegskriterien**:
  - MFI steigt nach dem Unterschreiten über `MfiOversoldLevel`; Limit-Kauf wird `LongEntryPercentage` unterhalb des Schlusskurses platziert.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Position wird durch Take-Profit oder Stop-Loss geschlossen (`ExitGainPercentage`, `StopLossPercentage`).
- **Stops**: Ja, über StartProtection.
- **Standardwerte**:
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: MFI
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
