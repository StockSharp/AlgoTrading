# GRIM309 CallPut-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die GRIM309 CallPut-Strategie handelt auf Basis der Ausrichtung mehrerer EMAs mit einem Warnsystem. Long-Positionen werden eröffnet, wenn kurzfristige EMAs einen Aufwärtstrend bestätigen und EMA5 über EMA10 steigt. Short-Positionen werden bei umgekehrten Bedingungen eröffnet. Eine Abkühlphase verhindert den sofortigen Wiedereinstieg nach einem Schließen. Eine zusätzliche Warnung löst frühzeitige Ausstiege aus, wenn sich der EMA5-EMA10-Spread schnell verengt.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: EMA10 über EMA20, Preis über EMA50, EMA5 steigt über EMA10, keine Position und Abkühlphase erfüllt.
  - **Short**: EMA10 unter EMA20, Preis unter EMA50, EMA5 fällt unter EMA10, keine Position und Abkühlphase erfüllt.
- **Ausstiegskriterien**: Preis kreuzt EMA15 oder Warnsignal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Ema5Length` = 5
  - `Ema10Length` = 10
  - `Ema15Length` = 15
  - `Ema20Length` = 20
  - `Ema50Length` = 50
  - `Ema200Length` = 200
  - `CooldownBars` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: EMA
  - Komplexität: Moderat
  - Risikolevel: Mittel
