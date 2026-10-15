# Estrategia de Cruce Lineal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula una regresión lineal del precio basada en el volumen para producir un precio predicho. Se abre una posición larga cuando el precio predicho cruza por encima de su media móvil ponderada y la línea MACD sube por encima de su señal. Se abre una posición corta cuando la línea MACD cae por debajo de su señal y los mínimos recientes son decrecientes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio predicho cruza por encima de su WMA y el MACD sube por encima de la señal.
  - **Corto**: El MACD cae por debajo de la señal y los mínimos hacen mínimos más bajos.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Ninguno; las posiciones se invierten con señales opuestas.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 21.
  - `LinearLength` = 9.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Linear Regression, WMA, MACD
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
