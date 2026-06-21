# Trendless AG Histogram-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt Umkehrungen, die vom **Trendless AG Histogram**-Indikator erkannt werden. Der Indikator misst den Abstand zwischen dem Preis und einem geglätteten gleitenden Durchschnitt und glättet das Ergebnis erneut, um ein Histogramm um null zu bilden. Lokale Minima weisen auf potenzielle Aufwärtsumkehrungen hin, während lokale Maxima auf Abwärtsumkehrungen hindeuten.

Positionen werden eröffnet, wenn das Histogramm die Richtung wechselt. Wenn der Indikator steigt, nachdem er unter früheren Werten lag, wird eine Long-Position eröffnet. Wenn er fällt, nachdem er über früheren Werten lag, wird eine Short-Position eröffnet. Optionale Stop-Loss- und Take-Profit-Niveaus managen das Risiko.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Histogrammwert steigt, während der vorherige Wert niedriger als sein Vorgänger war.
  - **Short**: Der Histogrammwert fällt, während der vorherige Wert höher als sein Vorgänger war.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal oder Stop-Loss/Take-Profit-Niveaus.
- **Stops**: Fester Stop-Loss und Take-Profit in Preiseinheiten.
- **Standardwerte**:
  - `Fast Length` = 7.
  - `Slow Length` = 5.
  - `Stop Loss` = 1000.
  - `Take Profit` = 2000.
  - `Candle Type` = 12-Stunden-Kerzen.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: Benutzerdefinierter Indikator auf Basis gleitender Durchschnitte.
  - Stops: Ja.
  - Komplexität: Moderat.
  - Zeitrahmen: Mittelfristig.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Ja.
  - Risikolevel: Mittel.
