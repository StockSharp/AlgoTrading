# Full Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Full Candle-Setup steigt ein, wenn eine Kerze über ihrem EMA schließt und auf der Ausbruchsseite nur einen kleinen Docht hinterlässt. Die Absicht ist es, Momentum-Kerzen zu handeln, die entschlossenes Handeln ohne viel Ablehnung zeigen. Optionale prozentbasierte Take-Profit- und Stop-Loss-Ausstiege verwalten den Trade, sobald er offen ist.

Das System eignet sich am besten für kurzfristige Ausbrüche, bei denen starke Kerzen oft zu einer schnellen Fortsetzung führen.

## Details

- **Einstiegskriterien**:
  - **Long**: bullische Kerze schließt über dem EMA mit Docht ≤ Schwellenwert
  - **Short**: bearishe Kerze schließt unter dem EMA mit Docht ≤ Schwellenwert
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Take-Profit- oder Stop-Loss-Prozentsätze, wenn aktiviert
- **Stops**: Optional
- **Standardwerte**:
  - `EmaLength` = 10
  - `ShadowPercent` = 5
  - `TPPercent` = 1.2
  - `SLPercent` = 1.8
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, price action
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
