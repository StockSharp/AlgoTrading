# Estrategia X Trader V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces entre dos medias móviles del precio medio. La primera media móvil es más larga y desplazada, mientras que la segunda es corta. Se abre una posición larga cuando la primera media móvil cruza por debajo de la segunda y permanece por debajo durante dos barras tras estar por encima dos barras antes. Se abre una posición corta en el cruce opuesto. Las posiciones pueden cerrarse con señales inversas. El trading se limita a una ventana horaria intradía específica. Están disponibles stops protectores opcionales.

## Detalles

- **Criterios de entrada**:
  - SMA del precio medio(`Ma1Period`) cruza por debajo de la SMA del precio medio(`Ma2Period`) y permanece por debajo durante dos barras ⇒ comprar cuando `AllowBuy` es verdadero.
  - SMA del precio medio(`Ma1Period`) cruza por encima de la SMA del precio medio(`Ma2Period`) y permanece por encima durante dos barras ⇒ vender cuando `AllowSell` es verdadero.
  - Tiempo de la vela entre `StartTime` y `EndTime`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto cuando `CloseOnReverseSignal` es verdadero.
- **Stops**:
  - Take profit y stop loss opcionales en ticks mediante `TakeProfitTicks` y `StopLossTicks`.
- **Valores predeterminados**:
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **Filtros**:
  - Categoría: Cruce
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
