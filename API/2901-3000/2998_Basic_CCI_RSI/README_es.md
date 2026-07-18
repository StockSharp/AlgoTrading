# Estrategia Básica CCI RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Básica CCI RSI reproduce el asesor experto original de MetaTrader que espera a que tanto el Commodity Channel Index (CCI) como el Relative Strength Index (RSI) confirmen el impulso durante dos velas cerradas consecutivas antes de entrar en una operación. La versión de StockSharp mantiene las reglas de gestión de dinero basadas en pips, las convierte en pasos de precio automáticamente y añade el mismo comportamiento de trailing stop que fue implementado con modificaciones de posición en MQL5.

## Cómo opera la estrategia

1. Al cierre de cada vela (por hora por defecto) la estrategia recibe valores frescos de CCI y RSI.
2. Las entradas largas requieren que **ambos** indicadores permanezcan por encima de sus umbrales superiores respectivos durante la vela actual y la anterior cerrada. Las entradas cortas requieren que ambos permanezcan por debajo de sus umbrales inferiores durante las últimas dos velas.
3. Cuando ocurre una señal, la estrategia abre una posición con el volumen configurado (cerrando cualquier exposición opuesta) e inmediatamente calcula precios fijos de stop-loss y take-profit usando las distancias en pips del script original.
4. Mientras la posición está abierta, la estrategia verifica constantemente si el rango de la vela tocó los niveles de stop o take y sale a mercado si alguno es alcanzado.
5. Un trailing stop replica la implementación de MetaTrader: una vez que el beneficio supera `TrailingStopPips + TrailingStepPips`, el stop protector se mueve a `TrailingStopPips` detrás del cierre actual (para largos) o por encima de él (para cortos). Los ajustes posteriores requieren un `TrailingStepPips` adicional de beneficio antes de ajustarse nuevamente.

Este flujo mantiene la lógica cerca del experto MQL5 fuente mientras usa suscripciones de velas de alto nivel de StockSharp e indicadores.

## Gestión de riesgos

- **Stop-loss**: distancia fija en pips convertida al paso de precio del instrumento. Deshabilitado cuando se establece en cero.
- **Take-profit**: distancia fija en pips convertida al paso de precio del instrumento. Deshabilitado cuando es cero.
- **Trailing stop**: distancia en pips opcional con un buffer de paso que imita la función `Trailing()` del asesor experto. Deshabilitado cuando `TrailingStopPips` es cero.
- **Dimensionamiento de posición**: controlado a través de la propiedad `Volume` de la estrategia; el lote predeterminado es un contrato.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `StopLossPips` | Distancia en pips entre el precio de entrada y la orden de stop-loss. |
| `TakeProfitPips` | Distancia en pips entre el precio de entrada y el objetivo de take-profit. |
| `TrailingStopPips` | Beneficio (en pips) requerido para comenzar a trailear el stop. |
| `TrailingStepPips` | Beneficio adicional (en pips) requerido antes de cada nuevo ajuste de trailing. |
| `CciPeriod` | Período de promediado para el indicador CCI. |
| `RsiPeriod` | Período de promediado para el indicador RSI. |
| `RsiLevelUp` | Nivel de sobrecompra que debe superarse para validar operaciones largas. |
| `RsiLevelDown` | Nivel de sobreventa que debe romperse para validar operaciones cortas. |
| `CciLevelUp` | Umbral superior de CCI que confirma impulso alcista. |
| `CciLevelDown` | Umbral inferior de CCI que confirma impulso bajista. |
| `CandleType` | Marco temporal utilizado para la agregación de velas y cálculos de indicadores. |

## Valores predeterminados

- `StopLossPips` = 125
- `TakeProfitPips` = 60
- `TrailingStopPips` = 5
- `TrailingStepPips` = 5
- `CciPeriod` = 12
- `RsiPeriod` = 15
- `RsiLevelUp` = 75
- `RsiLevelDown` = 30
- `CciLevelUp` = 80
- `CciLevelDown` = -95
- `CandleType` = velas de 1 hora

## Notas adicionales

- Las distancias en pips se escalan automáticamente: si el instrumento usa 3 o 5 decimales, la estrategia multiplica el paso de precio por diez, coincidiendo con la lógica de "punto ajustado" de MetaTrader.
- Las entradas se evalúan solo en velas cerradas para evitar el repintado y reflejar la condición original de "nueva barra" en el asesor experto.
- Las salidas siempre usan órdenes de mercado, proporcionando un comportamiento determinista dentro del entorno de backtesting de StockSharp.

## Etiquetas de clasificación

- Categoría: Confirmación de osciladores
- Dirección: Bidireccional
- Indicadores: CCI, RSI
- Stops: Fijos y trailing (basados en pips)
- Complejidad: Básico
- Marco temporal: Intradía a swing (predeterminado 1 hora)
- Estacionalidad: No
- Redes neuronales: No
- Divergencia: No
- Nivel de riesgo: Moderado
