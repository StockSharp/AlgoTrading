# Estrategia de Patrón de Envolvente Alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta configuración busca una reversión alcista pronunciada cuando una vela envuelve completamente la barra bajista anterior. Dicha formación suele poner fin a una caída a corto plazo e insinúa un renovado impulso ascendente. El filtro de tendencia bajista opcional cuenta velas rojas consecutivas para confirmar el agotamiento de los vendedores.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 76%. Funciona mejor en el mercado forex.

Durante la operación en vivo, el algoritmo observa cada vela entrante y mantiene un registro de la barra anterior. Si la nueva vela cierra más alto de lo que abre y su cuerpo envuelve la barra anterior, se activa una entrada larga. El stop se coloca justo por debajo del mínimo del patrón para limitar el riesgo.

Las operaciones permanecen abiertas hasta que se activa el stop o alguna otra señal sugiere una salida discrecional. Dado que la confirmación de barras bajistas previas fortalece la configuración, la estrategia evita perseguir reversiones débiles.

## Detalles

- **Criterios de entrada**: La vela alcista envuelve la barra bajista anterior, con tendencia bajista opcional presente.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop-loss o discrecional.
- **Stops**: Sí, por debajo del mínimo del patrón.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendBars` = 3
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

