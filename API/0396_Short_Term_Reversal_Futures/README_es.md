# Estrategia de Reversión a Corto Plazo en Futuros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Reversión a Corto Plazo en Futuros** busca la reversión a la media en contratos de futuros. Cada día la estrategia identifica los contratos con el peor retorno durante la semana anterior y los compra, mientras vende los que más subieron, esperando un retroceso.

Las operaciones se mantienen durante unos días antes de cerrarse en la siguiente señal.

## Detalles
- **Criterios de entrada**: Clasificación diaria por retorno de la semana anterior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Posiciones cerradas tras un corto período de tenencia o cuando el ranking se actualiza.
- **Stops**: Se puede aplicar un stop basado en volatilidad.
- **Valores predeterminados**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Basados en precio
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
