# Estratégia de canal de preços de vinte pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia de Canal de Preço de Vinte Pips é uma conversão do consultor especialista original MetaTrader *20 pips* que combina um canal de preço estilo Donchian com filtros de média móvel de curto prazo. O algoritmo abre negociações somente quando a vela atual abre oposta à anterior, filtra a direção com médias móveis calculadas sobre preços típicos e gerencia saídas através de um alvo fixo de vinte pips apoiado por um trailing stop dinâmico baseado em canal.

A versão StockSharp mantém o espírito da abordagem original enquanto adapta o gerenciamento de pedidos ao API de alto nível. As ordens de mercado são usadas para entradas e saídas, as metas de lucro são monitoradas internamente e os níveis de stop são emulados com condições de canal de preço.

## Lógica de negociação

1. **Pilha de indicadores**
   - Uma média móvel simples de um período do preço típico (H+L+C)/3 atua como uma linha de base rápida que reflete o preço típico da vela anterior.
   - Uma média móvel simples lenta configurável (padrão 20) calculada nos preços de fechamento desempenha o papel do filtro `MA_Low` do EA.
   - Os indicadores mais altos e mais baixos com o mesmo período do canal de preço (padrão 20) emulam os buffers originais do indicador personalizado.

2. **Condições de entrada**
   - Configuração longa: o preço típico rápido anterior está acima da média móvel lenta anterior ** e ** a vela atual abre abaixo da abertura anterior. Após uma negociação perdida, o volume é multiplicado pelo fator de recuperação (padrão 2). O preço de entrada é registrado para acompanhar lucros e perdas.
   - Configuração curta: o preço típico rápido anterior está abaixo da média móvel lenta anterior ** e ** a vela atual abre acima da abertura anterior. O escalonamento de volume segue a mesma lógica de recuperação das negociações longas.

3. **Gerenciamento de saídas**
   - Uma meta fixa de lucro igual a `TakeProfitPips` multiplicada pela etapa do preço do instrumento é colocada quando a posição é aberta.
   - Um trailing stop orientado por canal imita a chamada `OrderModify` original. Quando a barra anterior ultrapassa o canal de preço (mudança de duas barras da lógica MT4), o stop de proteção é movido para o extremo anterior menos/mais o deslocamento final em pips. Se a próxima vela ultrapassar esse extremo, a posição sai imediatamente ao preço de abertura.
   - As saídas de take-profit, trailing stop e gap são todas executadas por meio de ordens de mercado enquanto rastreiam o preço de saída real para atualizar o sinalizador de vitória/perda para a escala do estilo martingale.

4. **Martingale recuperação**
   - Após cada posição perdedora fechada, o próximo tamanho da entrada é multiplicado por `RecoveryMultiplier`. As negociações lucrativas redefinem a bandeira e revertem para o volume base.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período principal usado para cálculos. | Velas de 1 hora |
| `ChannelPeriod` | Período de lookback para o canal estilo Donchian. | 20 |
| `SlowMaPeriod` | Comprimento do filtro de média móvel lenta. | 20 |
| `TakeProfitPips` | Distância em pips para a meta de lucro fixo. | 20 |
| `TrailingOffsetPips` | Offset usado ao apertar o batente até o extremo anterior. | 10 |
| `RecoveryMultiplier` | Multiplicador de volume aplicado após uma perda. | 2 |
| `Volume` | Volume base de negociação antes do escalonamento de recuperação. | 0,1 |

## Notas de uso

- A estratégia espera que `Security.PriceStep` reflita o valor do pip do instrumento negociado. Ajuste `TakeProfitPips` e `TrailingOffsetPips` se o símbolo usar uma definição de pip diferente.
- Como StockSharp usa ordens de mercado para saídas, os backtests podem mostrar derrapagens em comparação com as ordens de stop e limite MT4 originais. A lógica ainda reproduz os mesmos limites de preços.
- Os valores do canal são alterados para emular as chamadas `iCustom(..., shift=2)`; tenha isso em mente ao modificar o comportamento final.
- O multiplicador de recuperação pode ser definido como 1 para desativar o escalonamento no estilo martingale.
