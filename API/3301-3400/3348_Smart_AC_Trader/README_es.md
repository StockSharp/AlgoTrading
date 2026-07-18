# Estrategia de comerciante de aire acondicionado inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Smart AC Trader adapta la idea original de MetaTrader "Smart AC Trader" al alto nivel de StockSharp API. El experto MQL evaluó la fortaleza relativa de las monedas dentro de un par y reaccionó cuando la moneda base superó a la moneda cotizada. En StockSharp nos centramos en el mismo comportamiento impulsado por el impulso, pero operamos con un único instrumento al que está vinculada la estrategia. La fuerza se aproxima mediante una combinación de promedios móviles exponenciales (EMA) y el indicador de tasa de cambio (ROC):

- Un EMA rápido mide la dirección de la tendencia a corto plazo.
- Un EMA lento representa la tendencia principal.
- ROC confirma que el impulso del precio se alinea con la tendencia antes de que se permitan las entradas.

Una vez que se abre una posición, la estrategia gestiona activamente la operación utilizando reglas de stop-loss, take-profit, trailing stop y punto de equilibrio que reflejan la amplia configuración de gestión del dinero del experto original.

## Lógica de trading
1. Suscríbase al tipo de vela configurado (período de tiempo) y calcule el EMA rápido, el {PH001}} lento y el ROC en el cierre de la vela.
2. Ingrese una posición larga cuando el EMA rápido esté por encima del EMA lento y ROC sea mayor o igual que el umbral de impulso de compra. La exposición corta existente se cierra antes de que se abra la nueva exposición larga.
3. Ingrese una posición corta cuando el EMA rápido esté por debajo del EMA lento y ROC sea menor o igual al umbral de impulso de venta negativo. La exposición larga existente se cierra antes de que se abra la nueva posición corta.
4. Administre una posición abierta en cada vela terminada:
   - Cierre la operación a las distancias configuradas de obtención de beneficios o límite de pérdidas (expresadas en incrementos de precio).
   - Opcionalmente, arme una salida de equilibrio una vez que el precio se mueva a favor de la operación en la distancia de activación y liquide si el precio regresa a la compensación preservada.
   - Opcionalmente, siga la parada por la distancia configurada desde el máximo más alto (largo) o el mínimo más bajo (corto) observado después de la entrada.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| **EMA rápida** | Longitud del filtro de tendencia EMA rápida. |
| **EMA lenta** | Longitud del filtro de tendencia EMA lenta. |
| **ROC Período** | Ventana retrospectiva para el filtro de impulso de tasa de cambio. |
| **Comprar impulso** | Se requiere un ROC positivo mínimo para abrir operaciones largas. |
| **Impulso de venta** | Mínimo negativo absoluto ROC requerido para abrir operaciones cortas. |
| **Detener pérdidas** | Distancia de stop-loss expresada en pasos de precio. |
| **Obtener ganancias** | Distancia de obtención de beneficios expresada en incrementos de precio. |
| **Usar seguimiento** | Permite la gestión de trailing stop. |
| **Arrastrándose** | Distancia del trailing stop en pasos de precio. |
| **Usar punto de equilibrio** | Habilita la lógica de protección del punto de equilibrio. |
| **Gatillo de equilibrio** | Se requieren ganancias en los escalones de precios para armar la lógica del equilibrio. |
| **Compensación de equilibrio** | Distancia en los pasos de precios que se mantiene después de alcanzar el punto de equilibrio. |
| **Tipo de vela** | Tipo de vela utilizada para alimentar los indicadores. |

## Notas
- La estrategia utiliza `Strategy.StartProtection()` una vez al inicio para garantizar que el sistema de protección de posición incorporado esté activo según lo recomendado por las pautas del proyecto.
- El tamaño de la posición depende de la propiedad base `Strategy.Volume`. Las órdenes de reversión incluyen automáticamente la exposición actual de modo que una señal opuesta cierra la posición existente y establece una nueva.
- Todos los parámetros de riesgo se expresan en pasos de precio porque el asesor experto original utilizó distancias basadas en pips. Asegúrese de que el instrumento tenga un `PriceStep` válido configurado.
