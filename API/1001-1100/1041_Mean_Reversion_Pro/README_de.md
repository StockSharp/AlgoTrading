# Mean Reversion Pro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean Reversion Pro ist ein Mean-Reversion-System für wichtige Indizes. Es verwendet zwei gleitende Durchschnitte und Intrabar-Bereiche, um Rücksetzer zu erkennen. Long-Trades werden bevorzugt, da Indizes tendenziell nach oben tendieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter schnellem SMA, Schlusskurs unter 20%-Bereichsebene, Schlusskurs über langsamem SMA, keine Position.
  - **Short**: Schlusskurs über schnellem SMA, Schlusskurs über 80%-Bereichsebene, Schlusskurs unter langsamem SMA, keine Position.
- **Long/Short**: Beide (Long empfohlen).
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs kreuzt schnellen SMA nach oben.
  - **Short**: Schlusskurs kreuzt schnellen SMA nach unten.
- **Stops**: Keine.
- **Standardwerte**:
  - `Fast SMA` = 5
  - `Slow SMA` = 100
  - `Direction` = Nur Long
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Konfigurierbar
  - Indikatoren: SMA
  - Stops: Keine
  - Komplexität: Einfach
  - Zeitrahmen: Täglich
