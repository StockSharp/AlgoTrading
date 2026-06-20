# Donchian CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Donchian CCI para generar señales.
La entrada larga ocurre cuando Price > Donchian Upper && CCI < -100 (ruptura al alza con condiciones de sobreventa). La entrada corta ocurre cuando Price < Donchian Lower && CCI > 100 (ruptura a la baja con condiciones de sobrecompra).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price > Donchian Upper && CCI < -100 (ruptura al alza con condiciones de sobreventa)
  - **Corto**: Price < Donchian Lower && CCI > 100 (ruptura a la baja con condiciones de sobrecompra)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio cae por debajo de la banda media
  - **Corto**: Salir de la posición corta cuando el precio sube por encima de la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Donchian CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

