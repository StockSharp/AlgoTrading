# Diagrama de sesgo SMMA para un solo instrumento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Pese al nombre histórico de la galería, no es una cesta. El diagrama sigue el VectorStrategy actual: un instrumento, medias suavizadas sobre velas de cuatro horas, reversión neta y salida por PnL flotante en dinero absoluto.

![schema](schema.svg)

## Resumen de la estrategia

- Velas terminadas de cuatro horas alimentan SMMA(3) y SMMA(7); su orden define el sesgo alcista o bajista.
- Un Flag de una sola vez arma N values y bloquea las primeras ocho velas terminadas.
- El sesgo alcista compra con Position <= 0 y el bajista vende con Position >= 0.
- El volumen es abs(Position) más el volumen base, combinando cierre y apertura en una reversión neta.
- P&L change cierra la exposición al alcanzar +5000 o -300000 en moneda de cuenta.

## Reglas de entrada y salida

- **Entrada en largo**: Tras el calentamiento, la SMMA rápida está sobre la lenta y Position está plana o corta. Modify position compra a mercado para cerrar el corto y dejar una unidad base larga.
- **Entrada en corto**: Tras el calentamiento, la SMMA rápida está bajo la lenta y Position está plana o larga. Modify position vende a mercado para cerrar el largo y dejar una unidad base corta.
- **Salida**: El sesgo contrario invierte directamente la posición. Además, PnL no realizado >= 5000 o <= -300000 activa ClosePosition a mercado; si el sesgo persiste, una vela posterior puede reentrar.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 04:00:00 | Intervalo terminado para ambas medias suavizadas y las decisiones de posición. |
| Fast SMMA Length | 3 | Cantidad de valores en la SmoothedMovingAverage rápida. |
| Slow SMMA Length | 7 | Cantidad de valores en la SmoothedMovingAverage lenta. |
| MA Shift Warmup | 8 | Velas iniciales terminadas bloqueadas antes de habilitar entradas. |
| Base Volume | 1 | Tamaño conservado tras entrada desde plano o reversión neta. |
| Profit Target, money | 5000 | Beneficio no realizado en moneda de cuenta que activa ClosePosition. |
| Loss Limit, money | -300000 | Límite de pérdida no realizada en moneda de cuenta; debe ser negativo. |

## Detalles del diagrama

- GetWorkingSecurities devuelve solo (Security, CandleType); por eso no hay Index, Sync, segundo instrumento ni confirmación de cesta.
- ProfitPercent 0,5 y LossPercent 30 se convierten con el saldo inicial de prueba 1.000.000 en +5000 y -300000.
- Designer expone PnL no realizado en dinero, pero no el saldo inicial, por lo que los porcentajes son importes explícitos.
- El calentamiento cuenta las primeras ocho velas terminadas como la guarda processedBars; SMMA(7) ya está formada.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
