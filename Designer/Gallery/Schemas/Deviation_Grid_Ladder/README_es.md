# Diagrama de escalera de desviación EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama une la señal de reversión EMA(30)/StandardDeviation(14) del C# con la idea de rejilla del README. Una desviación 1,5σ arma un límite cercano negociable y otro pendiente a 2,5σ; la salida vuelve al límite fuente EMA ± 0,5σ.

![schema](schema.svg)

## Resumen de la estrategia

- Velas replay terminadas de cinco minutos actualizan EMA, desviación y un cierre sincronizado tras ambos indicadores.
- Cruzar bajo EMA − 1,5σ registra compras en −1,5σ y −2,5σ con Position <= 0; el lado superior es simétrico con Position >= 0.
- El peldaño cercano suele ejecutarse de inmediato y el lejano espera un movimiento mayor.
- El largo cierra con Close > EMA + 0,5σ y el corto con Close < EMA − 0,5σ, como el código.
- Toda salida cancela ambos límites lejanos y una señal opuesta elimina el peldaño obsoleto anterior.

## Reglas de entrada y salida

- **Entrada en largo**: Al cruzar bajo EMA − 1,5σ con Position <= 0, registra compras de una unidad en EMA − 1,5σ y EMA − 2,5σ.
- **Entrada en corto**: Al cruzar sobre EMA + 1,5σ con Position >= 0, registra ventas de una unidad en EMA + 1,5σ y EMA + 2,5σ.
- **Salida**: Close > EMA + 0,5σ cierra el largo y Close < EMA − 0,5σ cierra el corto mediante ClosePosition, que calcula todo el volumen actual automáticamente.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado; cinco minutos adapta el valor C# de cuatro horas. |
| EMA Length | 30 | Periodo de EMA central y parámetro C# real. |
| Standard Deviation Length | 14 | Periodo de anchura; 14 es literal en C#. |
| Near Entry Deviation | 1.5σ | Multiplicador de entrada fuente, literal 1,5. |
| Far Grid Deviation | 2.5σ | Segundo rango pendiente añadido desde el README. |
| Mean-Reversion Exit Deviation | 0.5σ | Límite de retorno fuente, literal 0,5. |
| Volume per Rung | 1 | Volumen independiente de cada peldaño. |

## Detalles del diagrama

- Solo EmaLength y CandleType son StrategyParam en C#; periodo 14 y multiplicadores 1,5/0,5 son literales expuestos aquí para estudio.
- C# usa cuatro horas; cinco minutos es una adaptación replay para formar suficientes señales en un mes.
- El C# ejecutable tiene un único umbral 1,5σ pese al nombre Three Level Grid. El rango 2,5σ procede expresamente del README.
- La fuente revierte con dos mercados inmediatos. Aquí el límite cercano puede aplanar primero y el lejano completar después la reversión.
- Crossing crea una escalera por excursión y la cancelación evita que un rango pendiente abra luego una posición sin gestión.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
