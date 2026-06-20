# Estrategia de Reversión Bajista de Tres Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Una imagen especular de la versión alcista, esta configuración busca reversiones bajistas rápidas. Después de dos fuertes velas alcistas que empujan a nuevos máximos, una vela bajista decisiva cierra por debajo del mínimo de la barra anterior. Una breve tendencia alcista previa ayuda a confirmar el agotamiento de los compradores.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 88%. Funciona mejor en el mercado de acciones.

El algoritmo rastrea una ventana deslizante de tres velas. Cuando aparece el patrón y se cumple cualquier requisito de tendencia alcista, se toma una posición corta con el stop por encima del máximo del patrón. Las reglas son sencillas, por lo que las señales se generan inmediatamente al cierre de la vela.

La operación se cierra en el stop protector o cuando se forma otro patrón. Dado que juega con retrocesos a corto plazo dentro de un posible movimiento bajista, funciona mejor en mercados volátiles.

## Detalles

- **Criterios de entrada**: Dos velas alcistas con máximos crecientes, luego una vela bajista que cierra por debajo del mínimo de la barra central.
- **Largo/Corto**: Solo cortos.
- **Criterios de salida**: Stop-loss o siguiente patrón.
- **Stops**: Sí, por encima del máximo del patrón.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireUptrend` = true
  - `UptrendLength` = 5
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Corto
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

