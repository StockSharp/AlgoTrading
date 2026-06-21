# Estratégia RK's Framework Auto Color Gradient
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina Bollinger Bands %B e RSI em um único oscilador, mapeia-o para um gradiente de cor e opera quando cruza a linha central.

## Lógica
- Calcula Bollinger Bands %B e o Índice de Força Relativa.
- Normaliza ambos com um processo estocástico e os calcula em média.
- Converte o resultado em um gradiente de cor selecionável.
- Compra quando o valor médio está acima de zero.
- Vende quando o valor médio está abaixo de zero.
