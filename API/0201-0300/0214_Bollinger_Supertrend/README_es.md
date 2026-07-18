# Estrategia Bollinger Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia combina las Bandas de Bollinger con el indicador Supertrend para identificar entradas durante movimientos direccionales fuertes. Las Bandas de Bollinger miden la expansión de volatilidad mientras que la línea Supertrend sigue la tendencia general y actúa como stop dinámico.

Las pruebas indican un rendimiento anual promedio de aproximadamente 79%. Funciona mejor en el mercado de acciones.

Una operación larga se activa cuando el precio cierra por encima de la Banda de Bollinger superior y permanece por encima de la línea Supertrend, confirmando el alineamiento del momentum y la tendencia. Una operación corta ocurre cuando el precio cierra por debajo de la banda inferior mientras se mantiene bajo el nivel Supertrend. Las operaciones se cierran una vez que el precio cruza de vuelta a través del Supertrend, indicando que el momentum ha desaparecido.

Debido a que el sistema espera rupturas más allá de la volatilidad normal, es adecuado para traders que buscan capturar movimientos sostenidos en lugar de reversiones rápidas. El stop Supertrend se ajusta dinámicamente a los movimientos del mercado, ayudando a gestionar el riesgo sin intervención manual.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Close > upper Bollinger Band && Close > Supertrend
  - **Corto**: Close < lower Bollinger Band && Close < Supertrend
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el precio cruza por debajo del Supertrend
  - **Corto**: Salir cuando el precio cruza por encima del Supertrend
- **Stops**: Sí, vía stop dinámico Supertrend.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Supertrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

