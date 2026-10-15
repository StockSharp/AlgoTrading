# Liquiditäts-Grab-Volumen-Falle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wartet auf einen bärischen Liquiditäts-Grab bei flachem Volumen, der eine Fair-Value-Gap bildet. Wenn der Kurs über die Gap-Oberkante schließt und das Volumen nahe seinem gleitenden Durchschnitt bleibt, wird eine limitierte Kauforder an der Gap-Unterkante mit symmetrischem Stop-Loss und Take-Profit platziert.

## Details

- **Einstiegsbedingung**: `Close[2] < Open[1]` && `Close > High[1]` && bärischer Bruch bei flachem Volumen
- **Ausstiegskriterien**: Stop-Loss unterhalb des Gap-Bodens um die Gap-Höhe, Take-Profit bei `High[1]`
- **Typ**: Umkehr
- **Indikatoren**: Volumen SMA
- **Zeitrahmen**: 1 Minute (Standard)
