# Donchian-Kanalsystem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Donchian-Kanalsystem** handelt Ausbrüche des Donchian-Kanals mit einer optionalen Verschiebung zur Vermeidung von Vorausschauentzerrung.

## Funktionsweise
- **Long-Einstieg**: wenn der Schlusskurs die obere Donchian-Bande kreuzt, die vor `Shift` Bars berechnet wurde.
- **Short-Einstieg**: wenn der Schlusskurs die untere Donchian-Bande kreuzt, die vor `Shift` Bars berechnet wurde.
- Positionen werden beim gegenteiligen Ausbruch umgekehrt.

## Parameter
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## Indikatoren
- Donchian-Kanal
