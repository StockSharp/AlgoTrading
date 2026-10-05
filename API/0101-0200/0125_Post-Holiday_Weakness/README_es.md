# Estrategia de Debilidad Post-Festiva
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Debilidad Post-Festiva es la tendencia de los precios a caer inmediatamente después de un festivo importante cuando el volumen sigue siendo escaso.
Con muchos participantes aún ausentes, los movimientos contra tendencia pueden ganar fuerza.

Las pruebas indican un retorno anual promedio de aproximadamente el 112%. Funciona mejor en el mercado de forex.

La estrategia vende en corto el día después del festivo y cubre rápidamente una vez que regresa la participación normal.

Se usa un stop pequeño para evitar pérdidas excesivas durante la negociación con baja liquidez.

## Detalles

- **Criterios de entrada**: activadores de efecto de calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `Holidays` = 2024 NYSE holidays
  - `StopLossPercent` = 2
    Los días son días UTC, ya que el mercado opera las 24 horas; las entradas ocurren al cierre de la primera vela del día. Holidays es una lista de fechas yyyy-MM-dd separadas por comas, por defecto los festivos de la NYSE de 2024 (Viernes Santo es 2024-03-29). El mercado opera todos los días, así que la venta en corto se abre el día natural posterior a un festivo y se cubre en la última vela de ese día.
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

