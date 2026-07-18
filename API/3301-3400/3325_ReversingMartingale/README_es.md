# Estrategia Reversing Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La **estrategia Reversing Martingale** es un port directo en C# del asesor experto de MetaTrader "Reversing Martingale EA". Mantiene continuamente una sola posición de mercado y alterna la dirección de la operación después de cada operación cerrada. Las operaciones perdedoras activan una progresión martingala de volumen, mientras que las rentables reinician el ciclo al tamaño de lote inicial. Todas las posiciones se protegen con niveles simétricos de stop-loss y take-profit expresados en puntos de precio.

La estrategia no depende de indicadores ni de estructura de mercado. Simplemente reacciona a posiciones completadas y mantiene la exposición de capital activa en todo momento (salvo que el trading esté desactivado).

## Lógica principal
1. **Configuración inicial**
   - Al iniciar, la estrategia envía de inmediato una orden de mercado usando `Start Volume` y la dirección configurada en `First Trade Side`.
   - Las órdenes protectoras de stop-loss y take-profit se adjuntan usando la distancia especificada en `Target (points)`.
2. **Gestión de posición**
   - Solo puede haber una posición abierta a la vez. La estrategia espera hasta que la posición actual se cierre por completo por sus órdenes protectoras o por acciones externas.
   - Después de cada salida, la estrategia invierte la dirección de operación (compra -> venta o venta -> compra).
   - Si la última operación realizó una pérdida, el volumen de la siguiente orden equivale al tamaño de posición anterior multiplicado por `Lot Multiplier`. En caso contrario, el volumen se reinicia a `Start Volume`.
3. **Continuación del ciclo**
   - Una vez determinados el nuevo volumen y dirección, se envía inmediatamente la siguiente orden de mercado, manteniendo activo el ciclo martingala alternante.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| **Start Volume** | Volumen inicial usado al comienzo de cada ciclo ganador. |
| **Lot Multiplier** | Multiplicador de volumen aplicado después de una operación perdedora. Debe ser mayor que 1. |
| **First Trade Side** | Dirección de la primera operación cuando comienza la sesión de estrategia. |
| **Target (points)** | Distancia en pasos de precio usada para stop-loss y take-profit. |
| **Order Comment** | Etiqueta de texto opcional asignada a cada orden de mercado generada. |

## Notas adicionales
- La distancia de paso de precio se convierte en `UnitTypes.Step` y se pasa a `StartProtection`, por lo que stop-loss y take-profit están siempre activos.
- Los ajustes de volumen respetan el paso de volumen, mínimos y máximos de la seguridad mediante el helper `NormalizeVolume`.
- La estrategia espera eventos de ejecución del conector; si el trading se pausa o el conector está offline, el ciclo martingala se reanudará cuando el trading vuelva a estar permitido.
