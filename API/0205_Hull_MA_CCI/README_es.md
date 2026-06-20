# Hull MA CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Hull MA CCI para generar señales.
La entrada larga ocurre cuando HMA(t) > HMA(t-1) && CCI < -100 (HMA subiendo con condiciones de sobreventa). La entrada corta ocurre cuando HMA(t) < HMA(t-1) && CCI > 100 (HMA bajando con condiciones de sobrecompra).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 52%. Funciona mejor en el mercado cripto.

## Detalles
- **Criterios de entrada**:
  - **Largo**: HMA(t) > HMA(t-1) && CCI < -100 (HMA subiendo con condiciones de sobreventa)
  - **Corto**: HMA(t) < HMA(t-1) && CCI > 100 (HMA bajando con condiciones de sobrecompra)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando HMA comienza a bajar
  - **Corto**: Salir de la posición corta cuando HMA comienza a subir
- **Stops**: Sí.
- **Valores predeterminados**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Hull MA CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

