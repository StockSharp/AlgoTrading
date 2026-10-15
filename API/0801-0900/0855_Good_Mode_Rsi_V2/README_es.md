# Estrategia Good Mode RSI v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en extremos del RSI con umbrales personalizados de toma de ganancias y stop trailing. Vende cuando el RSI supera un nivel alto y cierra cuando el RSI cae a un valor de toma de ganancias. Compra cuando el RSI cae a un nivel bajo y cierra cuando el RSI sube al objetivo de ganancias. En ambos casos, un stop trailing sigue el precio más favorable para proteger las ganancias.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI < buy level`.
  - **Corto**: `RSI > sell level`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: `RSI > take profit level buy` o stop trailing activado.
  - **Corto**: `RSI < take profit level sell` o stop trailing activado.
- **Stops**: Stop trailing en ticks.
- **Valores predeterminados**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
