# Estrategia de canal equidistante
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia de canal equidistante** adapta el asesor experto MQL4 original "Equidistant Channel" a la API de alto nivel de StockSharp. La estrategia analiza cruces de la línea MACD y gestiona posiciones existentes mediante toques de Bollinger Bands, lógica de breakeven y objetivos trailing basados en dinero.

Cuando la línea MACD cruza por encima de su señal, la estrategia abre posiciones largas; cuando cruza por debajo de la señal, abre posiciones cortas. Mientras una operación está activa, la estrategia vigila salidas cuando el precio alcanza Bollinger Bands, cuando la ganancia flotante llega a objetivos monetarios o porcentuales configurables, o cuando se infringe un umbral de drawdown trailing. Un modo breakeven replica la implementación de MetaTrader moviendo el stop de protección cuando la ganancia supera un número configurable de pasos de precio.

## Indicadores
- **MACD (12, 26, 9)** - genera señales de entrada en cruces entre la línea MACD y su línea de señal.
- **Bollinger Bands (20, 2)** - proporcionan niveles de salida cuando el cierre de la vela toca la banda superior o inferior.

## Gestión de posiciones
- Distancias opcionales de stop loss, take profit y trailing stop expresadas en puntos de precio mediante `StartProtection`.
- Lógica de take profit y trailing basada en dinero que rastrea la ganancia flotante usando metadatos de precio/tamaño de paso del instrumento.
- Take profit porcentual calculado desde el valor inicial de la cartera.
- Modo breakeven que empuja el stop a la entrada más un desplazamiento cuando la ganancia alcanza un disparador definido.

## Parámetros
| Grupo | Nombre | Predeterminado | Descripción |
| --- | --- | --- | --- |
| Trading | Volumen | 1 | Volumen de orden para nuevas entradas. |
| General | Tipo de vela | 5 minutos | Serie de velas usada para los cálculos. |
| Indicadores | MACD rápido | 12 | Longitud de EMA rápida para MACD. |
| Indicadores | MACD lento | 26 | Longitud de EMA lenta para MACD. |
| Indicadores | Señal MACD | 9 | Longitud de la línea de señal para MACD. |
| Indicadores | Período BB | 20 | Período retrospectivo de Bollinger Bands. |
| Indicadores | Desviación BB | 2 | Anchura de Bollinger Bands en desviaciones estándar. |
| Riesgo | Stop Loss | 20 | Distancia del stop loss en puntos de precio. |
| Riesgo | Take Profit | 50 | Distancia del take profit en puntos de precio. |
| Riesgo | Trailing Stop | 40 | Distancia del trailing stop en puntos de precio. |
| Riesgo | Usar TP (dinero) | false | Cierra cuando la ganancia flotante alcanza un objetivo monetario absoluto. |
| Riesgo | Dinero TP | 10 | Valor absoluto de take profit en la divisa de la cuenta. |
| Riesgo | Usar TP (%) | false | Cierra cuando la ganancia flotante alcanza un porcentaje del capital inicial. |
| Riesgo | Porcentaje TP | 10 | Porcentaje del capital inicial para el take profit porcentual. |
| Riesgo | Habilitar trailing | true | Habilita la lógica trailing sobre la ganancia flotante. |
| Riesgo | Activar trailing | 40 | Nivel de ganancia (divisa) que arma la lógica trailing. |
| Riesgo | Paso trailing | 10 | Drawdown máximo permitido desde el pico de ganancia (divisa). |
| Riesgo | Usar stop BB | true | Habilita salidas cuando el precio toca Bollinger Bands. |
| Riesgo | Usar breakeven | true | Habilita el comportamiento breakeven. |
| Riesgo | Disparador breakeven | 10 | Ganancia (pasos de precio) necesaria para armar el stop breakeven. |
| Riesgo | Desplazamiento breakeven | 5 | Desplazamiento (pasos de precio) aplicado al nivel breakeven. |

## Notas
- La estrategia funciona con un solo instrumento que proporcione metadatos válidos de `PriceStep` y `StepPrice` para que los cálculos monetarios sean precisos.
- El módulo de trailing de ganancia sigue el comportamiento de MetaTrader: cuando la ganancia flotante supera el umbral de activación, la estrategia registra el máximo en curso y cierra la operación cuando el drawdown supera el paso trailing configurado.
- La lógica breakeven replica el EA original usando disparadores y desplazamientos basados en pasos de precio.
- Todos los comentarios dentro del código de la estrategia están escritos en inglés, como exigen las directrices del proyecto.
