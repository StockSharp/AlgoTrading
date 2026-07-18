# Estrategia PriceChannel Signal v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
PriceChannel Signal v2 es un sistema de seguimiento de tendencias creado alrededor de un canal Donchian modificado. El asesor experto original MQL5 observa las transiciones en la tendencia del canal, las condiciones de reingreso opcionales cuando el precio retrocede a través de las bandas y los niveles de salida protectores derivados del mismo rango. El puerto StockSharp mantiene el comportamiento original: negocia una sola posición a la vez, reacciona solo ante velas completadas y puede restringirse a una ventana intradiaria.

## Lógica comercial
1. El nivel alto y bajo del canal Donchian se calcula sobre el `ChannelPeriod` configurado.
2. El rango bruto se desplaza mediante dos multiplicadores:
   * **Factor de Riesgo** – comprime las bandas de entrada hacia la mediana del canal.
   * **Nivel de salida**: construye un segundo par de bandas internas que activan las salidas.
3. Se mantiene un estado de tendencia:
   * Cuando el cierre supera la banda de entrada superior, la tendencia se vuelve alcista.
   * Cuando el cierre cae por debajo de la banda de entrada inferior, la tendencia se vuelve bajista.
   * En caso contrario se mantiene la tendencia anterior.
4. Señales generadas a partir de ese estado:
   * **Entrada larga** – la tendencia cambia de bajista a alcista.
   * **Entrada corta** – la tendencia cambia de alcista a bajista.
   * **Reentrada larga** – opcional, el precio cierra nuevamente por encima de la banda superior mientras la tendencia ya es alcista.
   * **Reentrada corta** – opcional, el precio cierra nuevamente por debajo de la banda inferior mientras la tendencia ya es bajista.
   * **Salida larga** – opcional, el precio cierra por debajo de la banda de salida alcista después de estar por encima de ella en la barra anterior.
   * **Salida corta** – opcional, el precio cierra por encima de la banda de salida bajista después de estar por debajo de ella en la barra anterior.
5. Sólo se permite un pedido por barra y por dirección. La estrategia se niega a abrir una nueva posición si ya hay otra activa.
6. Si el filtro de tiempo intradiario está habilitado, todas las señales anteriores se ignoran fuera de la ventana configurada.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `ChannelPeriod` | Donchian longitud retrospectiva utilizada para calcular el canal de precios y las bandas de salida. |
| `RiskFactor` | Desplazamiento de las bandas de entrada (0–10). Los valores más bajos ensanchan las bandas, los valores más altos las estrechan. |
| `ExitLevel` | Desplazamiento de las bandas de salida. Debe ser mayor que `RiskFactor` para permanecer dentro del rango de entrada. |
| `UseReEntry` | Permite operaciones de reingreso cuando el precio retrocede a través de la banda activa. |
| `UseExitSignals` | Habilita la lógica de salida basada en las bandas protectoras internas. |
| `CandleType` | Marco de tiempo utilizado para construir velas y ejecutar los indicadores. |
| `UseTimeControl` | Alterna la ventana de negociación intradía. |
| `StartHour` / `StartMinute` | Inicio inclusivo de la ventana de negociación cuando el control de tiempo está activo. |
| `EndHour` / `EndMinute` | Fin exclusivo de la ventana de negociación cuando el control de tiempo está activo. |

## Reglas de entrada y salida.
* **Entrar en largo:** la tendencia cambia a alcista o se activa la condición de reingreso, la posición actual es plana y la barra está dentro de la ventana de tiempo permitida.
* **Entrar en corto:** la tendencia cambia a bajista o se dispara una condición de reingreso en corto, la posición actual es plana y la barra está dentro de la ventana de tiempo permitida.
* **Salida larga:** `UseExitSignals` está habilitado y el cierre cae por debajo de la banda de salida después de estar por encima de ella en la barra anterior.
* **Salida corta:** `UseExitSignals` está habilitado y el cierre se eleva por encima de la banda de salida después de estar por debajo de ella en la barra anterior.

## Notas adicionales
* La estrategia funciona con órdenes de mercado y no piramidal.
* Los valores del indicador se procesan únicamente en velas terminadas para evitar el repintado dentro de la barra.
* El volumen predeterminado es 1 contrato si no se proporciona explícitamente.
* El control de tiempo sigue el comportamiento original de EA: la hora de finalización es exclusiva y se admite el ajuste hasta la medianoche.
