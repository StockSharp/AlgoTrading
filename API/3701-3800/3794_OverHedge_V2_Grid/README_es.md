# Estrategia de red OverHedge V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

OverHedge V2 es un sistema de cuadrícula cubierto que alterna posiciones largas y cortas al tiempo que aumenta el tamaño de la operación después de cada ejecución. La estrategia analiza la relación entre una media móvil exponencial rápida y lenta (EMA) para decidir la dirección dominante para el próximo ciclo. Una vez que comienza un ciclo, el algoritmo coloca órdenes de mercado cada vez que el precio alcanza niveles de túnel predefinidos alrededor de la cotización inicial. La cuadrícula se expande simétricamente de modo que cada nuevo tramo compense la pérdida flotante del anterior. El ciclo finaliza cuando el beneficio abierto agregado excede un objetivo configurable o cuando el operador solicita manualmente un cierre.

La implementación mantiene recuentos separados para exposición larga y corta y utiliza precios de Nivel 1 en vivo para activar nuevas coberturas. El volumen comercial crece geométricamente según el multiplicador elegido, que reproduce el riesgo estilo martingala del asesor experto original MetaTrader. Debido a que las órdenes se ejecutan en el mercado, el sistema se adapta automáticamente a las condiciones de liquidez manteniendo el espaciado de la cuadrícula expresado en puntos.

## como funciona

1. **Filtro de dirección**: la estrategia calcula dos EMA en velas completadas. Cuando el EMA rápido está por encima del EMA lento, el siguiente ciclo comienza con un sesgo largo; de lo contrario, se parte de un sesgo corto.
2. **Inicialización del ciclo**: al comienzo de un ciclo, el algoritmo registra el precio de oferta actual y deriva dos límites de túnel separados por el ancho configurado y el diferencial en vivo. El primer orden sigue el sesgo EMA y el tramo opuesto se coloca a la distancia del túnel.
3. **Expansión de la red**: si el precio continúa con respecto a la última entrada, se activan alternativamente órdenes de mercado adicionales (compra, venta, compra,…). Cada nuevo tramo multiplica el volumen anterior por el multiplicador de cobertura, lo que permite que la posición general se recupere más rápido en caso de reversión.
4. **Recolección de ganancias**: el ciclo monitorea constantemente las ganancias no realizadas utilizando los mejores precios de oferta y demanda. Cuando se alcanza el valor objetivo, o si el operador activa la bandera de apagado, todos los tramos abiertos se liquidan y el ciclo se reinicia.
5. **Seguimiento de exposición**: la estrategia mantiene el precio y el volumen promedio de las coberturas largas y cortas para calcular las ganancias abiertas con precisión y evitar enviar órdenes duplicadas mientras las existentes aún están pendientes.

## Parámetros predeterminados

- `Base Volume` = 0,1 lotes: tamaño de la operación inicial para el primer tramo de la cuadrícula.
- `Hedge Multiplier` = 2,0: multiplicador de volumen aplicado a cada tramo posterior.
- `Tunnel Width (points)` = 20: distancia adicional entre órdenes alternas más allá del diferencial actual.
- `Profit Target` = 100: beneficio no realizado en la moneda de la cuenta que cierra toda la cuadrícula.
- `Short EMA` = 8 – Período del EMA rápida utilizado para la detección de dirección.
- `Long EMA` = 21: período del EMA lento utilizado para la detección de dirección.
- `Candle Type` = 1 minuto: período de tiempo que alimenta los filtros EMA.
- `Shutdown Grid` = false: cuando es verdadero, la estrategia sale inmediatamente de todos los tramos y deja de operar.

## Notas

- La cuadrícula funciona con cualquier instrumento que proporcione cotizaciones de Nivel 1 (mejor oferta/demanda). Los diferenciales más amplios aumentan el tamaño del túnel automáticamente.
- El volumen comercial se normaliza mediante el paso de volumen de seguridad para evitar pedidos rechazados.
- Debido a que el sistema utiliza un esquema de dimensionamiento martingala, es posible que se produzcan grandes caídas si las tendencias de precios persisten sin alcanzar el objetivo de ganancias.
- Para reanudar las operaciones después de un cierre, vuelva a cambiar el parámetro a `false` o reinicie la estrategia.
