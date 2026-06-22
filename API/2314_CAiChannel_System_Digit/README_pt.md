# Estratégia CaiChannel Sistema Dígito
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port simplificado para StockSharp do especialista MetaTrader **i-CAiChannel System Digit**.

O algoritmo monitora um canal de volatilidade construído a partir de uma média móvel e desvio padrão (Bandas de Bollinger).
Quando uma vela fecha fora do canal e a próxima vela retorna ao interior, a estratégia opera na direção do retorno.

## Parâmetros
- `Length` – período da média móvel.
- `Width` – multiplicador do desvio padrão.
- `Candle Type` – período para processamento.

## Lógica de negociação
1. Assinar velas do período selecionado.
2. Calcular as Bandas de Bollinger com os parâmetros especificados.
3. Se a vela anterior fechou acima da banda superior e a vela atual fecha de volta ao interior, ir comprado.
4. Se a vela anterior fechou abaixo da banda inferior e a vela atual fecha de volta ao interior, ir vendido.
5. A posição é invertida quando ocorre o sinal oposto.

Todos os sinais são gerados apenas em velas concluídas.
