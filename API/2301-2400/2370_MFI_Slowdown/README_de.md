# MFI-Verlangsamung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie überwacht den Money Flow Index (MFI) auf einem höheren Zeitrahmen und reagiert, wenn er extreme Zonen erreicht. Wenn `SeekSlowdown` aktiviert ist, wird ein Signal nur bestätigt, wenn sich der MFI-Wert zwischen zwei aufeinanderfolgenden Bars um weniger als einen Punkt ändert. Bei einem aufwärtsgerichteten Signal schließt sie Short-Positionen und eröffnet optional eine neue Long-Position; bei einem abwärtsgerichteten Signal schließt sie Long-Positionen und kann eine Short-Position eröffnen. Das Risikomanagement wird durch StartProtection gehandhabt.

## Details

- **Einstiegskriterien**:
  - Aufwärtssignal: `MFI >= UpperThreshold` und (keine Verlangsamungsprüfung oder Verlangsamung erkannt).
  - Abwärtssignal: `MFI <= LowerThreshold` und (keine Verlangsamungsprüfung oder Verlangsamung erkannt).
- **Long/Short**: Beide, je nach Parametern.
- **Ausstiegskriterien**:
  - Gegenteiliges Signal schließt die Position.
  - Stop-Loss und Take-Profit über `StopLossPercent` und `TakeProfitPercent`.
- **Stops**: Ja, über StartProtection.
- **Standardwerte**:
  - `MfiPeriod` = 2
  - `UpperThreshold` = 90
  - `LowerThreshold` = 10
  - `SeekSlowdown` = true
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = 6-Stunden-Zeitrahmen
  - `BuyPosOpen` = `BuyPosClose` = `SellPosOpen` = `SellPosClose` = true
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MFI
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Optional (Verlangsamungsprüfung)
  - Risikolevel: Mittel
