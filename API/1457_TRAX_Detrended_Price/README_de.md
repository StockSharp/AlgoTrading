# TRAX Detrendeter Preis-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die TRAX- und DPO-Oszillatoren verwendet, um Trendumkehrungen zu handeln.

## Details
- **Einstiegskriterien**: DPO kreuzt TRAX mit TRAX-Vorzeichen und SMA-Filter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzsignale.
- **Stops**: Keine.
- **Standardwerte**: TRAX-Länge 12, DPO-Länge 19, SMA-Bestätigungslänge 3.
- **Filter**: TRAX-Vorzeichen und Bestätigungs-SMA.
