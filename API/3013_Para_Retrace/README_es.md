# Estrategia de Retroceso Para
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia de Retroceso Para** es una conversión a C# del asesor experto original de MetaTrader 4 `Para_Retrace.mq4`. Reproduce la idea de usar el indicador Parabolic SAR como ancla dinámica y esperar retrocesos del precio hacia ese nivel antes de entrar al mercado. La conversión aprovecha la API de alto nivel de StockSharp para gestionar suscripciones de datos de mercado, actualizaciones de indicadores y ejecución de órdenes.

## Lógica de Trading
1. Calcular el valor del Parabolic SAR en cada vela terminada usando el paso de aceleración y la aceleración máxima configurados.
2. Determinar la tendencia predominante comprobando si toda la vela está por debajo o por encima del valor SAR:
   - **Contexto bajista:** si tanto el máximo como el mínimo de la vela están por debajo del valor SAR.
   - **Contexto alcista:** de lo contrario (el precio está tocando o por encima del nivel SAR).
3. Derivar un precio desencadenante desplazando el valor SAR un número de pips definido por el usuario:
   - En un contexto bajista la estrategia resta el desplazamiento, esperando un retroceso hacia arriba.
   - En un contexto alcista la estrategia suma el desplazamiento, esperando un retroceso hacia abajo.
4. Una vez que el precio toca el desencadenante (el máximo cruza por encima para cortos, el mínimo cruza por debajo para largos), la estrategia abre una orden de mercado en la dirección de la tendencia.
5. Las órdenes de stop-loss y take-profit de protección se adjuntan automáticamente usando la facilidad `StartProtection` de StockSharp, coincidiendo con las distancias del script original.

A diferencia del asesor experto original, la versión de StockSharp continúa operando después de que se abre una posición; no es necesario restablecer manualmente el valor de desplazamiento. Todas las acciones se realizan únicamente en velas completadas para evitar problemas de repintado intrabarra.

## Indicadores
- **Parabolic SAR** – impulsa tanto la detección de tendencia como los niveles de entrada.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `SarStep` | Factor de aceleración inicial para el Parabolic SAR. | `0.01` |
| `SarMax` | Factor de aceleración máximo para el Parabolic SAR. | `0.2` |
| `RetraceOffsetPips` | Distancia (en pips) entre el valor SAR y el desencadenante de entrada. | `30` |
| `StopLossPips` | Distancia de stop-loss en pips (convertida a precio absoluto). Establecer en `0` para deshabilitar. | `30` |
| `TakeProfitPips` | Distancia de take-profit en pips (convertida a precio absoluto). Establecer en `0` para deshabilitar. | `30` |
| `CandleType` | Marco temporal usado para velas y cálculos de indicadores. | `5 Minute` |

> **Nota:** La estrategia estima el tamaño del pip a partir de los metadatos de seguridad. Si el instrumento usa cinco decimales (típico para Forex), un pip equivale a diez pasos mínimos de precio.

## Gestión de Órdenes
- Las órdenes se colocan a mercado una vez que se satisface la condición de retroceso.
- El tamaño de operación predeterminado es un lote (`Volume = 1`), pero puede ajustarse mediante la propiedad base `Strategy.Volume` antes de iniciar la estrategia.
- `StartProtection` gestiona automáticamente las colocaciones de stop-loss y take-profit usando desplazamientos de precio absolutos derivados de los ajustes de pips.

## Consejos de Uso
- Ajuste el desplazamiento de pips, el stop y el objetivo para que coincidan con la volatilidad del instrumento que se está operando.
- Considere combinar la estrategia con filtros adicionales (hora del día, volatilidad, etc.) cuando se integre en un marco de trading más amplio.
- Siempre realice backtesting antes de desplegar en vivo, ya que la rentabilidad depende fuertemente de las condiciones del mercado y la ejecución del bróker.

## Diferencias vs. Script Original
- Trading continuo sin variables globales manuales.
- Usa velas completadas en lugar de verificaciones tick a tick, lo que proporciona comportamiento determinista para backtests.
- Gestión de riesgo integrada a través del módulo de órdenes de protección de StockSharp.

## Descargo de Responsabilidad
Esta estrategia se proporciona con fines educativos. Pruebe exhaustivamente en datos históricos y de demostración antes de comprometer capital real.
