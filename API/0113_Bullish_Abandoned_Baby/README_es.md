# Estrategia Bebé Abandonado Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Bebé Abandonado Alcista es un patrón raro de tres velas que presenta un doji con gap bajista seguido de un gap alcista.
Esta formación deja la vela del medio "abandonada" y a menudo precede a una fuerte reversión al alza.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 76%. Funciona mejor en el mercado forex.

La estrategia compra en la apertura de la tercera vela una vez que supera con gap al doji, anticipando un fuerte seguimiento a medida que los cortos cubren sus posiciones.

Los stops se ubican justo por debajo del mínimo del doji, asegurando que las pérdidas sean pequeñas si la reversión no se sostiene.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
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

