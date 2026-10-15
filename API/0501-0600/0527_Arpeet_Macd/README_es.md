# Estrategia Arpeet MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Arpeet MACD opera cruces de MACD con un filtro de línea cero. Una señal larga aparece cuando la línea MACD cruza por encima de la línea de señal mientras permanece por debajo de cero. Una señal corta ocurre cuando el MACD cruza por debajo de la línea de señal por encima de cero.

## Detalles

- **Criterios de entrada**:
  - **Largo**: MACD cruza por encima de la señal y MACD < 0.
  - **Corto**: MACD cruza por debajo de la señal y MACD > 0.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
