# Estrategia de comercio de automóviles de NNFX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de comercio automático de NNFX** replica el flujo de trabajo de gestión y dimensionamiento de riesgos del panel original NNFX MetaTrader 4 dentro de StockSharp. En lugar de una interfaz gráfica, la estrategia expone comandos manuales a través de parámetros. Los operadores pueden solicitar entradas largas o cortas, reducir instantáneamente la exposición o aplicar una lógica de equilibrio y seguimiento que refleje al asesor experto.

Características clave:

- Tamaño de volatilidad impulsado por ATR con una anulación opcional para distancias de parada y toma de ganancias manuales.
- Las entradas de posiciones se dividen en dos partes: una con un objetivo proyectado y un corredor que se deja abierto para gestión discrecional.
- Los comandos de equilibrio y seguimiento funcionan según demanda, actualizando los niveles de parada almacenados sin activarse automáticamente en cada barra.
- Se puede incluir capital adicional al calcular el riesgo monetario, coincidiendo con el comportamiento del script MQL.

## Lógica de trading
1. Colección **ATR**: la estrategia se suscribe al tipo de vela configurado y procesa un indicador de rango verdadero promedio. Cuando `UsePreviousDailyAtr` está habilitado, copia el valor ATR del día anterior durante las primeras 12 horas del nuevo día de negociación, imitando el script original.
2. **Dimensionamiento basado en el riesgo**: con un comando manual `Buy` o `Sell`, el motor calcula el riesgo monetario por unidad utilizando la distancia de parada protectora y convierte el porcentaje de riesgo deseado en un volumen ejecutable.
3. **División de posición**: el volumen de entrada se divide en dos mitades. La primera mitad se liquida automáticamente cuando se toca el objetivo proyectado, mientras que la segunda mitad permanece hasta que el comerciante da más órdenes.
4. **Manejo de paradas**: las paradas iniciales se almacenan internamente y se evalúan en cada vela terminada. Los comandos manuales pueden empujar el tope hasta el punto de equilibrio o avanzarlo de acuerdo con la fórmula final de NNFX.
5. **Controles de salida**: `CloseAll` aplana inmediatamente el libro, mientras que las violaciones de los límites o los objetivos parciales desencadenan salidas del mercado que respetan los volúmenes calculados.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `RiskPercent` | `2.0` | Porcentaje del capital de la cuenta (más `AdditionalCapital`) arriesgado por operación. |
| `AdditionalCapital` | `0` | Capital adicional añadido a la base patrimonial al dimensionar las posiciones. |
| `UseAdvancedTargets` | `false` | Cambia las distancias de riesgo de múltiplos de ATR a valores de pips manuales. |
| `AdvancedStopPips` | `0` | Distancia de parada en pips cuando el modo avanzado está activo. |
| `AdvancedTakeProfitPips` | `0` | Distancia objetivo en pips para la salida parcial cuando el modo avanzado está activo. |
| `UsePreviousDailyAtr` | `true` | Copia el ATR diario anterior durante las primeras 12 horas de un nuevo día. |
| `AtrPeriod` | `14` | ATR longitud retrospectiva. |
| `AtrStopMultiplier` | `1.5` | Multiplicador aplicado a ATR al calcular la distancia de parada. |
| `AtrTakeProfitMultiplier` | `1.0` | Multiplicador aplicado a ATR al calcular la distancia de obtención de beneficios. |
| `CandleType` | `1 Minute` | Tipo de vela utilizado para ATR y seguimiento de precios. |
| `BuyCommand` | `false` | Bandera manual: configurada en `true` para solicitar una entrada larga. Se reinicia automáticamente. |
| `SellCommand` | `false` | Bandera manual: configurada en `true` para solicitar una entrada breve. Se reinicia automáticamente. |
| `BreakevenCommand` | `false` | Bandera manual: mueve el stop de protección al precio de entrada. Se reinicia automáticamente. |
| `TrailingCommand` | `false` | Bandera manual: aplique la fórmula final NNFX una vez. Se reinicia automáticamente. |
| `CloseAllCommand` | `false` | Bandera manual: cierre todas las posiciones abiertas al instante. Se reinicia automáticamente. |

## Notas de uso
- La estrategia requiere una cartera conectada y seguridad con metadatos `Step`, `StepPrice` y `VolumeStep` válidos para realizar cálculos de riesgo precisos.
- Los comandos se evalúan en velas terminadas, por lo que se debe recibir una nueva barra (o actualización de vela) después de alternar un parámetro manual.
- Cuando utilice distancias avanzadas, asegúrese de que tanto `AdvancedStopPips` como `AdvancedTakeProfitPips` estén completos; de lo contrario, los valores predeterminados basados ​​en ATR seguirán vigentes.
