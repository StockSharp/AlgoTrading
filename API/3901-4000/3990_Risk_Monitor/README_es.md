# Estrategia de seguimiento de riesgos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Risk Monitor Strategy es una versión del MetaTrader4 asesor experto `risk.mq4`. El guión original nunca abrió operaciones; en lugar de eso
determinó cuántos lotes el comerciante podría implementar de forma segura en función del saldo de la cuenta y un porcentaje de riesgo definido por el usuario. esto
La versión StockSharp mantiene el mismo espíritu: realiza diagnósticos continuos de cuentas, calcula tamaños de operaciones sugeridos y monitorea
ganancias flotantes y realizadas, y publica los resultados directamente en el comentario de la estrategia para una rápida toma de decisiones.

A diferencia de las estrategias convencionales, Risk Monitor Strategy no envía órdenes automáticamente. Su función es de supervisión: da la
comerciante una instantánea de la exposición actual, la capacidad disponible de acuerdo con el presupuesto de riesgo elegido y la rentabilidad de los cerrados
posiciones. La línea de comentarios se actualiza cada vez que cambian las posiciones, PnL o las operaciones, de modo que la información siempre refleje las últimas novedades.
estado de la cartera.

## Cálculos
La estrategia deriva las cifras mostradas en el comentario de tres grupos de datos:

1. **Tamaño de lote base**: calculado como `AccountBalance / 1000` y alineado con el paso de volumen de seguridad. Esto refleja el original.
Lógica MT4 donde cada 1000 unidades de saldo corresponden a 1 lote estándar.
2. **Tamaño del lote de riesgo**: multiplica los lotes base por `Risk % / 100`, alinea el resultado con el paso de volumen y representa cuántos
Los lotes podrán abrirse respetando el presupuesto de riesgo configurado.
3. **Lotes abiertos y diferencia**: compara la posición neta absoluta con el tamaño del lote de riesgo. Si el comerciante está por debajo del umbral,
la diferencia muestra cuántos lotes quedan disponibles antes de alcanzar el límite. Una pequeña diferencia negativa que es menor que
el paso de volumen se redondea a cero para evitar ruidos confusos.

Para las ganancias, la estrategia distingue entre valores flotantes y realizados:

* **PnL flotante**: leído de la propiedad de la estrategia `PnL` y expresado tanto en unidades de precio como como porcentaje del actual
valor de la cartera.
* **Beneficio realizado** – acumulado de operaciones propias. El componente divide cada relleno de cierre en partes positivas y negativas,
aplica la comisión reportada y mantiene un total acumulado. La cifra final también se convierte en un porcentaje del capital social para
coincida con la lectura MT4.

## Parámetros
* **% de riesgo**: parte del saldo de la cuenta que se puede comprometer para nuevas posiciones. Predeterminado: `10`. El parámetro está expuesto para
optimización para que se puedan realizar pruebas retrospectivas de diferentes presupuestos de riesgo rápidamente.

## Formato de comentario
La estrategia actualiza el comentario con tres líneas:

1. `Base lots`, `Risk lots`, `Open lots`, `Lots to adjust`: vista rápida de las métricas de tamaño de posición.
2. `Risk`, `Floating PnL`: configuración de riesgo, beneficio flotante en unidades monetarias y beneficio flotante como porcentaje del saldo.
3. `Realized profit` – beneficio cerrado acumulado y su porcentaje.

Todos los valores se redondean de forma similar al script MT4, respetando el paso del lote de seguridad y utilizando dos decimales para los valores monetarios.
números. Debido a que el resultado se encuentra en el comentario, es inmediatamente visible en el gráfico o en la cuadrícula de estrategia sin necesidad de abrirlo.
paneles adicionales.

## Notas de uso
* Adjunte la estrategia al instrumento cuyo equilibrio y posición desea supervisar. Funciona con posiciones netas (sin estilo MT4
cobertura) al igual que el propio StockSharp.
* La estrategia tolera el comercio manual: reacciona a cualquier confirmación comercial para mantener las estadísticas sincronizadas.
* El comentario se borra automáticamente cuando la estrategia se detiene o se reinicia, lo que evita que los valores obsoletos persistan entre sesiones.
* No se proporciona ninguna implementación de Python; el paquete API contiene solo la versión C#.
