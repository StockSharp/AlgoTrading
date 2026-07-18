# Estrategia de Promedio de Potencia Bulls & Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Port del experto MetaTrader 5 `MySystem.mq5` ubicado en `MQL/22016`.
- Detecta reversiones de momentum promediando los valores de Elder Bulls Power y Bears Power calculados a partir de los extremos de las velas y una EMA.
- Entra **largo** cuando el promedio aumenta mientras aún está por debajo de cero (la presión bajista se está desvaneciendo) y **corto** cuando el promedio disminuye mientras aún está por encima de cero (la presión alcista se está desvaneciendo).
- Diseñado para una posición abierta a la vez; el stop-loss y take-profit son opcionales y se expresan en pips.

## Lógica del indicador
| Componente | Descripción |
|-----------|-------------|
| Exponential Moving Average (EMA) | Aplicado a los precios de cierre de velas. El parámetro `MaPeriod` controla la ventana de suavizado (predeterminado 5). |
| Bulls Power (derivado) | Calculado como `High - EMA`. Mide la fuerza alcista relativa a la EMA. |
| Bears Power (derivado) | Calculado como `Low - EMA`. Mide la fuerza bajista relativa a la EMA. |
| Potencia promedio | `(Bulls Power + Bears Power) / 2`. Este oscilador sintético se compara con su valor anterior para detectar cambios de momentum. |

La estrategia evalúa las últimas dos velas finalizadas. Las nuevas operaciones solo se evalúan cuando una vela está completamente completada para evitar el ruido intrabarra.

## Reglas de entrada
1. Esperar a que la EMA esté completamente formada (es decir, procesó al menos `MaPeriod` velas).
2. Calcular Bulls Power y Bears Power para la vela recién cerrada usando su high/low y el valor de la EMA.
3. Promediar ambas fuerzas para obtener la lectura actual del oscilador.
4. Comparar con el promedio anterior:
   - **Configuración larga**: promedio anterior `<` promedio actual **y** promedio actual `< 0`. Entrar largo si no hay posición existente.
   - **Configuración corta**: promedio anterior `>` promedio actual **y** promedio actual `> 0`. Entrar corto si está plano.
5. Una vez en una operación, depender de órdenes de protección opcionales o gestión manual. El algoritmo no genera señales de salida además del stop-loss/take-profit.

## Gestión de riesgos
- `StopLossPips`: Distancia de stop absoluta opcional en pips (0 deshabilita el stop). Convertido usando el `PriceStep` del instrumento.
- `TakeProfitPips`: Objetivo de beneficio absoluto opcional en pips (0 deshabilita el objetivo).
- Las órdenes de protección se registran tan pronto como la posición se abre a través de `StartProtection` con ejecución de mercado.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `OrderVolume` | 0.1 | Tamaño de orden para entradas de mercado. |
| `StopLossPips` | 15 | Distancia de stop-loss en pips. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | 95 | Distancia de take-profit en pips. Establecer en `0` para deshabilitar. |
| `MaPeriod` | 5 | Longitud de EMA usada para el cálculo de Bulls/Bears Power. |
| `CandleType` | Marco temporal de 1 hora | Serie de velas usada para todos los cálculos (cambiar para coincidir con su feed de datos). |

## Notas de uso
1. Adjuntar la estrategia a un instrumento y asegurarse de que `CandleType` coincida con el marco temporal previsto.
2. Ajustar `OrderVolume`, `StopLossPips` y `TakeProfitPips` para cumplir con los requisitos del broker.
3. Ejecutar la estrategia; se suscribe automáticamente a las velas, actualiza la EMA y emite órdenes de mercado en señales calificadas.
4. Solo puede existir una posición a la vez. Cuando un trade está activo, las nuevas señales se ignoran hasta que las órdenes de protección cierran la posición o se cierra manualmente.
5. Porque la versión MQL original usó `InpBarCurrent = 1`, este port siempre trabaja en velas completamente cerradas y compara valores consecutivos; no se realiza recálculo intrabarra.

## Diferencias vs. Experto MQL Original
- Usa la API `Strategy` de alto nivel de StockSharp con suscripciones de velas y vinculación de indicadores en lugar de acceso manual a buffers.
- Deriva automáticamente los pips de `PriceStep` en lugar de ajustes manuales de dígitos.
- Omite la gestión de órdenes comentada original y depende de la protección de posición incorporada.
- Mantiene la restricción de posición única de la fuente ignorando señales mientras existe una posición.

## Recomendaciones de prueba
- Realizar backtest en el símbolo/marco temporal previsto con datos históricos que incluyan precios high/low para un cálculo preciso de Bulls/Bears.
- Validar el comportamiento de las órdenes de protección con el tamaño del tick y el paso de volumen de su broker antes de ejecutar en vivo.
- Experimentar con diferentes valores de `MaPeriod` para adaptar la sensibilidad a la volatilidad del instrumento.
