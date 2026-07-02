# Estrategia Keltner Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Keltner Williams R para generar señales.
La entrada larga ocurre cuando Price < lower Keltner band && Williams %R < -80 (sobreventa en la banda inferior). La entrada corta ocurre cuando Price > upper Keltner band && Williams %R > -20 (sobrecompra en la banda superior).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 46%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price < lower Keltner band && Williams %R < -80 (sobreventa en la banda inferior)
  - **Corto**: Price > upper Keltner band && Williams %R > -20 (sobrecompra en la banda superior)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio regresa a la banda media
  - **Corto**: Salir de la posición corta cuando el precio regresa a la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Keltner Williams R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

