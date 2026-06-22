# Estratégia Delta RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador **Delta RSI**. Dois indicadores RSI com períodos diferentes são comparados:

- O **RSI Rápido** reage rapidamente às mudanças de preço.
- O **RSI Lento** atua como filtro de tendência.

Uma posição comprada é aberta na barra seguinte a um sinal **Up** quando:

1. O RSI lento está acima do limiar `Level`.
2. O RSI rápido é maior que o RSI lento.
3. A barra anterior mostrou o estado Up e a barra atual não está mais em Up.

Uma posição vendida é aberta na barra seguinte a um sinal **Down** quando:

1. O RSI lento está abaixo de `100 - Level`.
2. O RSI rápido é menor que o RSI lento.
3. A barra anterior mostrou o estado Down e a barra atual não está mais em Down.

Flags opcionais permitem habilitar ou desabilitar a abertura e o fechamento de posições compradas e vendidas separadamente.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `FastPeriod` | Período do RSI rápido. |
| `SlowPeriod` | Período do RSI lento. |
| `Level` | Nível limiar para o RSI lento. |
| `BuyPosOpen` / `SellPosOpen` | Permitir abertura de posições compradas/vendidas. |
| `BuyPosClose` / `SellPosClose` | Permitir fechamento de posições compradas/vendidas. |
| `CandleType` | Período das velas de entrada. |

A estratégia subscreve velas do período selecionado, calcula ambos os valores de RSI e processa sinais em cada vela finalizada. Quando um sinal aparece, a estratégia opcionalmente fecha a posição oposta e abre uma nova na direção do sinal.
