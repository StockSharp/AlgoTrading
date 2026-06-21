# Modulare Range-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie zielt auf seitwärts laufende Märkte mit zwei Modulen ab, die nicht gleichzeitig aktiv sein können. Das erste Modul basiert auf MACD-Momentum-Bestätigung mit RSI und Bollinger-Bänder-Mean-Reversion. Das zweite Modul kauft oder verkauft an Extremen, wenn der Preis innerhalb der Bollinger-Bänder zurückprallt und RSI überverkaufte oder überkaufte Niveaus zeigt. ATR-basierte Stops und optionale Ausstiege über Bollinger-Bänder oder RSI-Umkehrungen steuern das Risiko.

## Details

- **Einstiegskriterien**:
  - **Logik 1 Long**: ADX unter Schwellenwert, MACD kreuzt über Signallinie, RSI über seiner SMA, Preis unter mittlerem Bollinger-Band.
  - **Logik 1 Short**: ADX unter Schwellenwert, MACD kreuzt unter Signallinie, RSI unter seiner SMA, Preis über mittlerem Bollinger-Band.
  - **Logik 2 Long**: ADX unter Schwellenwert, Preis kreuzt zurück über unteres Band, RSI unter überverkauftem Niveau.
  - **Logik 2 Short**: ADX unter Schwellenwert, Preis kreuzt zurück unter oberes Band, RSI über überkauftem Niveau.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - ATR-Stop-Loss.
  - Optionale Bollinger- oder RSI-Signale je nach aktiver Logik.
- **Stops**: ATR-Vielfache.
- **Standardwerte**: Bollinger 20/2, RSI 14, MACD 12/26/9, ATR 14, ADX 14.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
