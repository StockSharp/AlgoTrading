# Strategie der Vorfeiertagsstärke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Vorfeiertagsstärke bezeichnet die bullische Tendenz kurz vor großen Marktfeiertagen, wenn das Volumen geringer und die Stimmung optimistisch ist.
Trader positionieren sich oft im Voraus und treiben die Kurse in der letzten oder den letzten beiden Handelssitzungen höher.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 109%. Sie funktioniert am besten im Kryptomarkt.

Die Strategie geht am Tag vor dem Feiertag long und schließt die Position in der darauffolgenden Sitzung oder zum Börsenschluss, um diesen kurzfristigen Bias zu nutzen.

Ein enger Stop wird verwendet, falls der erwartete Anstieg ausbleibt.

## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

