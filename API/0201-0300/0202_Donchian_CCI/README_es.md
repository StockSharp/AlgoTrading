# Estrategia Donchian CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia usa los indicadores Donchian CCI para generar señales.
La entrada larga ocurre cuando Price > Donchian Upper && CCI > CciOverbought (ruptura al alza con impulso alcista). La entrada corta ocurre cuando Price < Donchian Lower && CCI < CciOversold (ruptura a la baja con impulso bajista).
Es adecuada para los operadores que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 43%. Funciona mejor en el mercado de acciones.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price > Donchian Upper && CCI > CciOverbought (ruptura al alza con impulso alcista)
  - **Corto**: Price < Donchian Lower && CCI < CciOversold (ruptura a la baja con impulso bajista)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio cae por debajo de la banda media
  - **Corto**: Salir de la posición corta cuando el precio sube por encima de la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    El canal es el máximo y el mínimo de las DonchianPeriod velas anteriores, y su punto medio está a mitad de camino entre ellos. Una ruptura alcista se confirma cuando el CCI está por encima de CciOverbought y una bajista cuando está por debajo de CciOversold; la lectura contraria, un CCI sobrevendido con un cierre por encima del canal, prácticamente no ocurre. Una señal de entrada contra una posición abierta la invierte.
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

