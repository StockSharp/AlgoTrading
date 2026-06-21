# Rally Base Drop SND Pivots-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Rally Base Drop SND Pivots-Strategie handelt Ausbrüche aus Angebot- und Nachfrage-Pivot-Niveaus. Pivots werden erkannt, wenn Sequenzen aus bullischen und bärischen Kerzen Rally-Base-Drop- oder Drop-Base-Rally-Muster bilden. Wenn der Preis diese Pivot-Niveaus kreuzt, wird eine Position eröffnet. Der Ausstieg erfolgt über einen ATR-basierten Stop und ein Risiko-Rendite-Ziel.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis überschreitet ein Pivot-Hoch (oder Pivot-Tief bei Umkehrung).
  - **Short**: Preis unterschreitet ein Pivot-Tief (oder Pivot-Hoch bei Umkehrung).
- **Long/Short**: Konfigurierbar (nur Long, nur Short oder beide).
- **Ausstiegskriterien**:
  - Preis erreicht ATR-Stop oder Risiko-Rendite-Ziel.
- **Stops**: ATR-Multiplikator mit Risiko-Rendite-Ziel.
- **Standardwerte**:
  - `Length` = 3
  - `Mult` = 1.0
  - `RiskReward` = 6.0
  - `ReverseConditions` = false
- **Filter**:
  - Kategorie: Unterstützungs-/Widerstandsausbruch
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
