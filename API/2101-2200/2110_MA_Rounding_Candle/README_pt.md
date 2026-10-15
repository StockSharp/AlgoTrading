# Estratégia MA Rounding Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma interpretação do consultor especialista original do MQL5 "MA Rounding Candle". Ela usa duas médias móveis suavizadas aplicadas aos preços de abertura e fechamento das velas. A posição relativa dessas médias define a cor de uma vela sintética: verde quando o fechamento suavizado está acima da abertura, vermelha quando o fechamento está abaixo da abertura e cinza quando são iguais. Uma mudança de cor em relação à barra anterior gera sinais de negociação.

## Algoritmo

1. Para cada vela concluída, os valores de abertura e fechamento são suavizados com uma média móvel simples de comprimento configurável.
2. A cor da vela é definida comparando os valores suavizados:
   - **Vela de alta** – o fechamento suavizado é maior que a abertura suavizada.
   - **Vela de baixa** – o fechamento suavizado é menor que a abertura suavizada.
   - **Neutra** – ambos os valores são iguais.
3. Se a vela anterior foi de alta e a atual não for de alta, a estratégia entra em uma posição comprada e fecha qualquer posição vendida.
4. Se a vela anterior foi de baixa e a atual não for de baixa, a estratégia entra em uma posição vendida e fecha qualquer posição comprada.

## Parâmetros

- **MaLength** – período das médias móveis suavizadoras (padrão 12).
- **CandleType** – período das velas processadas.

## Notas

A estratégia demonstra como recriar sinais de um indicador personalizado usando apenas as ferramentas integradas do StockSharp. Não é aplicado stop loss ou take profit; as posições são revertidas imediatamente quando o sinal oposto aparece.
