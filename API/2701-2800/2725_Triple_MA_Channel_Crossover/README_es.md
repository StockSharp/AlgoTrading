# Estrategia de Cruce de Canal de Triple MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La **Estrategia de Cruce de Canal de Triple MA** negocia rupturas direccionales cuando una media móvil rápida se mueve a través de
una media móvil media y una lenta. Un canal de precio estilo Donchian se usa para gestionar salidas y para proporcionar niveles
opcionales automáticos de stop-loss y take-profit. La conversión está basada en el "3MACross EA" original de MetaTrader y mantiene
su estructura de media móvil configurable, controles de riesgo y lógica de trailing.

La estrategia escala hasta un número configurable de posiciones, soporta objetivos de riesgo manuales basados en pips, y puede
seguir el canal para salidas adaptativas. Cuando está habilitado, el disparador de break-even empuja el stop loss al precio de
entrada más un búfer de seguridad.

## Lógica de Trading
- **Criterios de entrada**
  - *Largo:* la media móvil rápida cruza por encima de las medias media y lenta. Si `Trade On Close` está habilitado el cruce
    debe ocurrir en una vela completamente cerrada; de lo contrario la señal larga se permite mientras la media rápida permanezca
    por encima de ambas medias más lentas.
  - *Corto:* la media móvil rápida cruza por debajo de las medias media y lenta con la misma lógica de confirmación.
  - Las posiciones existentes en el lado opuesto se cierran e invierten inmediatamente. El escalado en la misma dirección se
    permite hasta que se alcanza `Max Positions`.
- **Criterios de salida**
  - Precio alcanzando el take-profit configurado o el objetivo basado en canal.
  - Precio tocando el nivel de stop dinámico (distancia manual, trailing stop, movimiento de break-even o stop basado en canal).
  - El trailing stop opcional se ajusta después de que el precio se mueve a favor por al menos la distancia del paso de trailing.

## Gestión de Riesgo
- Los stops y objetivos pueden definirse manualmente en pips o derivarse del canal de precio cuando `Auto SL/TP` está habilitado.
- La lógica de trailing stop y break-even reflejan el asesor experto original. El stop se mueve solo en la dirección favorable y
  nunca se relaja.
- El canal Donchian proporciona límites naturales de soporte/resistencia que pueden usarse para la colocación automática de
  stop-loss y take-profit.
- `Max Positions` limita el número de pasos de escalado, previniendo el piramidado descontrolado.

## Parámetros Clave
| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Tamaño de orden para cada paso de escalado. |
| `Stop Loss (pips)` | Distancia fija para el stop protector. Establecer en `0` para deshabilitar. |
| `Take Profit (pips)` | Distancia fija para el objetivo de beneficio. Establecer en `0` para deshabilitar. |
| `Trailing Stop (pips)` | Distancia usada por el trailing stop. `0` deshabilita el trailing. |
| `Trailing Step (pips)` | Avance mínimo requerido antes de actualizar el trailing stop. |
| `Break Even (pips)` | Beneficio requerido antes de fijar un stop de break-even. |
| `Auto SL/TP` | Usar el canal Donchian en lugar de distancias fijas para la colocación de stop-loss y take-profit. |
| `Trade On Close` | Requerir que los cruces sean confirmados en una vela cerrada. Si está deshabilitado, la alineación de medias se verifica cada barra. |
| `Max Positions` | Número máximo de pasos de escalado por dirección. |
| `Fast/Middle/Slow MA Period` | Longitud de las medias móviles. |
| `Fast/Middle/Slow MA Shift` | Desplazamiento opcional (en barras) aplicado a cada media móvil. |
| `Fast/Middle/Slow MA Type` | Modo de cálculo de media móvil (Simple, Exponencial, Suavizada, Ponderada). |
| `Channel Period` | Lookback para el máximo/mínimo del canal Donchian. |
| `Candle Type` | Marco temporal de las velas procesadas por la estrategia. |

## Notas de Implementación
- Las distancias en pips se convierten usando `Security.PriceStep`. Para instrumentos sin un tamaño de tick válido la estrategia
  recurre a una distancia de `1` unidad de precio por pip.
- La gestión automática de canal mantiene los niveles de stop-loss y take-profit moviéndose solo más cerca del precio actual;
  nunca se amplían.
- La activación de break-even reutiliza el paso de trailing como un búfer adicional, coincidiendo con el comportamiento original del EA.
- La estrategia está diseñada para uso con las APIs de alto nivel de StockSharp y maneja el renderizado de gráficos (MAs y canal
  Donchian) para análisis visual.
- Asegúrese de que la profundidad de datos históricos sea suficiente para la media móvil lenta y el período del canal para que
  las señales de cruce sean válidas.

## Uso
1. Adjuntar la estrategia a un instrumento y establecer el marco temporal de velas deseado.
2. Configurar los períodos/métodos de medias móviles para que coincidan con el EA original o su adaptación.
3. Elegir entre configuraciones de riesgo manuales basadas en pips o habilitar salidas automáticas de canal.
4. Iniciar la estrategia; se suscribirá a las velas configuradas, calculará indicadores y negociará cuando se cumplan las
   condiciones de cruce.
5. Monitorear el trailing stop y los ajustes de break-even a través de los registros y superposiciones de gráficos.

> **Aviso:** El trading automatizado conlleva un riesgo significativo. Pruebe la estrategia exhaustivamente con datos históricos y
> en un entorno de simulación antes de desplegar en mercados en vivo.
