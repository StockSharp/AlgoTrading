# Altcoin-Index-Korrelations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie vergleicht EMA-Trends des gehandelten Instruments und eines Referenzindex. Sie eröffnet Long-Positionen, wenn beide schnellen EMAs über ihren langsamen EMAs liegen, und Short-Positionen, wenn beide darunter liegen. Optionale inverse Logik ermöglicht das Handeln gegen den Indextrend oder das vollständige Überspringen des Index.

## Details

- **Einstiegskriterien**:
  - Schneller EMA über dem langsamen EMA bei beiden Instrumenten (oder umgekehrt bei Inversion).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzungsbedingung.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
