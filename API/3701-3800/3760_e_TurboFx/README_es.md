# Estrategia clásica e-TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **e-TurboFx Classic** es una adaptación directa de C# del asesor experto MetaTrader 4 que se encuentra en `MQL/7262/e-TurboFx.mq4`. Detecta el agotamiento del impulso tras una racha de velas fuertes con cuerpos progresivamente más grandes y entra en la dirección opuesta. La versión StockSharp utiliza la estrategia de alto nivel API con suscripciones de velas, órdenes de protección automáticas y parámetros compatibles con la interfaz de usuario.

## Lógica comercial
1. Suscríbase al tipo de vela configurado e inspeccione solo las velas terminadas.
2. Mida el tamaño del cuerpo de la vela (`|close - open|`) para detectar la expansión.
3. Mantener dos contadores:
   - **Secuencia bajista**: cuenta velas bajistas consecutivas con cuerpos más grandes que la vela bajista anterior.
   - **Secuencia alcista**: cuenta velas alcistas consecutivas con cuerpos más grandes que la vela alcista anterior.
4. Restablezca ambas secuencias cuando aparezca un doji (abrir es igual a cerrar) o cuando una posición ya esté abierta. Esto imita el comportamiento original de EA que mantiene solo una operación a la vez.
5. **Entrada larga:** cuando la longitud de la secuencia bajista alcance el `SequenceLength` configurado, envíe una orden de compra de mercado y reinicie inmediatamente los contadores.
6. **Entrada corta:** cuando la longitud de la secuencia alcista alcance `SequenceLength`, envíe una orden de venta de mercado y reinicie los contadores.
7. Los niveles opcionales de stop-loss y take-profit se traducen desde distancias de puntos a StockSharp pasos de precio.

Por lo tanto, el algoritmo espera un movimiento similar a una capitulación en el que cada vela se acelera en la misma dirección. La siguiente orden de reversión intenta atenuar ese impulso extremo.

## Detalles de implementación
- Utiliza `SubscribeCandles().Bind(ProcessCandle)` para procesar velas terminadas sin gestión manual de indicadores.
- Se integra con `StartProtection` para que las distancias de stop-loss y take-profit se conviertan en pasos del precio de cambio (`UnitTypes.Step`).
- Los parámetros se registran a través de `Param(...)` para que aparezcan en la interfaz de usuario y se puedan optimizar.
- La estrategia funciona con cualquier instrumento que exponga un `PriceStep` válido; de lo contrario, las distancias de parada/objetivo deben permanecer en `0`.
- Mientras una posición está activa, la detección de señal se pausa y los contadores internos se borran, al igual que el script original MQL que se negaba a acumular órdenes.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `SequenceLength` | Número de velas terminadas consecutivas con cuerpos en expansión necesarias para activar una entrada. | `3` |
| `TakeProfitSteps` | Distancia de obtención de beneficios medida en pasos de precio (ticks). `0` desactiva el objetivo. | `120` |
| `StopLossSteps` | Distancia de stop-loss medida en pasos de precio (ticks). `0` desactiva la parada. | `70` |
| `TradeVolume` | Volumen de entradas al mercado. Cambiarlo actualiza la propiedad `Volume` al instante. | `0.1` |
| `CandleType` | Plazo de vela utilizado para el análisis. El valor predeterminado es velas de 1 hora. | `1 hour` |

## Notas de uso
- La estrategia espera datos de velas limpios. Al cambiar de instrumento o período de tiempo, permita que los cachés se reconstruyan para que los contadores reflejen únicamente velas nuevas.
- Debido a que el sistema se basa en una estricta expansión del cuerpo, los cuerpos de velas pequeños o iguales restablecen la secuencia. Ajuste `SequenceLength` cuando opere en períodos de tiempo más ruidosos.
- Realice una prueba retrospectiva de múltiples combinaciones de marco temporal/volumen para encontrar instrumentos en los que los movimientos de agotamiento sean lo suficientemente frecuentes como para compensar los diferenciales y los deslizamientos.
- Valide siempre el comportamiento en un entorno sandbox antes de habilitar el comercio real.
