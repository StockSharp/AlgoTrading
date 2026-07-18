# Manual EA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia EA manual** es una conversión uno a uno API de alto nivel de StockSharp del MetaTrader 4 asesor experto *Manual_EA.mq4* (carpeta `MQL/8159`). El sistema original emite órdenes discrecionales de compra o venta cada vez que el oscilador Stochastic sale de zonas extremas. El puerto StockSharp mantiene la misma configuración del oscilador 5-3-3, neta automáticamente la exposición existente antes de realizar la siguiente orden de mercado y expone las opciones comunes de administración de dinero a través de parámetros de estrategia.

## Lógica comercial

1. La estrategia se suscribe a la serie `CandleType` (predeterminado: velas de 15 minutos) e introduce los precios de cierre en un oscilador Stochastic configurado con:
   - `%K` retrospectiva = `KPeriod` (5 barras predeterminadas)
   - `%K` desaceleración = `Slowing` (3 barras predeterminadas)
   - `%D` suavizado = `DPeriod` (3 barras predeterminadas)
2. Las señales se evalúan según el valor final de la línea %D (señal) de cada vela terminada. Se comparan dos lecturas consecutivas para detectar pasos a nivel.
3. **Entrada larga**: cuando el valor %D anterior era inferior o igual a `OversoldLevel` (predeterminado 10) y el último valor supera ese umbral. La estrategia primero neutraliza cualquier exposición corta y luego compra `Volume + |short position|` por orden de mercado.
4. **Entrada corta**: cuando el valor %D anterior era superior o igual a `OverboughtLevel` (predeterminado 90) y el último valor cae por debajo de ese umbral. Cualquier posición larga existente se cierra antes de vender `Volume + |long position|` en el mercado.
5. Las órdenes de protección se manejan a través de `StartProtection`. Un `StopLoss` y/o `TakeProfit` positivo (medido en puntos de precio) activa la gestión automática de riesgos. Establecer un parámetro en `0` deshabilita la protección correspondiente.

El puerto evita deliberadamente los patrones de acceso al búfer del indicador y la lógica de vela inacabada, cumpliendo con las mejores prácticas de API de alto nivel de StockSharp.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `CandleType` | Marco de tiempo (como `DataType`) utilizado para construir velas e impulsar el oscilador. | plazo de 15 minutos |
| `KPeriod` | Longitud retrospectiva de la línea Stochastic %K. | 5 |
| `DPeriod` | Longitud de suavizado de la línea de señal Stochastic %D. | 3 |
| `Slowing` | Se aplica un suavizado adicional a %K antes de que se calcule %D. | 3 |
| `OverboughtLevel` | Límite superior que activa entradas cortas cuando %D lo cruza hacia abajo. | 90 |
| `OversoldLevel` | Límite inferior que activa entradas largas cuando %D lo cruza hacia arriba. | 10 |
| `StopLoss` | Distancia en puntos de precio para el stop-loss protector (0 = deshabilitado). | 100 |
| `TakeProfit` | Distancia en puntos de precio para el objetivo de obtención de beneficios (0 = deshabilitado). | 100 |
| `Volume` | Tamaño del pedido enviado con cada nueva señal (lotes). Las posiciones opuestas existentes se compensan primero. | 0.1 |

## Notas adicionales

- La estrategia utiliza `SubscribeCandles` junto con `BindEx` para transmitir `StochasticOscillatorValue` actualizaciones, lo que garantiza que los valores de los indicadores sean definitivos antes de que se tomen decisiones comerciales.
- La visualización del gráfico traza automáticamente la serie de velas seleccionada, el oscilador Stochastic y las operaciones propias cuando hay un área del gráfico disponible.
- Debido a que los cruces %D se evalúan en velas terminadas consecutivas, el comportamiento coincide con la implementación MQL que comparó los valores `MODE_SIGNAL` en los turnos 1 y 2.
