# Scalping-Strategie von TradingConToto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Scalping-Strategie von TradingConToto zieht Linien zwischen aufeinanderfolgenden Pivot-Hochs oder Pivot-Tiefs, abhängig vom EMA-Trend. Wenn der Kurs während eines Aufwärtstrends eine absteigende Pivot-Hoch-Linie nach oben durchbricht, eröffnet die Strategie eine Long-Position. Wenn der Kurs während eines Abwärtstrends eine aufsteigende Pivot-Tief-Linie nach unten durchbricht, wird Short gegangen. Der Handel ist nur während einer festgelegten Sitzung erlaubt.

## Details

- **Einstiegskriterien**: Aufwärtstrend mit Kursausbruch über eine absteigende Pivot-Hoch-Linie für Long; Abwärtstrend mit Kursausbruch unter eine aufsteigende Pivot-Tief-Linie für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Take-Profit und Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `Pivot` = 16
  - `Pips` = 64
  - `Spread` = 0
  - `Session` = "0830-0930"
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, Pivot
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
