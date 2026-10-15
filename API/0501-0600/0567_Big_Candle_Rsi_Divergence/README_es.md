# Divergencia RSI de Vela Grande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Identifica velas inusualmente grandes en relación con las cinco barras anteriores y compara los valores de RSI rápido y lento. Las operaciones siguen la dirección de la vela y usan un stop trailing retrasado que se activa solo después de que el precio se mueva un número determinado de ticks en beneficio.

El stop trailing comienza una vez alcanzado el umbral de beneficio y luego sigue el precio a una distancia fija, mientras que un stop fijo inicial protege la operación desde el inicio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El cuerpo de la vela actual es mayor que los cinco anteriores y cierra al alza.
  - **Corto**: El cuerpo de la vela actual es mayor que los cinco anteriores y cierra a la baja.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop inicial o stop trailing alcanzado.
- **Stops**: Sí, stop trailing retrasado.
- **Valores predeterminados**:
  - `TrailStartTicks` = 200
  - `TrailDistanceTicks` = 150
  - `InitialStopLossTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
