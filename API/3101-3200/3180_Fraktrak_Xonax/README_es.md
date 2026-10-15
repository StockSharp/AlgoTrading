# Estrategia de Fraktrak XonaX Advanced
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión en C# del asesor experto de MetaTrader 5 **Fraktrak xonax.mq5**. El robot original rastrea fractales de Williams y abre operaciones cuando el precio rompe el nivel fractal más reciente. La versión de StockSharp mantiene la misma idea aprovechando características de la API de alto nivel como suscripciones a velas, helpers integrados de gestión monetaria y protección automática de operaciones.

## Lógica de trading

1. **Detección de fractales** – el algoritmo mantiene una ventana de cinco velas. Cuando la vela del medio crea un máximo más alto (o mínimo más bajo) que sus vecinas, el precio se guarda como el último fractal superior (o inferior).
2. **Señales de ruptura** – cuando una vela completada toca o supera el nivel fractal actual, la estrategia se prepara para operar:
   - Ruptura de fractal superior → abrir posición larga (o posición corta cuando el *Modo Reversión* está habilitado).
   - Ruptura de fractal inferior → abrir posición corta (o posición larga cuando el *Modo Reversión* está habilitado).
3. **Gestión de posiciones** – la estrategia convertida reproduce el comportamiento de MetaTrader:
   - Cierre opcional de la posición opuesta antes de abrir una nueva.
   - Stop-loss y take-profit iniciales se establecen según las distancias configuradas en pips.
   - Un trailing stop de dos etapas mueve el nivel protector después de que el precio avanza por el *Paso de Trailing* especificado.
4. **Gestión monetaria** – elegir entre lote fijo o porcentaje de riesgo basado en patrimonio. Cuando el modo de riesgo está activo, el algoritmo estima el volumen usando el patrimonio del portafolio, el tamaño del paso de precio y la distancia de stop configurada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StopLossPips` | Distancia de stop-loss expresada en pips. Establecer en cero para deshabilitar el nivel de stop-loss. |
| `TakeProfitPips` | Distancia de take-profit en pips. Cero deshabilita el objetivo. |
| `TrailingStopPips` | Distancia base del trailing stop. Requiere que `TrailingStepPips` sea mayor que cero. |
| `TrailingStepPips` | Distancia adicional que el precio debe recorrer antes de que el trailing stop avance. |
| `ReverseMode` | Invertir las reglas de ruptura (vender fractales superiores, comprar fractales inferiores). |
| `CloseOpposite` | Cuando es verdadero, cualquier posición opuesta se cierra antes de abrir una nueva operación. |
| `ManagementMode` | Seleccionar entre gestión monetaria `FixedLot` o `RiskPercent`. |
| `ManagementValue` | Valor usado por el modo de gestión monetaria activo (tamaño de lote o porcentaje). |
| `CandleType` | Serie de velas usada tanto para la detección de fractales como para las decisiones de trading. |

## Notas de uso

- El tamaño del pip se deriva automáticamente del paso de precio del instrumento. Los activos con tres o cinco dígitos decimales se tratan como instrumentos de pip fraccional (0.1 pip). Ajustar los parámetros pip en consecuencia.
- La lógica del trailing stop coincide con el experto original: requiere que tanto la distancia de trailing como el paso adicional sean positivos. De lo contrario, el trailing se omite.
- La gestión monetaria en modo de riesgo asume que el costo del paso de precio está disponible. Si no lo está, la estrategia recurre a un cálculo simplificado basado en la distancia de precio bruta.
- Habilitar *Cerrar Opuesto* para emular el comportamiento del asesor experto donde una nueva ruptura cierra la operación en ejecución antes de entrar en la dirección opuesta.

## Archivos

- `CS/FraktrakXonaxAdvancedStrategy.cs` – implementación de la estrategia.
- `README.md` – documento actual.
- `README_ru.md` – descripción en ruso.
- `README_zh.md` – descripción en chino.
