# Options-Strategie V1.3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine EMA-Crossover-Strategie mit RSI, ATR-basiertem Stop und Take-Profit sowie Volumenfilter. Das System kann optional einen Ausbruch aus der Opening Range verlangen und schließt Positionen um 15:55 Uhr New Yorker Zeit. Der Handel wird während vordefinierter Sitzungen und eines benutzerdefinierten Nicht-Handelszeitraums gesperrt.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurze EMA kreuzt über die lange EMA, RSI ≥ `RsiLongThreshold`, Volumen ≥ Volumen-SMA, optional Schlusskurs > Opening-Range-Hoch.
  - **Short**: Kurze EMA kreuzt unter die lange EMA, RSI ≤ `RsiShortThreshold`, Volumen ≥ Volumen-SMA, optional Schlusskurs < Opening-Range-Tief.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss und Take-Profit.
  - Entgegengesetzter EMA-Kreuzung.
  - Automatisches Schließen um 15:55 Uhr NY-Zeit.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaShortLength = 8`
  - `EmaLongLength = 28`
  - `RsiLength = 12`
  - `AtrLength = 14`
  - `SlMultiplier = 1.4`
  - `TpSlRatio = 4`
  - `VolumeMaLength = 20`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: EMA, RSI, ATR, SMA
  - Stops: Ja
  - Zeitrahmen: Intraday
