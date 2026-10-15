# CCI Automatizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

CCI Automatizado es una estrategia de reversión que reacciona a los cruces de umbral del Índice de Canal de Materias Primas (CCI). Va largo cuando el CCI sube por encima de −80 después de caer por debajo de −90, y va corto cuando el CCI cae por debajo de 80 después de superar 90. El sistema duplica las operaciones hasta un límite definido por el usuario, gestiona el riesgo con niveles fijos de take-profit y stop-loss, y sigue los beneficios con un stop de arrastre configurable.

El enfoque busca capturar cambios tempranos de momentum después de condiciones de sobrecompra o sobreventa. Al acumular múltiples posiciones y mover el stop a medida que el precio avanza, intenta capitalizar reversiones sostenidas mientras limita el riesgo a la baja.

## Detalles

- **Criterios de entrada**: CCI cruza por encima de -80 después de estar por debajo de -90 para largos; cruza por debajo de 80 después de estar por encima de 90 para cortos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss, take profit o stop de arrastre.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `CciPeriod` = 9
  - `TradesDuplicator` = 3
  - `Volume` = 0.03
  - `StopLoss` = 50
  - `TakeProfit` = 200
  - `TrailingStop` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
