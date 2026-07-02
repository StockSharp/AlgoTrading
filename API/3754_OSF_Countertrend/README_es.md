# Estrategia de contratendencia OSF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce la contratendencia del experto en Open Source Forex "Sobrecompra/Sobreventa".
Se aproxima al oscilador original promediando varias lecturas RSI e interpreta
la distancia desde el nivel de equilibrio (50) como señal tanto de dirección como de tamaño de posición.
Las operaciones se ejecutan sobre velas terminadas y se cierran mediante una toma de ganancias fija medida en
puntos del instrumento.

## Reglas de trading

- **Datos**: Velas terminadas del `CandleType` configurado.
- **Indicador**: RSI con período definido por `RsiPeriod`. El experto original MQL promedió cinco
valores RSI idénticos, por lo que un solo RSI es suficiente aquí.
- **Lógica de señal**:
  - Cuando RSI > 50, el mercado se considera sobrecomprado y se abre una posición corta.
  - Cuando RSI < 50, el mercado se considera sobrevendido y se abre una posición larga.
  - La distancia absoluta |RSI − 50| determina el volumen negociado a través de `VolumePerPoint`.
- **Enfriamiento**: Después de cada operación, la estrategia espera `CooldownBars` velas terminadas antes
evaluando una nueva entrada. Esto imita el comportamiento de suavizado de barras del código fuente.
- **Salidas**: Cada entrada coloca una toma de ganancias manual a `TakeProfitPoints` * `PriceStep` de distancia de
el precio de llenado. No se utiliza ningún stop-loss, exactamente como en el experto original.
- **Reversiones**: Al abrir una operación en la dirección opuesta se cierra primero cualquier posición existente.
ajustando el volumen de la orden de mercado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `RsiPeriod` | RSI longitud utilizada para aproximar el oscilador OSF (predeterminado 14). |
| `VolumePerPoint` | Volumen negociado por cada RSI punto alejado del nivel 50 (predeterminado 0,01). |
| `TakeProfitPoints` | Distancia al objetivo de toma de ganancias expresada en puntos del instrumento (por defecto 150). |
| `CooldownBars` | Número de velas terminadas que se omitirán después de cada operación (5 por defecto). |
| `CandleType` | Tipo de vela para cálculos de indicadores (período de tiempo predeterminado de 1 minuto). |

## Notas

- La estrategia asume que `PriceStep` está definido para el instrumento seleccionado; de lo contrario una unidad
El paso de 1 se utiliza para calcular el nivel de obtención de beneficios.
- Debido a que el experto original no tenía un límite de pérdidas protector, se debe agregar la gestión de riesgos.
manualmente al implementar la estrategia en vivo.
