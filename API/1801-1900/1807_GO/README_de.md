# GO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet einen zusammengesetzten **GO**-Wert auf Basis exponentieller gleitender Durchschnitte (EMA) der Eröffnungs-, Hoch-, Tief- und Schlusskurse multipliziert mit dem Volumen. Handelsentscheidungen werden anhand des Vorzeichens und des Niveaus des GO-Wertes getroffen.

## Formel

`GO = ((C - O) + (H - O) + (L - O) + (C - L) + (C - H)) * V`

Dabei gilt:
- `C`, `O`, `H`, `L` – EMA-Werte der Schluss-, Eröffnungs-, Hoch- und Tief-Kurse.
- `V` – Volumen der verarbeiteten Kerze.

## Handelsregeln

- **Long eröffnen**: GO > `OpenLevel`
- **Short eröffnen**: GO < `-OpenLevel`
- **Long schließen**: GO < (`OpenLevel` - `CloseLevelDiff`)
- **Short schließen**: GO > -(`OpenLevel` - `CloseLevelDiff`)

## Parameter

| Name | Beschreibung |
|------|--------------|
| `MaPeriod` | EMA-Periode für die Preisglättung. |
| `OpenLevel` | GO-Niveau zum Auslösen neuer Positionen. |
| `CloseLevelDiff` | Differenz zwischen Eröffnungs- und Schließniveau. |
| `ShowGo` | Ob GO-Werte protokolliert werden. |
| `CandleType` | Art der für die Verarbeitung verwendeten Kerzen. |

Die Strategie arbeitet auf abgeschlossenen Kerzen und verwendet Marktorders für das Positionsmanagement.
