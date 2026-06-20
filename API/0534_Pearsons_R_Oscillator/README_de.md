# Pearson's R-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Pearson's R-Oszillator-Strategie sucht dynamisch nach dem Zeitraum, in dem der Preis am besten in einen linearen Regressionskanal passt, indem der Pearson-Korrelationskoeffizient verwendet wird. Wenn die Korrelation den angegebenen positiven oder negativen Schwellenwert erreicht, bildet die Strategie einen Regressionskanal und handelt Ausbrüche.

Positionen werden eröffnet, wenn der Preis die Kanalgrenzen kreuzt, und können bei Mittellinienkreuzungen geschlossen werden. Der Ansatz passt sich den Marktbedingungen an, indem das Analysefenster automatisch auf die stärkste Korrelation eingestellt wird.

## Details

- **Einstiegskriterien**:
  - Preis kreuzt über die obere Regressionslinie → **Long**.
  - Preis kreuzt unter die untere Regressionslinie → **Short**.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Mittellinienkreuzung in entgegengesetzter Richtung.
- **Stops**: Keine.
- **Standardwerte**:
  - `MinPeriod` = 48
  - `MaxPeriod` = 360
  - `Step` = 12
  - `IdealPositive` = 0.85
  - `IdealNegative` = -0.85
  - `Deviations` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Pearson's R, Lineare Regression
  - Stops: Keine
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
