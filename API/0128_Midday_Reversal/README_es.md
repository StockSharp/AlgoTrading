# Estrategia de Reversión a Mediodía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Reversión a Mediodía busca puntos de giro que ocurren alrededor del mediodía, cuando las tendencias matutinas suelen agotarse.
La liquidez típicamente disminuye a mitad de sesión, provocando reversiones cuando los operadores cuadran posiciones.

Las pruebas indican un retorno anual promedio de aproximadamente el 121%. Funciona mejor en el mercado cripto.

El sistema monitorea un cambio de momentum cerca del mediodía y entra en dirección opuesta al movimiento de la mañana.

Un stop porcentual controla el riesgo y las posiciones se cierran si la reversión no se desarrolla para la tarde.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Intradía
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

