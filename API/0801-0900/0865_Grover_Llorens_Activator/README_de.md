# Grover Llorens Activator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptive Trailing-Strategie auf ATR-Basis, die die Richtung wechselt, wenn der Preis die interne Aktivierungslinie kreuzt.

Kauft, wenn die Differenz zwischen Preis und Trailing-Linie über null kreuzt. Verkauft, wenn sie unter null kreuzt.

## Details

- **Einstiegskriterien**: Preiskreuzung der aus ATR berechneten Trailing-Stop-Linie.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length` = 480
  - `Multiplier` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
