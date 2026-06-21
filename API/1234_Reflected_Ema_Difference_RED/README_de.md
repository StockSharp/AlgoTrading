# Reflected EMA Difference RED-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie spiegelt die Distanz zwischen zwei Hull Moving Averages und verfolgt einen geglätteten Wert. Wenn die geglättete Reflexion um einen bestimmten Prozentsatz umkehrt, werden entsprechend Long- oder Short-Positionen eingegangen.

## Details

- **Einstiegskriterien**:
  - Long: die geglättete Reflexion steigt über ihre Rückzugslimit.
  - Short: die geglättete Reflexion fällt unter ihre Rückzugslimit.
- **Long/Short**: Beide
- **Standardwerte**:
  - `Smoothing Period` = 2
  - `Change Percent` = 0.04
