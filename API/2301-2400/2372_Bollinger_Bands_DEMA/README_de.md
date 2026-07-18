# Bollinger Bands mit DEMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert Bollinger Bands, die auf 30-Minuten-Kerzen berechnet werden, mit einem Double Exponential Moving Average (DEMA) aus Tagesdaten, um Ausbrüche mit Trendbestätigung zu handeln.

Ein Long-Setup tritt auf, wenn eine bullische Kerze das untere Band nach oben kreuzt, während die DEMA steigt und damit den Aufwärtsimpuls bestätigt. Ein Short-Setup tritt auf, wenn eine bärische Kerze das obere Band nach unten kreuzt, während die DEMA fällt. Positionen werden geschlossen, wenn eine Kerze der entgegengesetzten Farbe das äußere Band gegen die Handelsrichtung kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Die Kerze schließt über dem unteren Band und öffnet darunter UND die tägliche DEMA steigt drei aufeinanderfolgende Tage.
  - **Short**: Die Kerze schließt unter dem oberen Band und öffnet darüber UND die tägliche DEMA fällt drei aufeinanderfolgende Tage.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: Eine bärische Kerze schließt unter dem oberen Band, nachdem sie darüber geöffnet hat.
  - **Short**: Eine bullische Kerze schließt über dem unteren Band, nachdem sie darunter geöffnet hat.
- **Stops**: Keine.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `DemaPeriod` = 20
  - `Deviation` = 2
  - `CandleType` = 30-Minuten-Zeitrahmen
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, DEMA
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Intraday mit täglichem Trendfilter
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
