# Estratégia PA Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem do especialista MQL5 **Exp_PA_Oscillator.mq5**. Aplica duas médias móveis exponenciais (EMAs) aos preços de fechamento das velas e analisa a derivada de sua diferença.

## Lógica

1. Calcular EMAs rápida e lenta.
2. Calcular a diferença entre elas e rastrear sua mudança em relação ao valor anterior.
3. Determinar um código de cor para a derivada:
   - **0** – a derivada é positiva e o MACD está subindo.
   - **1** – a derivada é zero.
   - **2** – a derivada é negativa e o MACD está caindo.
4. Usar as cores das duas últimas velas concluídas para gerar sinais:
   - Duas barras atrás a cor era `0` e a barra anterior mudou de `0` → abrir posição comprada e fechar posição vendida.
   - Duas barras atrás a cor era `2` e a barra anterior mudou de `2` → abrir posição vendida e fechar posição comprada.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `FastLength` | Comprimento do EMA rápido. |
| `SlowLength` | Comprimento do EMA lento. |
| `BuyPosOpen` | Habilitar abertura de posições compradas. |
| `SellPosOpen` | Habilitar abertura de posições vendidas. |
| `BuyPosClose` | Habilitar fechamento de posições compradas. |
| `SellPosClose` | Habilitar fechamento de posições vendidas. |
| `CandleType` | Período de velas utilizado para os cálculos. |

## Notas

- Apenas velas concluídas são processadas.
- Ordens a mercado são usadas para entradas e saídas.
- Esta implementação foca na clareza e fins educacionais, não na rentabilidade.
