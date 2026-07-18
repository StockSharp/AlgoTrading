# Selbstlernende Experten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie lernt aus historischen binären Preismustern und schätzt die Wahrscheinlichkeit einer zukünftigen Aufwärts- oder Abwärtsbewegung. Wenn die Wahrscheinlichkeit einen benutzerdefinierten Schwellenwert überschreitet, eröffnet die Strategie eine Marktposition in dieser Richtung. Die gesammelten Statistiken zerfallen im Laufe der Zeit über einen Vergesslichkeitsfaktor, um dem jüngsten Verhalten mehr Gewicht zu geben. Das System kann optional Stop-Niveaus verschieben, wenn neue Signale erscheinen, und unterstützt einen Trailing-Stop basierend auf Preisschritten.

## Details

- **Einstiegskriterien**:
  - **Long**: Wahrscheinlichkeit einer Aufwärtsbewegung ≥ `ProbabilityThreshold`.
  - **Short**: Wahrscheinlichkeit einer Abwärtsbewegung ≥ `ProbabilityThreshold`.
- **Stops**: Optionaler Trailing-Stop mit symmetrischem Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `PatternSize` = 10
  - `ProbabilityThreshold` = 0.8
  - `ForgetRate` = 1.05
  - `Trailing` = 0 (deaktiviert)
- **Filter**:
  - Kategorie: Mustererkennung
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Optional
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
