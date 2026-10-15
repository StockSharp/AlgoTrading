# Estratégia de Velas MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o especialista MetaTrader "Exp_MACDCandle". Converte a saída de cor de um indicador de velas baseado em MACD em sinais de trading usando a API de alto nível do StockSharp.

## Conceito

O indicador MACD Candle constrói velas sintéticas a partir de valores MACD calculados nos preços de abertura e fechamento. Se o MACD calculado no fechamento estiver acima do MACD calculado na abertura, a vela é considerada de alta (cor 2). O oposto resulta em uma vela de baixa (cor 0). Uma cor neutra (1) aparece quando ambos os valores são iguais.

A estratégia abre posições compradas quando uma vela de alta aparece após uma não-altista, e abre posições vendidas quando uma vela de baixa segue uma não-baixista. As posições existentes são revertidas na nova direção.

## Parâmetros

- `FastLength` – período da EMA rápida para MACD (padrão 12).
- `SlowLength` – período da EMA lenta para MACD (padrão 26).
- `SignalLength` – período da linha de sinal para MACD (padrão 9).
- `CandleType` – tipo de vela utilizado para cálculos, padrão `TimeFrameCandle` com um período de quatro horas.

Todos os parâmetros são configuráveis e suportam otimização.

## Regras de Entrada e Saída

- **Entrada comprada**: o MACD no fechamento sobe acima do MACD na abertura enquanto a vela anterior não era altista.
- **Entrada vendida**: o MACD na abertura sobe acima do MACD no fechamento enquanto a vela anterior não era baixista.
- **Saída**: a estratégia fecha a posição atual quando ocorre um sinal oposto; nenhum stop‑loss ou take‑profit explícito é aplicado.

## Notas

- A estratégia usa ordens a mercado (`BuyMarket` e `SellMarket`).
- Os sinais são avaliados apenas em velas finalizadas para evitar ruído.
- O exemplo destina-se a fins educacionais e não inclui gestão de risco.
