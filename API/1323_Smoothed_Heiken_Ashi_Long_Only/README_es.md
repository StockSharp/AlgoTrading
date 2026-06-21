# Estrategia Heiken Ashi Suavizado Solo Largos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo largos que utiliza velas Heikin-Ashi suavizadas. Compra cuando la vela suavizada cambia de rojo a verde y sale cuando vuelve a ponerse roja.

## Detalles

- **Criterios de entrada**: El HA suavizado cambia de rojo a verde
- **Largo/Corto**: Solo largos
- **Criterios de salida**: El HA suavizado se vuelve rojo
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `EmaLength` = 10
  - `SmoothingLength` = 10
