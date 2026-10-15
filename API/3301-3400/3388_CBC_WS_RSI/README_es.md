# CBC_WS_RSI Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **CBC_WS_RSI** es una implementación StockSharp de alto nivel del asesor experto MQL5 que combina los patrones de velas "Tres soldados blancos" y "Tres cuervos negros" con confirmación RSI. La estrategia se centra en identificar fuertes reversiones de múltiples velas y solo ingresa a una operación cuando el impulso del mercado, medido por RSI, concuerda con el patrón. Las salidas están controladas por cruces de umbrales RSI y gestión de riesgos opcional a través de protecciones de limitación de pérdidas y toma de ganancias.

La estrategia se suscribe a una serie de velas configurables y procesa datos exclusivamente en velas completamente formadas. Toda la lógica se implementa utilizando API (`SubscribeCandles().Bind(...)`) de alto nivel de StockSharp sin acceso directo a los buffers de indicador.

## Lógica de trading
### Configuración larga
1. Detecta tres velas alcistas consecutivas que forman el patrón **Tres Soldados Blancos**:
   - Cada vela se cierra por encima de su apertura.
   - Cada cierre es mayor que el cierre anterior.
   - La segunda y tercera vela se abren dentro del cuerpo de la vela anterior.
2. Confirma que el valor RSI de la vela actual está **por debajo o igual al nivel de confirmación larga** (predeterminado 40).
3. Si la cuenta es plana, la estrategia compra `Volume` lotes en el mercado. Si existe una posición corta, se cubre antes de abrir una nueva posición larga.

### Configuración corta
1. Detecta tres velas bajistas consecutivas que forman el patrón **Tres Cuervos Negros**:
   - Cada vela cierra por debajo de su apertura.
   - Cada cierre es más bajo que el cierre anterior.
   - La segunda y tercera vela se abren dentro del cuerpo de la vela anterior.
2. Confirma que el valor RSI de la vela actual está **por encima o igual al nivel de confirmación corta** (predeterminado 60).
3. Si la cuenta es plana, la estrategia vende `Volume` lotes en el mercado. Si existe una posición larga, se cierra antes de abrir una nueva posición corta.

### Reglas de salida
- **Cerrar posiciones largas:** RSI cruza por debajo del nivel de salida superior (predeterminado 70) o del nivel de salida inferior (predeterminado 30).
- **Cerrar cortos:** RSI cruza por encima del nivel de salida inferior (predeterminado 30) o del nivel de salida superior (predeterminado 70).
- **Protección:** Los valores opcionales de stop-loss y take-profit se pueden definir como porcentajes del precio de entrada. Cuando son distintos de cero, se administran a través de `StartProtection`.

Todas las condiciones de salida utilizan los dos valores RSI más recientes para detectar un cruce de niveles, lo que garantiza que las operaciones se cierren tan pronto como el impulso contradiga la posición activa.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Tipo de datos de vela y período de tiempo al que suscribirse. | plazo de 1 hora |
| `RsiPeriod` | RSI período utilizado para la confirmación. | 37 |
| `LongConfirmationLevel` | Valor máximo RSI que permite una entrada larga. | 40 |
| `ShortConfirmationLevel` | Valor mínimo RSI que permite una entrada corta. | 60 |
| `LowerExitLevel` | Nivel RSI utilizado para detectar una reversión del impulso cerca del territorio de sobreventa. | 30 |
| `UpperExitLevel` | Nivel RSI utilizado para detectar una reversión del impulso cerca del territorio de sobrecompra. | 70 |
| `StopLossPercent` | Stop-loss opcional en porcentaje; 0 desactiva la protección. | 1 |
| `TakeProfitPercent` | Toma de ganancias opcional en porcentaje; 0 desactiva la protección. | 2 |

Todos los parámetros numéricos se pueden optimizar a través del optimizador integrado gracias a `SetCanOptimize(true)`.

## Visualización
Cuando un área del gráfico está disponible, la estrategia dibuja:
- La serie de velas seleccionada.
- El indicador RSI.
- Operaciones ejecutadas, lo que facilita la inspección de detecciones y salidas de patrones.

## Notas de uso
- Asegúrese de que `Volume` esté configurado antes de iniciar la estrategia.
- Funciona en cualquier instrumento que admita OHLC datos de velas.
- La lógica de detección de patrones filtra las velas tipo doji al requerir cuerpos de vela distintos de cero.
- Las confirmaciones RSI protegen contra señales falsas durante reversiones débiles, manteniendo la estrategia alineada con el impulso.
