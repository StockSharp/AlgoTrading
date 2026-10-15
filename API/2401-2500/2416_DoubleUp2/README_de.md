# DoubleUp2 CCI MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DoubleUp2 ist eine Martingal-artige Strategie, die den Commodity Channel Index (CCI) und MACD kombiniert.
Sie öffnet Short-Positionen, wenn beide Indikatoren extreme positive Werte zeigen, und Long-Positionen, wenn beide extrem negativ sind.
Nach einem Verlust-Trade verdoppelt sich die Positionsgröße, um frühere Verluste zu kompensieren.
Gewinnbringende Trades werden geschlossen, sobald der Kurs um eine feste Anzahl von Punkten vorrückt.

## Details

- **Einstiegskriterien**:
  - **Long**: `CCI < -Threshold` und `MACD < -Threshold`.
  - **Short**: `CCI > Threshold` und `MACD > Threshold`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal oder Kurs bewegt sich `ExitDistance` Punkte im Gewinn.
- **Stops**: Kein expliziter Stop-Loss.
- **Standardwerte**:
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: CCI, MACD
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
