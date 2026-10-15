# Estratégia HelloSmart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma abordagem simples de grid trading que abre posições em apenas uma direção. Uma nova ordem é colocada cada vez que o mercado se move um número configurado de ticks contra a última entrada. Quando o volume acumulado da posição atinge um limite, o tamanho da próxima ordem é multiplicado. Todas as posições são fechadas quando o lucro ou perda total atinge os limites predefinidos.

## Parâmetros
- **Trade Direction** – escolher 1 para abrir apenas posições compradas ou 2 para abrir apenas posições vendidas.
- **Step** – número de ticks de preço que o mercado deve mover antes de adicionar outra posição.
- **Initial Lot** – volume base para a primeira ordem.
- **Threshold Volume** – tamanho acumulado da posição que aciona a multiplicação do lote.
- **Maximum Lot** – limite superior para o volume de qualquer ordem individual.
- **Profit Target** – valor de lucro em moeda após o qual todas as posições são fechadas.
- **Loss Limit** – valor de perda em moeda após o qual todas as posições são fechadas.
- **Lot Multiplier** – fator aplicado à próxima ordem quando o volume limite é excedido.
- **Candle Type** – série de velas usada para medir o movimento de preços.
