# Estrategia RSI ProPlus de Mercado Bajista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra cuando el RSI cruza por encima de un nivel especificado y sale con un porcentaje fijo desde el precio de entrada. Está diseñada para condiciones de mercado bajista con expectativa de rebotes rápidos.

## Detalles

- **Criterios de entrada**: RSI cruza por encima del nivel
- **Largo/Corto**: Largo
- **Criterios de salida**: Take profit a un porcentaje desde la entrada
- **Stops**: No
- **Valores predeterminados**:
  - `RSI Period` = 11
  - `RSI Level` = 8
  - `Take Profit %` = 0.11
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
