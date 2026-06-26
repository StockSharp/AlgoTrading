# Estrategia de Exp Color PEMA Digit Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Exp Color PEMA Digit Tm Plus** es un port directo del asesor experto de MetaTrader 5 "Exp_ColorPEMA_Digit_Tm_Plus". La estrategia reconstruye el indicador original de Media Móvil Exponencial Quíntuple (PEMA) y reproduce cada flag de permiso de trading presente en el EA. Las órdenes se ejecutan en la serie de velas seleccionada solo después de que el indicador confirma un cambio de color y ha transcurrido el período de espera opcional (`Signal Bar`).

La versión de StockSharp mantiene las mismas opciones de gestión monetaria, controles de stop/objetivo y salida basada en tiempo que existían en la implementación MQL. Cada configuración se expone a través de `StrategyParam<T>` para soportar la configuración de UI y la optimización.

## Lógica del indicador
* El indicador alimenta una cascada de ocho medias móviles exponenciales usando la `PEMA Length` y el `Applied Price` configurados.
* La línea final se redondea a los `Rounding Digits` solicitados, exactamente como en el indicador original.
* La pendiente de la línea redondeada produce tres estados:
  * **Up (magenta)** – presión alcista, posible configuración larga.
  * **Flat (gris)** – neutral, sin acción.
  * **Down (azul dodger)** – presión bajista, posible configuración corta.
* La estrategia almacena el estado del indicador de cada vela completada para poder referenciar barras más antiguas cuando `Signal Bar` es mayor que cero.

## Reglas de trading
1. **Detección de señal** – en una vela completada, evaluar el estado del indicador que tiene `Signal Bar` velas de antigüedad y compararlo con el estado anterior.
2. **Configuración larga** – cuando el estado pasa a *Up* desde cualquier otra cosa:
   * encolar una entrada larga si `Allow Long Entries` está habilitado;
   * encolar una salida de cortos existentes si `Allow Short Exits` está habilitado.
3. **Configuración corta** – cuando el estado pasa a *Down* desde cualquier otra cosa:
   * encolar una entrada corta si `Allow Short Entries` está habilitado;
   * encolar una salida de largos existentes si `Allow Long Exits` está habilitado.
4. **Capa de ejecución** – las acciones en cola se ejecutan solo cuando:
   * la estrategia está en línea y el trading está permitido;
   * se ha alcanzado el timestamp de activación vinculado a la vela fuente; y
   * las reglas de dimensionamiento de posición permiten un volumen no nulo.
5. **Gestión de riesgo** –
   * los niveles opcionales de stop-loss y take-profit se derivan del precio de llenado usando las mismas distancias en puntos que en MetaTrader;
   * `Use Time Exit` cierra posiciones que excedan el tiempo de vida configurado en `Holding Minutes`;
   * las señales opuestas pueden aplanar inmediatamente la exposición si el permiso de salida respectivo está activo.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| Money Management | Valor base usado por las reglas de dimensionamiento de posición. |
| Money Mode | Elige entre dimensionamiento basado en lotes o modelos de porcentaje de saldo/margen libre. |
| Stop Loss (points) | Distancia al stop loss en puntos de precio. |
| Take Profit (points) | Distancia al take profit en puntos de precio. |
| Allowed Deviation | Parámetro de marcador de posición preservado del EA por completitud. |
| Allow Long Entries / Allow Short Entries | Habilitar o deshabilitar la apertura de operaciones en cada dirección. |
| Allow Long Exits / Allow Short Exits | Habilitar o deshabilitar el cierre de operaciones cuando aparecen señales opuestas. |
| Use Time Exit | Activa la lógica de aplanamiento basada en tiempo. |
| Holding Minutes | Tiempo máximo de tenencia de una posición, expresado en minutos. |
| Candle Type | Serie de velas procesada por la estrategia. Por defecto H4. |
| PEMA Length | Longitud usada para las ocho etapas EMA en la PEMA Quíntuple. |
| Applied Price | Precio fuente usado en el cálculo del indicador. |
| Rounding Digits | Dígitos decimales usados para redondear la salida del indicador. |
| Signal Bar | Número de barras completadas a esperar antes de evaluar una señal. |

## Notas de uso
* Colocar la estrategia dentro de un conector StockSharp que proporcione acceso al instrumento deseado y la serie de velas.
* Configurar los parámetros para que coincidan con la configuración de MetaTrader que desea replicar.
* Ejecutar backtests o trading en vivo según sea necesario; la estrategia solo reacciona a velas completamente cerradas.

## Estado de conversión
* **Versión C#** – implementada (`CS/ExpColorPemaDigitTmPlusStrategy.cs`).
* **Versión Python** – no creada (según instrucción).
