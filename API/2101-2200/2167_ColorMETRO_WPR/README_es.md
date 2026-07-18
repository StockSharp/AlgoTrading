# Estrategia ColorMETRO WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador ColorMETRO Williams %R, que construye líneas escalonadas rápidas y lentas alrededor del oscilador Williams %R.
La línea rápida reacciona rápidamente a los cambios de precio, mientras que la línea lenta suaviza el ruido. Las decisiones de trading se toman cuando estas líneas
se cruzan entre sí, señalando posibles cambios en el impulso. Cuando la línea rápida cruza por debajo de la línea lenta, la estrategia asume que el
mercado está sobrevendido y entra en posición larga. Por el contrario, cuando la línea rápida cruza por encima de la línea lenta, entra en posición corta.
Las posiciones existentes se cierran cuando se detecta la condición opuesta.

La gestión de riesgos se maneja a través de niveles de take-profit y stop-loss basados en porcentajes. Esto permite que la estrategia se adapte a los niveles de precio
en diferentes instrumentos. El marco temporal de velas predeterminado es de ocho horas, lo que ayuda a filtrar la volatilidad intradía y
centrarse en las tendencias a mediano plazo. La lógica funciona en ambos lados del mercado, habilitando operaciones largas y cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la `Línea rápida` cruza **por debajo** de la `Línea lenta`.
  - **Corto**: la `Línea rápida` cruza **por encima** de la `Línea lenta`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: la `Línea rápida` sube por encima de la `Línea lenta`.
  - **Corto**: la `Línea rápida` cae por debajo de la `Línea lenta`.
- **Stops**: Sí, take-profit y stop-loss basados en porcentajes.
- **Valores predeterminados**:
  - `WprPeriod` = 7.
  - `FastStep` = 5.
  - `SlowStep` = 15.
  - `TakeProfitPercent` = 4.
  - `StopLossPercent` = 2.
  - `CandleType` = velas de 8 horas.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Único (basado en Williams %R).
  - Stops: Sí.
  - Complejidad: Medio.
  - Marco temporal: Medio plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
