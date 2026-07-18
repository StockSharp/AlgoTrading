# Estrategia de Valor Book-to-Market
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Book-to-Market Value** demuestra la configuración de parámetros del universo y la suscripción a velas diarias para el factor book-to-market.
Este ejemplo es un marcador de posición y actualmente no contiene lógica de trading.

## Detalles
- **Criterios de entrada**: Lógica del factor no implementada.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Ninguno.
- **Stops**: No.
- **Valores predeterminados**:
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Fundamental
  - Dirección: Ambos
  - Indicadores: Fundamentals
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
