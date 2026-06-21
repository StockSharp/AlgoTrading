# Estrategia J-Lines Ribbon Motor de 4 Ciclos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia J-Lines Ribbon 4-Cycle Engine clasifica el mercado en ciclos CHOP, LONG y SHORT usando una cinta de EMAs y el Average Directional Index. Las entradas ocurren en nuevas detecciones de ciclo y rebotes desde EMAs clave, mientras las salidas se activan en cruces opuestos o rupturas de swing.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Nuevo ciclo LONG o rebote por encima de EMA72/EMA126 mientras EMA72 está por encima de EMA89.
  - **Corto**: Nuevo ciclo SHORT o rebote por debajo de EMA72/EMA126 mientras EMA72 está por debajo de EMA89.
- **Stops**: Último máximo/mínimo de swing.
- **Valores predeterminados**:
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ADX
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
