# Gold-Scalping-Strategie BOS & CHoCH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Break-of-Structure (BOS)- und Change-of-Character (CHoCH)-Muster bei Gold. Sie leitet kurzfristige Unterstützungs- und Widerstandsniveaus ab und steigt ein, wenn einem BOS unmittelbar ein CHoCH folgt, unter Verwendung dynamischer Stop-Loss- und Take-Profit-Ziele.

## Details

- **Einstiegskriterien**:
  - **Long**: `High > LastSwingHigh` und `Close` kreuzt über `LastSwingLow`
  - **Short**: `Low < LastSwingLow` und `Close` kreuzt unter `LastSwingHigh`
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Dynamisch
- **Standardwerte**:
  - `RecentLength` = 10
  - `SwingLength` = 5
  - `TakeProfitFactor` = 2
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: Highest, Lowest
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
