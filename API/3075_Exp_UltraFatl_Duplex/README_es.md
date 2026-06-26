# Estrategia Exp UltraFATL Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Exp UltraFATL Duplex** es una conversión en C# del asesor experto de MetaTrader 5 `Exp_UltraFatl_Duplex`. El sistema ejecuta dos pipelines independientes del indicador UltraFATL: uno dedicado a oportunidades largas y otro ajustado para configuraciones cortas. Cada pipeline evalúa una escalera de valores FATL suavizados y cuenta cuántas etapas están subiendo o bajando. El equilibrio entre los contadores alcista y bajista define la dirección de la siguiente operación.

## Lógica de trading
1. Suscribirse al marco temporal de velas configurado para cada bloque direccional.
2. Filtrar el precio aplicado con el kernel FATL (filtro digital de 39 coeficientes).
3. Alimentar la serie filtrada a través de una escalera de medias móviles cuyos tamaños aumentan por el paso configurado. La escalera usa el método de suavizado especificado por el usuario.
4. Comparar valores consecutivos dentro de la escalera para contar votos alcistas y bajistas. Suavizar ambos contadores con una segunda media móvil.
5. Evaluar los contadores en el desplazamiento de señal seleccionado (predeterminado: una vela completamente cerrada):
   - El **bloque largo** abre una posición cuando la vela anterior mostró dominio alcista, pero la vela actual muestra contadores cruzándose hacia abajo (alcistas ≤ bajistas). Cierra la posición larga cuando los bajistas superan a los alcistas en la vela anterior.
   - El **bloque corto** funciona en la dirección opuesta: abre un corto cuando la vela anterior está dominada por bajistas y la vela actual cruza hacia arriba (alcistas ≥ bajistas). Cierra el corto cuando los alcistas lideran en la vela anterior.
6. Los niveles opcionales de stop-loss y take-profit se evalúan sobre datos de velas usando el paso de precio del instrumento.

La estrategia aplica una posición neta: las señales cortas cierran los largos existentes antes de abrir, y viceversa. Se usan órdenes de mercado para entradas y salidas.

## Parámetros
### Bloque largo
- **Long Volume** – tamaño de orden al abrir una operación larga.
- **Allow Long Entries** – habilitar o deshabilitar nuevas posiciones largas.
- **Allow Long Exits** – permitir el cierre de largos en señales opuestas.
- **Long Candle Type** – marco temporal usado para el pipeline UltraFATL largo.
- **Long Applied Price** – fuente de precio (cierre, típico, DeMark, etc.) alimentada al kernel FATL.
- **Long Trend Method / Start Length / Phase / Step / Steps** – configuración de suavizado de la escalera.
- **Long Counter Method / Counter Length / Counter Phase** – configuración de suavizado para los contadores alcista/bajista.
- **Long Signal Bar** – número de velas completadas usadas como desplazamiento de señal (valores menores a 1 se tratan como 1).
- **Long Stop (pts)** – distancia de stop-loss opcional en pasos de precio.
- **Long Target (pts)** – distancia de take-profit opcional en pasos de precio.

### Bloque corto
Configuraciones simétricas para el pipeline corto: **Short Volume**, **Allow Short Entries**, **Allow Short Exits**, **Short Candle Type**, **Short Applied Price**, **Short Trend Method / Start Length / Phase / Step / Steps**, **Short Counter Method / Counter Length / Counter Phase**, **Short Signal Bar**, **Short Stop (pts)**, **Short Target (pts)**.

## Notas de implementación
- Los métodos de suavizado se mapean a indicadores de StockSharp. Las opciones basadas en Jurik reutilizan `JurikMovingAverage`; métodos como `Parabolic` y `T3` se aproximan con medias móviles exponenciales o Jurik porque los kernels personalizados originales no están disponibles.
- Los niveles de stop-loss y take-profit se evalúan sobre los máximos/mínimos de velas; no son órdenes protectoras del lado del servidor.
- Los desplazamientos de señal menores a una barra no pueden reproducirse porque el port de StockSharp reacciona solo a velas terminadas. Por lo tanto, establecer la barra de señal en cero se comporta idénticamente a un desplazamiento de uno.
- Ambos pipelines de indicadores dibujan sus contadores suavizados en áreas de gráfico dedicadas para inspección visual.

## Uso
Agregar la estrategia a su solución de StockSharp, configurar los bloques direccionales de acuerdo con su plan de trading y ejecutarla dentro del Designer, Shell o Runner. Asegurarse de que el instrumento proporcione la serie de velas requerida y que los parámetros `LongVolume`/`ShortVolume` estén configurados con el tamaño de orden deseado.
