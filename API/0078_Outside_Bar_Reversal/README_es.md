# Estrategia de Reversión con Barra Exterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una barra exterior ocurre cuando el rango de una vela supera el de la vela anterior, creando un breve aumento de volatilidad. Esta estrategia opera en contra del movimiento si la barra exterior cierra en la dirección opuesta a la tendencia anterior, esperando un retorno hacia el equilibrio.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 121%. Funciona mejor en el mercado de criptomonedas.

Cuando se forma una barra exterior, el algoritmo determina si la vela es alcista o bajista. Una barra exterior alcista tras una caída abre una posición larga con un stop por debajo del mínimo de la barra. Una barra exterior bajista tras una subida activa un corto con un stop por encima de su máximo. Las operaciones salen si el precio posteriormente rompe ese extremo.

La configuración busca reversiones rápidas tras un impulso agotador y se usa mejor cuando los mercados están agitados en lugar de seguir una tendencia fuerte.

## Detalles

- **Criterios de entrada**: Barra exterior cerrando en dirección opuesta al movimiento anterior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio rompiendo el máximo/mínimo de la barra exterior o stop-loss.
- **Stops**: Sí, colocados más allá del patrón.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

