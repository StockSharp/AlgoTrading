# Estratégia Diff TF MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Esta estratégia é um port para StockSharp do consultor especialista do MetaTrader "Diff_TF_MA_EA".
- Os sinais de trading vêm da comparação de uma média móvel simples calculada em um período superior com outra média móvel redimensionada para o período de trading.
- O código mantém apenas velas finalizadas, espelha as regras de cruzamento originais e fecha qualquer exposição oposta antes de abrir uma nova posição.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `MaPeriod` | Comprimento da média móvel simples calculada no período superior. |
| `CandleType` | Período de trading usado para geração de ordens. |
| `HigherCandleType` | Período superior que fornece a média móvel de referência. |
| `ReverseSignals` | Inverte as regras de cruzamento (comprar no cruzamento de baixa e vender no cruzamento de alta). |
| `Volume` | Volume da estratégia usado pelas chamadas `BuyMarket`/`SellMarket` (definido através da propriedade base `Strategy.Volume`). |

## Lógica de trading
1. Assinar tanto o período de trading (`CandleType`) quanto o período superior (`HigherCandleType`).
2. Construir uma média móvel simples com comprimento `MaPeriod` no período superior.
3. Converter o comprimento do período superior no período de trading multiplicando pela relação de durações de períodos e executar outra média móvel nas velas de trading.
4. Armazenar os dois últimos valores completados para ambas as médias móveis e verificar cruzamentos em cada vela de trading finalizada.
5. Abrir ou reverter para uma posição comprada quando a MA do período superior cruza acima da MA de trading (a menos que `ReverseSignals` seja `true`).
6. Abrir ou reverter para uma posição vendida quando a MA do período superior cruza abaixo da MA de trading (a menos que `ReverseSignals` seja `true`).
7. As posições são niveladas e invertidas enviando volume suficiente para compensar qualquer exposição existente.

## Notas de uso
- Escolher períodos compatíveis: o período superior geralmente deve ser maior que o período de trading para que o comprimento redimensionado seja significativo.
- O volume padrão é `1`. Ajustar `Strategy.Volume` antes de iniciar a estratégia se outro tamanho for necessário.
- Os stops e take-profits da versão do MetaTrader não são reproduzidos; o gerenciamento de risco pode ser anexado através das proteções do StockSharp se necessário.
- Quando `ReverseSignals` está habilitado, as ações de alta e baixa são trocadas enquanto o restante da lógica permanece inalterado.
