# Estratégia Bear Bulls Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do especialista MetaTrader 5 "Exp_Bear_Bulls_Power". Ela usa um indicador Bear/Bulls Power suavizado para detectar reversões de tendência.

## Como funciona

1. Calcular o preço mediano de cada vela: `(High + Low) / 2`.
2. Suavizar o preço mediano com uma média móvel de comprimento `FirstLength`.
3. Calcular a diferença entre o preço mediano e sua média móvel.
4. Aplicar uma segunda suavização com uma média móvel de comprimento `SecondLength`.
5. Determinar a direção da tendência comparando o valor suavizado atual com o anterior.
6. Gerar sinais quando a direção mudar:
   - Uma virada para cima acima de zero abre uma posição comprada.
   - Uma virada para baixo abaixo de zero abre uma posição vendida.

## Parâmetros

- **Candle Type** – período das velas processadas.
- **First Length** – período para suavização do preço.
- **Second Length** – período para suavização do sinal.

A estratégia usa ordens de mercado e funciona apenas com velas concluídas.
