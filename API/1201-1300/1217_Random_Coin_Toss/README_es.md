# Estrategia de Lanzamiento de Moneda Aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia experimental lanza una moneda cada N barras y entra largo o corto según el resultado. El riesgo se gestiona mediante niveles de stop-loss y take-profit basados en ATR.

Las pruebas indican un rendimiento anual promedio de aproximadamente 8%. Funciona mejor en el mercado de criptomonedas.

La idea es proporcionar una base de referencia para entradas aleatorias manteniendo salidas disciplinadas.

## Detalles

- **Criterios de entrada**: Cada `EntryFrequency` barras se lanza una moneda; cara va largo, cruz va corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Alcanza el stop-loss o take-profit.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrLength` = 14
  - `SlMultiplier` = 1m
  - `TpMultiplier` = 2m
  - `EntryFrequency` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Experimental
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

