# Estrategia de Debilidad del Lunes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Debilidad del Lunes señala que las acciones suelen abrir más bajas después del fin de semana, mientras los operadores digieren noticias y reposicionan sus carteras.
La presión bajista a corto plazo puede aparecer al inicio de la semana antes de que los mercados se estabilicen.

Las pruebas indican un retorno anual promedio de aproximadamente el 106%. Funciona mejor en el mercado de acciones.

La estrategia vende en corto en la apertura del lunes y cubre al cierre, buscando beneficiarse de esa debilidad inicial.

Los stops se mantienen ajustados para evitar pérdidas si el mercado rompe la tendencia y sube.

## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
    Los días son días UTC, ya que el mercado opera las 24 horas; las entradas ocurren al cierre de la primera vela del día. La venta en corto se abre en la primera vela del lunes y se cubre en la última vela del lunes.
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

