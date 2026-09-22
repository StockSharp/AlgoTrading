# Diagrama de escalera porcentual de rejilla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama construye una rejilla porcentual simétrica alrededor del primer cierre terminado de cinco minutos. Coloca tres compras limitadas bajo el ancla y tres ventas sobre ella; tras la primera ejecución cancela el resto y entrega la posición a la protección porcentual.

![schema](schema.svg)

## Resumen de la estrategia

- El cierre de la primera vela terminada de cinco minutos se fija como ancla; no se usa Level1 ni el punto medio bid/ask.
- Un espaciado de 1,5% crea hasta tres niveles compradores debajo y tres vendedores encima del ancla.
- Grid Levels per Side habilita los peldaños del uno al tres y los interruptores long y short controlan cada lado por separado.
- La primera entrada ejecutada cancela todas las órdenes restantes e inicia protección con beneficio de 2% y pérdida de 3%.
- Una salida protectora cancela residuos, fija el último cierre como nueva ancla y registra otra escalera.

## Reglas de entrada y salida

- **Entrada en largo**: Con long habilitado se colocan compras de una unidad en anchor × (1 − spacing × peldaño), con uno a tres niveles inferiores activos.
- **Entrada en corto**: Con short habilitado se colocan ventas de una unidad en anchor × (1 + spacing × peldaño), con uno a tres niveles superiores activos.
- **Salida**: La primera orden ejecutada inicia el único ciclo de posición. La protección cierra en +2% o −3% y vuelve a anclar seis órdenes en el último cierre terminado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Grid Spacing, % | 1.5 | Distancia porcentual entre peldaños vecinos; 1,5 significa 1,5%. |
| Grid Levels per Side | 3 | Peldaños habilitados por lado, de uno al máximo visual de tres. |
| Enable Long | true | Habilita compras limitadas debajo del ancla. |
| Enable Short | true | Habilita ventas limitadas encima del ancla. |
| Take Profit, % | 2 | Distancia de beneficio desde la entrada usada por la protección. |
| Stop Loss, % | 3 | Distancia de pérdida desde la entrada usada por la protección. |

## Detalles del diagrama

- La estrategia C# conserva niveles virtuales y envía órdenes de mercado cuando el cierre los alcanza. El diagrama los materializa como límites pendientes para mostrar los cubos de órdenes y su cancelación.
- El código fuente puede activar varios niveles. Aquí se simplifica deliberadamente a un ciclo: una ejecución cancela los demás peldaños, negocia volumen fijo uno y espera la protección antes de reconstruir.
- Grid Levels per Side admite de uno a tres. El máximo visual es tres, por lo que valores superiores no crean bloques adicionales.
- El reanclaje cancela y luego registra órdenes nuevas, sin reemplazo; el ajuste al paso de precio está desactivado para instrumentos de replay sin paso declarado.
- El cierre exacto de la vela sirve de ancla inicial y posterior, igual que el precio de reinicio del código, sin depender de Level1.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
