# Diagrama de cruce de medias con sincronización
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama sincroniza una vela horaria con los valores completos de una media exponencial rápida y otra lenta antes de evaluar su cruce. Abre una unidad en la primera señal y usa dos veces el volumen base en cada señal opuesta, por lo que invierte la posición sin aumentar su tamaño.

![schema](schema.svg)

## Resumen de la estrategia

- Una sola serie de velas horarias finalizadas alimenta EMA(20) y EMA(50), manteniendo la misma base de precios para ambas medias.
- El bloque Sync forma un grupo horario con la vela, la EMA rápida y la EMA lenta; no se decide un cruce con un grupo incompleto.
- Crossing emite `true` cuando la EMA rápida supera a la lenta y `false` cuando cae por debajo; un bloque NOT convierte este último valor en el disparo corto.
- Las comprobaciones del signo de la posición eligen una apertura con volumen base desde plano o una inversión con dos veces el volumen base desde el lado opuesto.
- El gráfico recibe la vela sincronizada, ambos valores EMA sincronizados y todas las ejecuciones de la estrategia.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando EMA(20) sincronizada cruza por encima de EMA(50), se compra un volumen base desde plano o dos volúmenes base desde corto, dejando una posición larga de un volumen base.
- **Entrada en corto**: Cuando EMA(20) sincronizada cruza por debajo de EMA(50), se vende un volumen base desde plano o dos volúmenes base desde largo, dejando una posición corta de un volumen base.
- **Salida**: No hay stop, objetivo ni salida temporal independiente. El siguiente cruce opuesto lanza una inversión a mercado cuyo volumen cierra la unidad actual y abre una unidad en la nueva dirección.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA Length | 20 | Longitud de la media móvil exponencial rápida. |
| Slow EMA Length | 50 | Longitud de la media móvil exponencial lenta. |
| Candles | 01:00:00 | Marco temporal de las velas finalizadas utilizadas por ambas medias. |
| Sync interval | 01:00:00 | Rango temporal con el que Sync agrupa la vela y los dos valores de indicadores. |
| Base volume | 1 | Tamaño abierto desde plano; una inversión usa automáticamente el doble de este valor. |

## Detalles del diagrama

- La salida de velas actualiza primero la instantánea de posición y las constantes de cero y volumen, y después alimenta EMA(20) y EMA(50); su conexión final entra en el tercer puerto de Sync y completa el grupo tras ambos cálculos.
- Sync Input 1 recibe EMA(20), Input 2 recibe EMA(50) e Input 3 recibe la vela. Las tres salidas emparejadas están conectadas y el bloque limpia cada grupo completo.
- Sync Output 1 y Output 2 alimentan Crossing Input Up e Input Down. Output 3 atraviesa un convertidor ClosePrice y también suministra la serie de velas al gráfico.
- Cuatro rutas lógicas distinguen los estados plano, largo y corto para las dos direcciones de cruce. Una señal del mismo lado no puede aumentar una posición existente.
- Cuatro bloques Modify position envían órdenes a mercado: dos aperturas usan el volumen base y dos inversiones usan la fórmula `2 × volumen base`.
- Como cada posición se abre con el volumen base, el importe de inversión equivale a `abs(position) + volumen base` y conserva la magnitud de la exposición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
