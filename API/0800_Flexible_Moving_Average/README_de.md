# Flexible Gleitender Durchschnitt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Passt die Position basierend auf Kreuzungen zwischen dem Schlusskurs der vorherigen Periode und einem konfigurierbaren gleitenden Durchschnitt an. Ein Kreuzung nach unten reduziert die Position um einen benutzerdefinierten Prozentsatz, während eine Kreuzung nach oben die volle Position wiederherstellt.

## Details

- **Einstiegskriterien**:
  - **Initial**: Optionaler vollständiger Long auf der ersten Kerze.
  - **Erhöhung**: Vorheriger Schlusskurs kreuzt über den gleitenden Durchschnitt → Position auf 100%.
- **Ausstiegskriterien**:
  - **Reduzierung**: Vorheriger Schlusskurs kreuzt unter den gleitenden Durchschnitt → Reduzierung um `SellPercentage`.
- **Indikatoren**:
  - Einfacher, exponentieller, gewichteter, Hull- oder geglätteter gleitender Durchschnitt.
- **Stops**: Keine.
- **Standardwerte**:
  - `MaLength` = 200
  - `SellPercentage` = 100
  - `MaMethod` = SMA
  - `AllowInitialBuy` = true
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: Gleitende Durchschnitte
  - Stops: keine
  - Komplexität: Grundlegend

