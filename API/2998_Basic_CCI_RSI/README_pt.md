# Estratégia Básica CCI RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia Básica CCI RSI reproduz o consultor especialista original do MetaTrader que aguarda que tanto o Commodity Channel Index (CCI) quanto o Relative Strength Index (RSI) confirmem o momentum por duas velas fechadas consecutivas antes de entrar em um trade. A versão StockSharp mantém as regras de gerenciamento de dinheiro baseadas em pips, converte-as em passos de preço automaticamente e adiciona o mesmo comportamento de trailing stop que foi implementado com modificações de posição em MQL5.

## Como a estratégia negocia

1. No fechamento de cada vela (por padrão, a cada hora) a estratégia recebe novos valores de CCI e RSI.
2. Entradas compradas requerem que **ambos** os indicadores permaneçam acima de seus respectivos limites superiores para a vela atual e a anterior fechada. Entradas vendidas requerem que ambos permaneçam abaixo de seus limites inferiores pelas últimas duas velas.
3. Quando um sinal ocorre, a estratégia abre uma posição com o volume configurado (fechando qualquer exposição oposta) e imediatamente calcula preços fixos de stop-loss e take-profit usando as distâncias em pips do script original.
4. Enquanto a posição está aberta, a estratégia verifica constantemente se o range da vela tocou os níveis de stop ou take e sai a mercado se algum for atingido.
5. Um trailing stop replica a implementação do MetaTrader: uma vez que o lucro excede `TrailingStopPips + TrailingStepPips`, o stop protetor é movido para `TrailingStopPips` atrás do fechamento atual (para comprados) ou acima dele (para vendidos). Ajustes posteriores requerem um `TrailingStepPips` adicional de lucro antes de apertar novamente.

Este fluxo mantém a lógica próxima ao expert MQL5 fonte enquanto usa assinaturas de velas de alto nível do StockSharp e indicadores.

## Gerenciamento de risco

- **Stop-loss**: distância fixa em pips convertida para o passo de preço do instrumento. Desabilitado quando definido como zero.
- **Take-profit**: distância fixa em pips convertida para o passo de preço do instrumento. Desabilitado quando zero.
- **Trailing stop**: distância em pips opcional com um buffer de passo que imita a função `Trailing()` do expert advisor. Desabilitado quando `TrailingStopPips` é zero.
- **Dimensionamento de posição**: controlado através da propriedade `Volume` da estratégia; o lote padrão é um contrato.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `StopLossPips` | Distância em pips entre o preço de entrada e a ordem de stop-loss. |
| `TakeProfitPips` | Distância em pips entre o preço de entrada e o alvo de take-profit. |
| `TrailingStopPips` | Lucro (em pips) necessário para começar a trailear o stop. |
| `TrailingStepPips` | Lucro adicional (em pips) necessário antes de cada novo ajuste de trailing. |
| `CciPeriod` | Período de averaging para o indicador CCI. |
| `RsiPeriod` | Período de averaging para o indicador RSI. |
| `RsiLevelUp` | Nível de sobrecompra que deve ser excedido para validar trades comprados. |
| `RsiLevelDown` | Nível de sobrevenda que deve ser ultrapassado para validar trades vendidos. |
| `CciLevelUp` | Limite superior do CCI que confirma momentum otimista. |
| `CciLevelDown` | Limite inferior do CCI que confirma momentum pessimista. |
| `CandleType` | Período usado para agregação de velas e cálculos de indicadores. |

## Valores padrão

- `StopLossPips` = 125
- `TakeProfitPips` = 60
- `TrailingStopPips` = 5
- `TrailingStepPips` = 5
- `CciPeriod` = 12
- `RsiPeriod` = 15
- `RsiLevelUp` = 75
- `RsiLevelDown` = 30
- `CciLevelUp` = 80
- `CciLevelDown` = -95
- `CandleType` = velas de 1 hora

## Notas adicionais

- Distâncias em pips são escaladas automaticamente: se o instrumento usa 3 ou 5 casas decimais, a estratégia multiplica o passo de preço por dez, correspondendo à lógica do "ponto ajustado" do MetaTrader.
- Entradas são avaliadas apenas em velas fechadas para evitar repintagem e espelhar a condição original de "nova barra" no expert advisor.
- Saídas sempre usam ordens de mercado, fornecendo comportamento determinístico dentro do ambiente de backtesting do StockSharp.

## Tags de classificação

- Categoria: Confirmação de osciladores
- Direção: Bidirecional
- Indicadores: CCI, RSI
- Stops: Fixo e trailing (baseados em pips)
- Complexidade: Básico
- Período: Intradiário a swing (padrão 1 hora)
- Sazonalidade: Não
- Redes neurais: Não
- Divergência: Não
- Nível de risco: Moderado
