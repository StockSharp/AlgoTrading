# Elegante Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Bollinger Bands-Ausbrüche. Sie kauft, wenn der Schlusskurs über das obere Band kreuzt, und verkauft, wenn der Schlusskurs unter das untere Band kreuzt.

## Details

- **Einstiegsbedingungen**:
  - **Long**: Schlusskurs kreuzt über das obere Band
  - **Short**: Schlusskurs kreuzt unter das untere Band
- **Ausstiegsbedingungen**: umgekehrter Kreuzungspunkt
- **Typ**: Ausbruch
- **Indikatoren**: Bollinger Bands
- **Zeitrahmen**: 1 Minute (Standard)
- **Stops**: keine

**Prüfbare Regel und Standardwerte.** Die Bollinger-Bänder verwenden die letzten 20 abgeschlossenen Schlusskurse **einschließlich der Signalkerze** und das Populations-Standardabweichungsmaß mit Faktor 2. `BollingerPeriod=20`, `DeviationMultiplier=2`, `CandleType=1 Minute`. Ohne Position wird long eröffnet, wenn der Schlusskurs das obere Band von unten überschreitet, und short, wenn er das untere Band von oben unterschreitet. Die **gesamte Long-Position** schließt bei Rückkreuzung unter das obere Band; die **gesamte Short-Position** bei Rückkreuzung über das untere Band. Für das vorige und aktuelle Kreuzungssignal werden die jeweils abgeschlossenen Kerzen verwendet. Keine Stops, Gewinnziele oder Umkehr auf derselben Kerze.
