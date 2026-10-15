# Exp X2MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Exp X2MA-Strategie handelt Wendepunkte eines doppelt geglätteten gleitenden Durchschnitts.
Der Preis wird zunächst mit einem einfachen gleitenden Durchschnitt und anschließend mit einem Jurik-Durchschnitt geglättet.
Wenn die geglättete Linie ein lokales Minimum bildet, kauft die Strategie und schließt Short-Positionen.
Wenn sie ein lokales Maximum bildet, verkauft die Strategie und schließt Long-Positionen.
Ein optionaler fester Stop-Loss und Take-Profit schützen offene Positionen.

## Details
- **Daten**: Preiskerzen (Standard 4 Stunden).
- **Einstiegskriterien**:
  - **Long**: Der vorherige X2MA-Wert ist niedriger als der ältere und der aktuelle Wert dreht nach oben.
  - **Short**: Der vorherige X2MA-Wert ist höher als der ältere und der aktuelle Wert dreht nach unten.
- **Ausstiegskriterien**: Entgegengesetztes Extrem, Stop-Loss oder Take-Profit.
- **Stops**: Fester Stop-Loss und Take-Profit in Punkten.
- **Standardwerte**:
  - `FirstMaLength` = 12
  - `SecondMaLength` = 5
  - `StopLossPoints` = 1000
  - `TakeProfitPoints` = 2000
- **Filter**:
  - Kategorie: Trendumkehr
  - Richtung: Long und Short
  - Indikatoren: SMA, JurikMovingAverage
  - Stops: Ja
  - Komplexität: Niedrig
  - Risikolevel: Mittel
