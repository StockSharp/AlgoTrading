# ZPF-Volumen-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der ZPF-Volumen-Filter kombiniert zwei gleitende Durchschnitte mit einem Volumendurchschnitt. Der Indikatorwert ist die volumengeglättete Differenz zwischen einem schnellen und einem langsamen gleitenden Durchschnitt. Wenn dieser Wert die Nulllinie nach oben kreuzt, wird bullischer Druck angenommen; ein Kreuzen nach unten signalisiert bärischen Druck.

Die Strategie handelt in beide Richtungen. Einstiege erfolgen, wenn der ZPF-Indikator die Nulllinie kreuzt. Positionen werden bei einem entgegengesetzten Kreuzungssignal geschlossen.

## Details

- **Einstiegskriterien**: ZPF kreuzt die Nulllinie nach oben oder unten.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Kreuzungssignal an der Nulllinie.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Moving Average, Volume
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

