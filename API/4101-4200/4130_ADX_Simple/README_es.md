# ADX Estrategia de tendencia simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **ADX Estrategia de tendencias simple** es una adaptación directa del clásico asesor experto MetaTrader "ADX Simple". Sigue la dirección del índice direccional promedio (ADX) comparando los indicadores de movimiento direccional positivo y negativo (DI+ y DI-) y exigiendo que la línea principal ADX aumente antes de abrir cualquier operación. La versión StockSharp mantiene la naturaleza minimalista del sistema original al tiempo que lo adapta a patrones y controles de riesgo de alto nivel API.

## Pila de indicadores
- **Índice direccional promedio (ADX)** con período configurable (predeterminado 25).
  - Proporciona la línea **principal ADX** utilizada para confirmar la fuerza de la tendencia.
  - Proporciona valores **DI+** y **DI-** que definen el dominio alcista o bajista.
- **El plazo** se puede seleccionar hasta `CandleType` (el valor predeterminado es velas de 15 minutos).

## Generación de señal
### Entrada larga
1. Espere una vela terminada y un valor ADX finalizado.
2. Confirme que DI+ esté encima de DI- en la misma barra.
3. Requerir que la línea principal ADX sea estrictamente mayor que su valor anterior (la tendencia se está fortaleciendo).
4. Si no existe ninguna posición abierta, envíe una orden de compra de mercado utilizando el volumen de la estrategia.

### Entrada corta
1. Espere una vela terminada y una lectura finalizada de ADX.
2. Confirme que DI- esté por encima de DI+.
3. Requerir que la línea principal ADX sea mayor que su valor anterior.
4. Si es plano, envíe una orden de venta de mercado con el volumen de la estrategia.

### Salir de la lógica
- **Cerrar en Largo**: Cuando DI- cruza por encima de DI+ (el impulso de la tendencia se vuelve bajista).
- **Cerrar en corto**: Cuando DI+ cruza por encima de DI- (el impulso de la tendencia se vuelve alcista).
- La verificación de pendiente ADX no es necesaria para las salidas, lo que refleja la EA original que cerró posiciones inmediatamente después de un cruce DI.

## Gestión de Puestos
- La estrategia siempre es plana, larga o corta; nunca ocupa posiciones simultáneas en ambas direcciones.
- Las órdenes de mercado se dimensionan utilizando la propiedad incorporada `Strategy.Volume` (predeterminada 1). Ajuste esta propiedad al configurar la instancia de estrategia para que coincida con el tamaño de su instrumento.
- No existen órdenes automáticas de stop-loss o take-profit. El riesgo debe controlarse externamente o modificando la estrategia.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `AdxPeriod` | `int` | 25 | Longitud retrospectiva para cálculos ADX, DI+ y DI-. |
| `CandleType` | `DataType` | plazo de 15 minutos | Suscripción de vela utilizada para impulsar los cálculos de indicadores. |

## Diferencias con la versión original MQL
- Gestión del dinero: los EA lotes originales redimensionados según el saldo de la cuenta; la estrategia StockSharp utiliza `Strategy.Volume` y deja la gestión del capital al entorno de alojamiento.
- Seguimiento de pedidos: en lugar de iterar a través de MetaTrader grupos de pedidos, StockSharp se basa en el valor integrado `Position`.
- Manejo de datos: la estrategia ignora las velas inacabadas y solo opera con datos finalizados.
- Los enlaces de registro y visualización están disponibles a través de los ayudantes `CreateChartArea`, `DrawCandles` y `DrawIndicator` para facilitar la depuración.

## Pautas de uso
1. Adjunte la estrategia a un instrumento con suficiente movimiento de tendencia (por ejemplo, índices o divisas principales).
2. Establezca el tipo de vela deseado y la longitud ADX mediante parámetros antes de comenzar la estrategia.
3. Opcionalmente, habilite la gestión de riesgos a nivel de cartera (stop-outs, límites de reducción) a través de la aplicación de alojamiento.
4. Supervise los cruces de DI y la pendiente ADX en el visualizador de gráficos para verificar el comportamiento.

## Ampliando la estrategia
- Agregue filtros de volatilidad (ATR, desviación estándar) para evitar condiciones de baja volatilidad.
- Introduzca la automatización de stop-loss/take-profit llamando a `StartProtection` o una lógica de orden personalizada en `ProcessCandle`.
- Combínelo con filtros de períodos de tiempo más altos suscribiéndose a flujos de velas adicionales.

Esta documentación tiene como objetivo proporcionar una vista completa de la ADX Estrategia de tendencia simple para que pueda implementarla y ampliarla de forma segura dentro del marco StockSharp.
