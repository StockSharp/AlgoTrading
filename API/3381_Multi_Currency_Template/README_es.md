# Estrategia de plantilla multidivisa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de plantilla multimoneda** es una conversión del asesor experto MetaTrader 4 *Plantilla multimoneda v4*. Reproduce la lógica de entrada cruzada EMA original junto con un promedio estilo martingala, niveles de protección basados ​​en pips y gestión de seguimiento utilizando la API de alto nivel de StockSharp. El período de tiempo predeterminado son velas de cinco minutos, pero se puede cambiar mediante un parámetro.

## Lógica comercial
- Se calculan dos promedios móviles exponenciales (EMA 20 y EMA 50) en cada vela terminada del período de tiempo seleccionado.
- Aparece una señal larga cuando el EMA rápido (20) cierra por encima del EMA lento (50). Aparece una señal corta cuando el EMA rápido cierra por debajo del EMA lento.
- El parámetro `Order Method` decide si la estrategia actúa sobre ambas señales o restringe el comercio a operaciones solo largas o cortas.
- Sólo se mantiene una posición neta por dirección. Cuando llega una nueva señal, la estrategia cierra cualquier posición opuesta antes de abrir el lado solicitado.

## Gestión de Puestos
- **Stop Loss / Take Profit**: las distancias se ingresan en MetaTrader pips. Se convierten a unidades de precio utilizando el paso del precio del valor, reproduciendo el manejo original de los símbolos Forex de 4 y 5 dígitos.
- **Trailing Stop**: se activa una vez que el precio se mueve a favor de la posición en `Trailing Stop (pts)` y se ajusta después de cada mejora adicional de `Trailing Step (pts)`.
- **Martingale Promedio**: cuando está habilitado, se envían órdenes de mercado adicionales cada `Step (pts)` contra la posición actual. Cada nuevo volumen de pedido se escala en `Lot Multiplier` y el proceso se repite hasta que se cierra la posición.
- **Take Profit promedio**: cuando se abren dos o más órdenes promedio, el objetivo de toma de ganancias puede usar opcionalmente el precio de la posición ponderada más `Average TP Offset (pts)` para emular el comportamiento de MetaTrader "promedio de TP".

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| Método de pedido | Dirección comercial (Compra y venta, Solo compra, Solo venta). | Comprar y vender |
| Volumen (lotes) | Tamaño base de la orden del mercado. | 0,01 |
| Stop Loss (pips) | Distancia de parada de protección en MetaTrader pips. | 50 |
| Tomar ganancias (pips) | Distancia objetivo de ganancias en MetaTrader pips. | 100 |
| Trailing Stop (pts) | Umbral de activación del trailing stop en MetaTrader puntos. | 15 |
| Paso final (pts) | Se necesita una mejora mínima antes de mover el trailing stop. | 5 |
| Habilitar Martingale | Permite promediar hacia abajo/arriba con un volumen creciente. | cierto |
| Multiplicador de lote | Multiplicador de volumen aplicado a cada nuevo pedido promedio. | 1.2 |
| Paso (ptos) | MetaTrader distancia de puntos antes de realizar el siguiente pedido promedio. | 150 |
| Toma de ganancias promedio | Cambie entre toma de ganancias fija o promedio cuando existan múltiples órdenes. | cierto |
| Compensación promedio de TP (pts) | MetaTrader compensación de puntos aplicada a la toma de ganancias promedio. | 20 |
| Tipo de vela | Tipo de vela (marco de tiempo) utilizado para los cálculos del indicador. | velas de 5 minutos |

## Diferencias frente al Expert Advisor original
- StockSharp ejecuta posiciones netas en lugar de gestionar tickets MetaTrader individuales. El módulo martingala aumenta el tamaño de la posición neta en lugar de adjuntar objetivos separados específicos del ticket.
- El comercio con múltiples símbolos debe lograrse lanzando varias instancias de estrategia, una por valor. El asesor experto original admitía una lista multidivisa integrada dentro de una instancia EA.
- Los controles de administración de dinero (`CheckMoneyForTrade`, `CheckVolumeValue`) y las restricciones específicas del corredor se reemplazan por la validación de órdenes StockSharp.

## Notas de uso
1. Asegúrese de que los metadatos de seguridad (escalón de precio y decimales) coincidan con el instrumento para que la conversión de pips siga siendo precisa.
2. La lógica de trailing stop y martingala actúa sobre los precios de cierre de las velas de forma predeterminada. Para un comportamiento más reactivo, conecte fuentes de datos adicionales (cotizaciones u operaciones) y llame a los ayudantes de administración desde allí.
3. Debido a que se utilizan órdenes de mercado, el control de deslizamiento se delega al corredor o simulador conectado.
