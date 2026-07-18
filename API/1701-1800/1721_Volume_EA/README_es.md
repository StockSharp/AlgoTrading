# Estrategia Volume EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia opera basándose en picos de volumen y el Índice de Canal de Materias Primas (CCI). Abre posiciones al inicio de una nueva hora cuando el volumen de la vela anterior supera al de la vela previa por un factor configurable. Los valores del CCI deben caer dentro de bandas específicas para confirmar la señal.

## Reglas
- Solo hay una posición abierta a la vez.
- Al comienzo de cada hora:
  - **Entrada larga** cuando:
    - La vela anterior es alcista.
    - Volumen anterior > volumen previo × `Factor`.
    - El CCI está entre `CciLevel1` y `CciLevel2`.
  - **Entrada corta** cuando:
    - La vela anterior es bajista.
    - Volumen anterior > volumen previo × `Factor`.
    - El CCI está entre `CciLevel4` y `CciLevel3`.
- Un stop de seguimiento de `TrailingStop` pasos de precio protege las ganancias.
- Todas las posiciones se cierran cuando la hora es igual a 23.

## Parámetros
- `Factor` – umbral multiplicador de volumen.
- `TrailingStop` – distancia de seguimiento en pasos de precio.
- `CciLevel1` / `CciLevel2` – límites del CCI para operaciones largas.
- `CciLevel3` / `CciLevel4` – límites del CCI para operaciones cortas.
- `CandleType` – marco temporal de velas utilizado para los cálculos.
