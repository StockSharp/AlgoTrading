# Estrategia Mejorada de Estructura de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estructura de Mercado Mejorada combina el análisis de máximos y mínimos de swing con filtros ATR, RSI, volumen, MACD y EMA. La estrategia entra en rupturas o reversiones de barrido cuando múltiples filtros confirman el momentum.

## Detalles

- **Criterios de entrada**: ruptura o barrido de swing reciente con filtros
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ATR, RSI, MACD, EMA, Volumen
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

