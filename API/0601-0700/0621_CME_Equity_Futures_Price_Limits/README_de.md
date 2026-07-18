# CME Aktienindex-Futures Preislimit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet die täglichen Preislimit-Niveaus für CME-Aktienindex-Futures. Sie erfasst einen Referenzpreis zu einer bestimmten Stunde und berechnet die Limit-up/down-Niveaus (+/-5%) sowie die -7%, -13% und -20% Limit-down-Niveaus. Die Ergebnisse werden zur Überwachung in das Log geschrieben.

## Parameter

- **ManualReference** – manueller Referenzpreis-Override (0 zum Deaktivieren).
- **ShowLimitDownLevels** – Protokollierung der -7/-13/-20%-Niveaus aktivieren.
- **OffsetHour** – Stunde (0-23) zur Erfassung des Referenzpreises.
- **CandleType** – zu verarbeitender Kerzentyp (Standard: 1 Minute).
