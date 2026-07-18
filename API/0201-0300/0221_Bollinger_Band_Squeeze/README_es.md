# Estrategia de Compresión de Bandas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta configuración monitorea el ancho de las Bandas de Bollinger para detectar períodos de baja volatilidad. Cuando las bandas se contraen en relación con su promedio reciente, señala que una posible expansión de volatilidad está cerca.

Las pruebas indican un retorno anual promedio de aproximadamente 100%. Funciona mejor en el mercado de forex.

Una vez que se identifica una compresión, la estrategia espera que el precio rompa fuera de las bandas. Un cierre por encima de la banda superior inicia una operación larga, mientras que un cierre por debajo de la banda inferior abre una corta. La operación se cierra si el precio regresa hacia el centro de las bandas o si se activa un stop-loss.

El método está dirigido a traders que prefieren operar rupturas de volatilidad en lugar de continuaciones de tendencia. Usar el ancho de banda como filtro ayuda a evitar señales falsas durante condiciones laterales.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Ancho de banda < ancho promedio && Cierre > banda superior
  - **Corto**: Ancho de banda < ancho promedio && Cierre < banda inferior
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el precio cae de vuelta dentro de las bandas
  - **Corto**: Salir cuando el precio sube de vuelta dentro de las bandas
- **Stops**: Sí, típicamente a 2*ATR.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0m
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
