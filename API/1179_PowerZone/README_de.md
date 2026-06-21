# PowerZone Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie identifiziert "PowerZone"-Orderblöcke, die durch eine bearishe Kerze gefolgt von aufeinanderfolgenden bullischen Kerzen entstehen (und umgekehrt). Ein Ausbruch über die Bull-Zone löst einen Long-Trade aus, während ein Einbruch unter die Bear-Zone eine Short-Position eröffnet. Ziele und Stops basieren auf dem Bereich der Zone.

## Details

- **Einstiegskriterien**:
  - **Long**: Bearishe Kerze vor `Periods+1` Bars gefolgt von `Periods` bullischen Kerzen und Preis bricht über das Zonenhoch.
  - **Short**: Bullische Kerze vor `Periods+1` Bars gefolgt von `Periods` bearishen Kerzen und Preis bricht unter das Zonentief.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Take-Profit und Stop-Loss als Vielfache des Zonenbereichs.
- **Indikatoren**: Keine.
- **Standardwerte**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
