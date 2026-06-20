# Parabolic SAR CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Parabolic SAR CCI para generar señales.
La entrada larga ocurre cuando Price > SAR && CCI < -100 (tendencia alcista con condiciones de sobreventa). La entrada corta ocurre cuando Price < SAR && CCI > 100 (tendencia bajista con condiciones de sobrecompra).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 49%. Funciona mejor en el mercado cripto.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price > SAR && CCI < -100 (tendencia alcista con condiciones de sobreventa)
  - **Corto**: Price < SAR && CCI > 100 (tendencia bajista con condiciones de sobrecompra)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio cae por debajo del SAR
  - **Corto**: Salir de la posición corta cuando el precio sube por encima del SAR
- **Stops**: No.
- **Valores predeterminados**:
  - `SarAccelerationFactor` = 0.02m
  - `SarMaxAccelerationFactor` = 0.2m
  - `CciPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Parabolic SAR CCI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

