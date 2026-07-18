# Estrategia BHS System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

El BHS System es un enfoque de ruptura que convierte el asesor experto original de MetaTrader 5 en la API de alto nivel de StockSharp. La estrategia observa la relación entre el precio y una Media Móvil Adaptativa de Kaufman (AMA). Cuando la barra actual cierra por encima del AMA, el sistema se prepara para unirse a una ruptura alcista; cuando el cierre cae por debajo del AMA, se prepara para una expansión bajista. En lugar de entrar inmediatamente, el algoritmo espera a que el precio toque niveles de "números redondos" predefinidos y envía órdenes stop en esos niveles. Esto mantiene el comportamiento de la estrategia portada idéntico a la versión MQL donde las órdenes pendientes siempre estaban alineadas con límites de precio redondeados.

## Lógica de trading

1. En cada vela completada, la estrategia calcula los próximos niveles de precio redondo más alto y más bajo. El redondeo usa el paso definido por el usuario (en puntos) y el paso de precio del instrumento para producir precios de activación exactos compatibles con el exchange.
2. El valor AMA anterior (desplazado un bar, como en la implementación MQL original) se compara con el cierre de la vela actual.
3. Si no hay posición abierta y no hay orden de entrada activa:
   - Cuando cierre > AMA, se coloca un buy stop en el nivel de techo redondeado.
   - Cuando cierre < AMA, se coloca un sell stop en el nivel de piso redondeado.
4. Las órdenes pendientes expiran automáticamente después del número configurado de horas. Esto refleja el campo de vida útil de la solicitud de orden MT5.
5. Cuando se ejecuta una orden de entrada, la orden pendiente opuesta se cancela y se registra una orden stop de protección usando la distancia de stop-loss seleccionada. El sistema entonces monitorea el movimiento del precio y mueve el stop de acuerdo con los parámetros de trailing.
6. Los trailing stops solo se ajustan cuando el precio ha avanzado al menos la distancia de trailing más el paso de trailing. Esto evita modificaciones constantes y refleja la lógica de trailing discreta en el código MT5.

## Gestión de riesgo

- **Stop-loss inicial:** Distancias separadas basadas en puntos para operaciones largas y cortas se convierten en offsets de precio absolutos y se usan para colocar órdenes stop de protección inmediatamente después de la entrada.
- **Trailing stop:** Las posiciones largas y cortas tienen distancias de trailing independientes. Los stops se actualizan solo cuando el nuevo stop mejora al menos el paso de trailing, evitando micro-ajustes en mercados tranquilos.
- **Expiración de órdenes:** Ambas órdenes de entrada almacenan su tiempo de creación. Si la orden permanece activa después del número especificado de horas, se cancela para evitar exposición pendiente obsoleta.

## Parámetros

- `OrderVolume` – tamaño de lote usado tanto para entradas como para órdenes de protección.
- `StopLossBuyPoints` / `StopLossSellPoints` – distancia de stop-loss en puntos para posiciones largas y cortas respectivamente.
- `TrailingStopBuyPoints` / `TrailingStopSellPoints` – distancia de trailing stop para posiciones largas y cortas expresada en puntos.
- `TrailingStepPoints` – brecha adicional (en puntos) requerida antes de que el trailing stop pueda mejorarse nuevamente.
- `RoundStepPoints` – número de puntos usados al construir niveles de activación redondeados.
- `ExpirationHours` – vida útil de una orden de entrada pendiente. Cuando se establece en cero, las órdenes nunca expiran automáticamente.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – parámetros de la Media Móvil Adaptativa de Kaufman usada como filtro direccional.
- `CandleType` – tipo de dato/marco temporal de velas que impulsa la estrategia.

## Notas de implementación

- La estrategia usa el indicador `KaufmanAdaptiveMovingAverage` de StockSharp y un espacio de nombres de ámbito de archivo consistente con las pautas del repositorio.
- Todas las operaciones de trading dependen de ayudantes de API de alto nivel (`BuyStop`, `SellStop`, `CancelOrder`) y no se recuperan valores de indicadores a través de llamadas `GetValue`.
- El soporte de gráficos está habilitado: la suscripción dibuja velas, la línea AMA y las operaciones propias cuando hay un contexto de gráfico disponible.
- La lógica de protección se consolida en una referencia de orden stop única, de modo que el mecanismo de trailing reutiliza el stop original en lugar de generar órdenes adicionales.
- La conversión mantiene comentarios en inglés y preserva el comportamiento de la rutina de trailing MQL original usando las mismas verificaciones de umbral.
