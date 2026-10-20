# Ichimoku con exponente de Hurst
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de tendencia que combina cruces Tenkan/Kijun, la nube Ichimoku y un filtro Hurst sobre rendimientos logarítmicos. Las señales usan velas cerradas.

## Cálculo

Tenkan y Kijun son `(máximo High + mínimo Low) / 2` de sus ventanas respectivas. Senkou A sin desplazar es su promedio; Senkou B es el punto medio de `SenkouSpanBPeriod`. Se usan las líneas causales desplazadas de StockSharp Ichimoku, con desplazamiento `KijunPeriod` y su búfer de inicialización; no se leen velas futuras.

Los últimos `HurstPeriod` cierres producen `n = HurstPeriod - 1` rendimientos `r[i] = ln(C[i] / C[i-1])`. Con media `mu`, `X[k] = sum(r[i] - mu)`, `R = max(X) - min(X)` y desviación estándar poblacional `S`, se calcula `H = ln(R / S) / ln(n)` mediante StockSharp HurstExponent sobre esos rendimientos. La historia empieza en la primera vela cerrada. Se requieren al menos once cierres, una ventana completa y varianza positiva. Un cierre no positivo reinicia la ventana; un valor Hurst no disponible impide entrar.

## Operaciones

- **Largo:** sin posición, cierre estrictamente por encima de ambas líneas, Tenkan cruza Kijun de `<=` a `>`, `H > HurstThreshold`, ATR formado y positivo, espera terminada.
- **Corto:** sin posición, cierre estrictamente por debajo de ambas líneas y cruce inverso; se aplican los mismos requisitos Hurst, ATR y espera.
- **Salida por señal:** cerrar todo el largo al cerrar bajo ambas líneas o cruzar Tenkan a la baja; cerrar todo el corto con las condiciones inversas. Las salidas siguen activas durante la espera y no requieren superar el filtro Hurst de entrada.
- **Órdenes:** entrada de mercado por `Volume` y salida por la posición restante real. Una orden sin terminar bloquea otras órdenes por señal. La salida cierra antes de una entrada posterior.
- **Espera:** cada orden por señal reinicia `SignalCooldownBars`; otra entrada es posible tras ese número de velas cerradas.

## Protección

El ATR de Wilder usa `AtrPeriod`. Cada entrada fija la distancia `StopLossAtr * ATR` de su vela de señal respecto al precio real de ejecución. La protección local procesa Level1 y puede cerrar mediante orden de mercado entre velas. Los cambios posteriores del ATR no alteran la distancia. `StopLossAtr = 0` desactiva la protección para la ejecución. No hay take-profit ni stop móvil.

## Valores predeterminados

| Parámetro | Valor |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 100 cierres |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 minutos |

Las pruebas comparan cada orden por señal y sus ejecuciones con nubes y R/S de rendimientos calculados de forma independiente en el archivo BTC/TON incluido. También verifican calentamiento Hurst, distancias ATR fijas y ejecuciones protectoras reales dentro de la vela.

La comparación protectora usa períodos Tenkan/Kijun/Senkou B 15/40/70 y HurstThreshold 0.45 para activar realmente la distancia predeterminada de 2 × ATR en el archivo. Las señales predeterminadas 9/26/52 se prueban aparte. Las pruebas protectoras entran con volumen 3.
