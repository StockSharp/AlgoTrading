# Bollinger Bands Abstand-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie für den Handel von Bollinger-Bands-Umkehrungen mit einem zusätzlichen Abstandsfilter. Verkauft, wenn der Preis über dem oberen Band plus einem festgelegten Abstand schließt, und kauft, wenn er unter dem unteren Band minus demselben Abstand schließt. Positionen werden durch ein Gewinnziel oder einen Stop-Loss in Preisschritten geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Schluss unterhalb des unteren Bollinger Bands minus Abstand
  - Short: Schluss oberhalb des oberen Bollinger Bands plus Abstand
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Gewinnziel erreicht
  - Stop-Loss erreicht
- **Stops**: Absolut in Preisschritten
- **Standardwerte**:
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
