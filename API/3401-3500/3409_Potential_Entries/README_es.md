# Estrategia de entradas potenciales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de entradas potenciales** replica la lógica del asesor experto `EA_PotentialEntries.mq5` original. Analiza pares de las velas terminadas más recientes y emite operaciones cuando aparecen patrones de impulso o reversión específicos de dos velas. La estrategia funciona en una dirección a la vez (alcista o bajista), seleccionable a través del parámetro `Pattern Side`. Los niveles de parada de protección se recalculan en cada entrada para reflejar la ubicación de la parada MetaTrader original en el extremo del par de velas analizado.

La implementación utiliza el nivel alto de StockSharp API: se suscribe al tipo de vela configurado, procesa el flujo dentro de `ProcessCandle`, abre posiciones con `BuyMarket`/`SellMarket` y cierra operaciones a través de salidas de mercado cuando se supera el precio de parada rastreado internamente. Los gráficos representan la serie de velas junto con las operaciones de estrategia para una inspección visual rápida.

## Datos y parámetros
| grupo | Nombre | Descripción |
| --- | --- | --- |
| generales | Lado del patrón | Dirección del escaneo del patrón: `Bullish` busca reversiones alcistas, `Bearish` busca reversiones bajistas. |
| Comercio | Volumen comercial | Tamaño de orden de mercado utilizado para cada entrada. La estrategia aplana la exposición opuesta antes de abrir una nueva posición. |
| generales | Tipo de vela | Serie de velas utilizadas para el reconocimiento de patrones (predeterminado: velas por hora). |

## Lógica de trading
La estrategia evalúa la vela finalizada más reciente (`C1`) junto con la vela anterior (`C2`). Todas las medidas de mecha y cuerpo se calculan en unidades de precio.

### Modo alcista
Cuando `Pattern Side = Bullish`, las siguientes configuraciones activan una entrada larga:
1. **Martillo alcista**
   - `C1` cierra por encima de su apertura mientras que `C2` es bajista.
   - La mecha inferior de `C1` mide al menos el doble del cuerpo y más del triple de la mecha superior.
   - Se envía una orden de compra de mercado y el nivel de parada se establece en el menor de los mínimos de `C1` y `C2`.
2. **Martillo invertido alcista**
   - `C1` es alcista y `C2` es bajista.
   - La mecha superior de `C1` tiene al menos el doble del cuerpo y al menos el triple de la mecha inferior.
   - Ejecuta la misma orden y lógica de parada que la configuración del martillo.
3. **Creador de impulso alcista**
   - `C1` y `C2` son ambos alcistas.
   - El rango de `C1` es mayor que el rango de `C2` y el cuerpo de `C1` es al menos el doble del cuerpo de `C2`.
   - Abre una posición larga con el stop por debajo del mínimo mínimo del par.

### Modo bajista
Cuando `Pattern Side = Bearish`, las siguientes configuraciones activan una entrada corta:
1. **Estrella fugaz**
   - `C1` cierra por debajo de su apertura mientras que `C2` es alcista.
   - La mecha superior de `C1` tiene al menos el doble del cuerpo y al menos el triple de la mecha inferior.
   - Se envía una orden de venta de mercado con el stop colocado por encima del máximo superior de `C1` y `C2`.
2. **Hombre ahorcado**
   - `C1` es bajista y `C2` es alcista.
   - La mecha inferior de `C1` mide al menos el doble del cuerpo y más del triple de la mecha superior.
   - Abre una posición corta y utiliza la misma lógica de parada que la estrella fugaz.
3. **Creador de impulso bajista**
   - `C1` y `C2` son bajistas.
   - El cuerpo de `C1` es más grande que el cuerpo de `C2` y el rango de `C1` es al menos el doble del rango de `C2`.
   - Entra en corto y almacena el stop por encima del máximo máximo de las velas analizadas.

### Gestión de paradas y manejo de posiciones
- Sólo un modo direccional está activo a la vez. Antes de iniciar una operación, la estrategia cierra cualquier posición en la dirección opuesta.
- Cada entrada registra un precio stop en el extremo del par de velas. Con la llegada de cada nueva vela terminada, la estrategia comprueba si el mínimo (para largos) o el máximo (para cortos) viola el nivel almacenado y cierra la posición con una orden de mercado si se activa.
- Cuando no hay ninguna posición abierta, el valor de parada almacenado se borra, lo que garantiza que los niveles obsoletos nunca se reutilicen.

## Notas de uso
- Elija el modo `Bullish` o `Bearish` dependiendo de si desea buscar oportunidades largas o cortas.
- Las velas horarias predeterminadas se pueden reemplazar con cualquier otro tipo de datos de vela disponible.
- Aún no existe un puerto para Python, tal como se solicitó. Solo se proporciona la implementación de C#.
- La estrategia no establece objetivos de ganancias. Las salidas dependen únicamente de la lógica de parada basada en velas o de la intervención manual.
