# Estrategia de ayuda de la empresa de utilería
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Prop Firm Helper Strategy es un sistema de desglose de canales Donchian convertido del asesor experto MetaTrader "Prop Firm Helper". La estrategia envía órdenes stop por encima del rango reciente para entradas largas y por debajo del rango para entradas cortas. Automáticamente aplica las reglas de impugnación de las empresas de apoyo al detener la negociación después de alcanzar el capital objetivo o cuando se supera el límite de pérdida diaria.

## Lógica de trading
- Suscríbase a velas definidas por el parámetro `Candle Type`.
- Calcula dos Donchian canales:
  - `Entry Period`/`Entry Shift` para detectar brotes.
  - `Exit Period`/`Exit Shift` para rastrear las operaciones abiertas.
- Coloque órdenes stop de compra un tick por encima del máximo superior Donchian desplazado cuando esté plano o en corto.
- Coloque órdenes de stop de venta un tick por debajo del mínimo inferior desplazado Donchian cuando esté plano o en posición larga.
- Utilice el suavizado de rango verdadero promedio (`ATR Period`) para decidir cuándo avanzar las órdenes stop.
- Cierre posiciones largas si la vela se sitúa por debajo del mínimo final Donchian. Cierre las posiciones cortas cuando la vela cierre por encima del máximo final Donchian.

## Gestión del riesgo
- `Risk Per Trade %` calcula el volumen de la orden a partir del capital de la cartera actual, el tamaño del paso del instrumento y el precio del paso. El volumen se redondea al paso de volumen de intercambio y se limita al volumen mínimo/máximo.
- Las órdenes stop de protección siguen la posición utilizando el canal de salida Donchian más un búfer ATR para evitar una rotación excesiva de órdenes.

## Reglas de desafío de la empresa de utilería
- `Use Challenge Rules` habilita comprobaciones de desafío.
- Las operaciones se detienen una vez que se alcanza el capital `Pass Criteria`. Todas las órdenes se cancelan y la posición se cierra.
- Las reducciones diarias superiores a `Daily Loss Limit` desencadenan una liquidación completa y desactivan nuevas órdenes durante el resto de la sesión. El valor de referencia se reinicia al comienzo de cada día de negociación.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Entry Period` | Búsqueda retrospectiva del canal Donchian principal. |
| `Entry Shift` | Número de velas terminadas ignoradas cuando se utiliza el canal de ruptura. |
| `Exit Period` | Búsqueda retrospectiva del canal final de Donchian. |
| `Exit Shift` | Número de velas terminadas ignoradas para los trailingstops. |
| `Risk Per Trade %` | Porcentaje de capital de la cartera a riesgo en cada entrada. |
| `ATR Period` | Búsqueda retrospectiva del filtro ATR utilizado cuando se detiene el movimiento. |
| `Use Challenge Rules` | Permite propiciar condiciones de desafío firme. |
| `Pass Criteria` | Nivel de capital que impide seguir negociando. |
| `Daily Loss Limit` | Reducción diaria permitida antes de que se detenga la negociación. |
| `Candle Type` | Suscripción de vela utilizada para los cálculos. |

## Notas
- La estrategia requiere una conexión de cartera para calcular el tamaño de las posiciones basadas en el riesgo y las métricas de desafío.
- Las órdenes se cancelan y se vuelven a enviar en cada vela terminada para mantener los precios de activación alineados con los últimos niveles de Donchian.
- Los parámetros predeterminados reproducen el comportamiento del asesor experto MetaTrader original.
