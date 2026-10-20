# BTC DCA AHR999

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Verwendet abgeschlossene UTC-Tageskerzen. Die letzten `Length` positiven Schlusskurse einschließlich des aktuellen ergeben `G=exp(mean(ln(C)))`. Das BTC-Alter sind ganze Tage seit 2009-01-03: `F=10^(5.84*log10(age)-17.01)`, `AHR999=(C/G)*(C/F)`. Das vollständige Fenster ist erforderlich.

An einem zulässigen Montag wird bei einem Index strikt unter 0.45 das Budget `UsdInvest2` gekauft, sonst bei strikt unter 1.2 das Budget `UsdInvest1`. Andere Werte, Wochentage und Daten erzeugen keinen Kauf. Die UTC-Datumsgrenzen sind einschließlich; je Tageskerze gibt es höchstens eine Entscheidung. Menge: `floor(budget/C/VolumeStep)*VolumeStep`; Mengen unter dem Minimum entfallen. Das Budget verwendet den Signalschluss, der Marktpreis kann abweichen. Offene Orders blockieren weitere Aufträge. Bestände werden ohne automatischen Ausstieg angesammelt.

## Parameter

| Parameter | Standardwert |
|---|---|
| `CandleType` | `1d` |
| `Length` | `200` |
| `UsdInvest1` | `100` |
| `UsdInvest2` | `1000` |
| `StartDate` | `2024-02-01T00:00:00Z` |
| `EndDate` | `2025-12-31T00:00:00Z` |

## Quellen

- [AHR999 geometric-cost implementation](https://github.com/alibaba-web3/quant-master/blob/main/ahr999_index.py)
