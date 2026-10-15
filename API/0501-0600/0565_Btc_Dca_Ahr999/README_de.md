# BTC DCA AHR999-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft Bitcoin jeden Montag zwischen dem konfigurierten Start- und Enddatum. Der investierte Betrag hängt vom AHR999-Index ab, der ein geometrisches Mittel des Preises mit einem logarithmischen Wachstumsmodell für Bitcoin kombiniert.

## Details

- **Einstiegskriterien**:
  - An Montagen innerhalb des Datumsbereichs, wenn AHR999 < 0.45, den Betrag `UsdInvest2` kaufen.
  - An Montagen innerhalb des Datumsbereichs, wenn AHR999 < 1.2, den Betrag `UsdInvest1` kaufen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Positionen werden gehalten; keine automatische Ausstiegslogik enthalten.
- **Stops**: Keine.
- **Standardwerte**:
  - UsdInvest1 = 100.
  - UsdInvest2 = 1000.
  - Length = 200.
  - Startdatum = 2024-02-01, Enddatum = 2025-12-31.
- **Filter**:
  - Kategorie: Akkumulation.
  - Richtung: Long.
  - Indikatoren: AHR999.
  - Stops: Nein.
  - Komplexität: Moderat.
  - Zeitrahmen: Täglich.
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Mittel.
