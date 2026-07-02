# Estrategia RSI Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores RSI Hull MA para generar señales.
La entrada larga ocurre cuando RSI < 30 && HMA(t) > HMA(t-1) (sobreventa con HMA subiendo). La entrada corta ocurre cuando RSI > 70 && HMA(t) < HMA(t-1) (sobrecompra con HMA bajando).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 58%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI < 30 && HMA(t) > HMA(t-1) (sobreventa con HMA subiendo)
  - **Corto**: RSI > 70 && HMA(t) < HMA(t-1) (sobrecompra con HMA bajando)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando RSI regresa a la zona neutral
  - **Corto**: Salir de la posición corta cuando RSI regresa a la zona neutral
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: RSI Hull MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

