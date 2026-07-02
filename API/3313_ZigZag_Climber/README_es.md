# Estrategia ZigZag Climber
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
El asesor experto ZigZag Climber generado por fxDreema contiene solo tres bloques: un filtro **No trade** seguido de acciones **Buy now** y **Sell now**. Cuando el terminal detecta que no hay posiciones abiertas, lanza de inmediato una orden de compra a mercado con niveles predefinidos de stop-loss y take-profit y, sin más comprobaciones, coloca una orden de venta a mercado simétrica. Ambas operaciones heredan los mismos parámetros de riesgo y están pensadas para coexistir como un par cubierto.

Este port C# reproduce ese comportamiento en StockSharp esperando la primera vela terminada del marco elegido y enviando después las patas de compra y venta una tras otra con distancias protectoras idénticas. No hay generación adicional de señales, trailing ni gestión de posición, exactamente como en el proyecto MQL original.

## Lógica de negociación
1. Esperar a que la primera vela del marco configurado se forme por completo.
2. Si la estrategia puede operar y aún no se han colocado órdenes, enviar una **compra** a mercado con el volumen fijo.
3. Adjuntar órdenes de stop-loss y take-profit al largo usando distancias en pips estilo MetaTrader (convertidas mediante `PriceStep` del instrumento).
4. Enviar inmediatamente una **venta** a mercado con el mismo volumen y adjuntar niveles protectores reflejados.
5. No abrir más órdenes durante el resto de la ejecución.

> **Importante:** MetaTrader 4 trabaja en modo hedging, por lo que ambos lados pueden permanecer abiertos simultáneamente. StockSharp usa el modelo de ejecución del broker; en cuentas netting, la segunda orden compensará la primera y la estrategia terminará plana. Use un conector con hedging (por ejemplo, gateway MetaTrader configurado para cuentas hedge) si desea mantener vivas ambas patas.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `Candle Type` | 1 minuto | Marco temporal que dispara la secuencia de entrada única. |
| `Trade Volume` | 0.01 | Volumen fijo aplicado a ambas órdenes de mercado. |
| `Stop-Loss (pips)` | 99.9 | Distancia del stop protector en pips MetaTrader (maneja automáticamente símbolos de 4/5 dígitos). |
| `Take-Profit (pips)` | 100 | Distancia objetivo en pips MetaTrader. |

Todas las distancias se convierten a puntos de precio mediante `PriceStep` y precisión decimal del instrumento antes de pasarse a `SetStopLoss`/`SetTakeProfit`.

## Gestión de riesgo
La estrategia se apoya en el servicio integrado `StartProtection()` y los métodos auxiliares `SetStopLoss`/`SetTakeProfit` para colocar órdenes protectoras justo después de cada orden de mercado. No hay lógica de trailing ni break-even.

## Notas de uso
- Asigne el instrumento y la cartera deseados antes de iniciar la estrategia. Asegúrese de que el símbolo exponga `PriceStep` y `Decimals` para que la conversión de pips funcione correctamente.
- Como la lógica de entrada se ejecuta solo una vez, reiniciar la estrategia es la única forma de crear un nuevo ciclo hedge.
- Al probar en un simulador netting, el comportamiento realizado diferirá de MetaTrader: la orden de venta neutralizará la compra casi de inmediato.
