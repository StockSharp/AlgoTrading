# Reversión por Anuncio de Resultados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de **Reversión por Anuncio de Resultados** vende en corto a los ganadores recientes y compra a los perdedores recientes en los días de anuncio de resultados.

## Detalles
- **Criterios de entrada**: En el día de resultados, ir corto en acciones con retornos recientes positivos y comprar las de retornos negativos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Posición ajustada tras la señal; sin regla de tenencia explícita.
- **Stops**: No.
- **Valores predeterminados**:
  - `LookbackDays = 5`
  - `HoldingDays = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Event-driven
  - Dirección: Ambos
  - Indicadores: Returns
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
