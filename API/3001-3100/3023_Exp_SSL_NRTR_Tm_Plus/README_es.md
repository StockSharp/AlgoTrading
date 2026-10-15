# Estrategia Exp SSL NRTR Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia replica el asesor experto de MetaTrader "Exp_SSL_NRTR_Tm_Plus" usando la infraestructura de alto nivel de StockSharp. Se
suscribe a un solo marco temporal, calcula el canal SSL NRTR con un método de suavizado configurable y reacciona a las transiciones
de color proporcionadas por el indicador. Las entradas largas se activan cuando el canal se vuelve alcista mientras que las entradas cortas
ocurren en transiciones bajistas. La implementación preserva los controles de riesgo originales, los filtros de operaciones opcionales y la salida basada en temporizador.

## Parámetros

| Grupo | Parámetro | Descripción |
| --- | --- | --- |
| Trading | Money Management | Fracción del portafolio (o lotes directos cuando es negativo/modo `Lot`) usada para dimensionar órdenes. |
| Trading | Margin Mode | Modo usado para traducir el valor de gestión de dinero a un tamaño de posición. Los modos distintos de `Lot` se aproximan con cálculos basados en el portafolio. |
| Trading | Allow Long/Short Entries | Habilitar o deshabilitar la apertura de posiciones en la dirección respectiva. |
| Trading | Allow Long/Short Exits | Permitir a la estrategia cerrar posiciones en la dirección respectiva en reversiones del indicador. |
| Risk | Stop Loss | Distancia de stop protector en pasos de precio. La estrategia monitorea los niveles en lugar de colocar órdenes de stop nativas. |
| Risk | Take Profit | Distancia de take profit en pasos de precio. |
| Risk | Slippage | Parámetro informacional mantenido del EA original. |
| Risk | Use Time Exit | Habilitar el temporizador que fuerza una posición plana tras el período de mantenimiento configurado. |
| Risk | Exit Minutes | Período de mantenimiento en minutos para la salida basada en tiempo. |
| Data | Candle Type | Marco temporal de trabajo usado tanto para trading como para cálculos del indicador. |
| Indicator | Smoothing Method | Tipo de media móvil usado por el canal SSL NRTR. Los tipos personalizados no soportados recurren a una EMA. |
| Indicator | Length | Período base del algoritmo de suavizado. |
| Indicator | Phase | Parámetro auxiliar usado por promedios adaptativos (T3, VIDYA, AMA). |
| Indicator | Signal Bar | Número de barras cerradas hacia atrás al evaluar los colores SSL. |

## Lógica de trading

1. Suscribirse al marco temporal configurado y procesar solo las velas terminadas.
2. Calcular las medias móviles SSL NRTR y derivar el color del canal (arriba, abajo o neutral).
3. Cuando el color cambia a alcista (`0`), opcionalmente cerrar posiciones cortas y, si está habilitado, abrir una posición larga.
4. Cuando el color cambia a bajista (`2`), opcionalmente cerrar posiciones largas y, si está habilitado, abrir una posición corta.
5. Rastrear los niveles de stop-loss/take-profit usando el precio de entrada y cerrar la posición cuando se alcance cualquier nivel.
6. Opcionalmente cerrar posiciones una vez que el tiempo de mantenimiento supere el parámetro `Exit Minutes`.
7. Prevenir entradas repetidas dentro de la misma barra con la lógica del "nivel de tiempo" original de MT5.

## Gestión de dinero

- El modo `Lot` trata el valor de gestión de dinero como un volumen directo expresado en lotes/contratos.
- `FreeMargin` y `Balance` aproximan la fracción de capital solicitada dividiéndola por el último precio de cierre.
- `LossFreeMargin` y `LossBalance` estiman el volumen negociable a partir de la pérdida permitida por operación usando la distancia de stop-loss configurada.
- Los valores negativos de gestión de dinero siempre se asignan a un tamaño de lote absoluto.

## Notas

- Solo los métodos de suavizado disponibles en StockSharp se implementan directamente. `Jurx` y `Parma` recurren a la media móvil exponencial y este comportamiento está documentado en comentarios del código.
- La estrategia mantiene la lógica de stop-loss y take-profit dentro del bucle de la estrategia en lugar de enviar órdenes de protección nativas para mantenerse agnóstica a la plataforma.
- El deslizamiento es un parámetro informacional mantenido para completitud; las órdenes se envían como órdenes de mercado simples.
- La implementación dibuja velas y operaciones propias en el área del gráfico por defecto.
