# Estrategia de Vela de 30 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque compara el precio de apertura de la vela actual de 30 minutos con el cierre de la vela anterior.
Si una nueva vela abre por encima del cierre anterior, se abre una posición larga.
Cuando ya se está largo y la siguiente vela abre por debajo del cierre anterior, la estrategia se invierte a una posición corta.
Todas las posiciones abiertas se cierran un minuto antes de que termine la vela actual.

## Detalles

- **Criterios de entrada**:
  - **Largo**: apertura de la vela actual > cierre de la vela anterior.
  - **Corto**: apertura de la vela actual < cierre de la vela anterior mientras se mantiene una posición larga.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cerrar cualquier posición un minuto antes de que cierre la vela.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(30).TimeFrame().
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Price action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
