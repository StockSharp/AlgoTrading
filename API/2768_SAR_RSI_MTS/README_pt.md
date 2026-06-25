# Estratégia SAR RSI MTS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia SAR RSI MTS** é uma tradução direta do consultor especialista original do MetaTrader 5 "SAR RSI MTS" para a API de alto nível do StockSharp. O sistema segue a direção do indicador Parabolic SAR e confirma as entradas com o Índice de Força Relativa (RSI). Funciona apenas em velas completas (período padrão de 1 hora) e respeita um limite configurável no tamanho líquido da posição.

## Indicadores e dados

- **Parabolic SAR** (`Acceleration = SarStep`, `AccelerationStep = SarStep`, `AccelerationMax = SarMax`).
- **Índice de Força Relativa** com período personalizável e nível neutro (padrão 50).
- Velas fornecidas por `CandleType`, que assume por padrão dados de período horário.

Internamente a estratégia calcula um valor de pip a partir dos metadados do instrumento. Se o símbolo tiver 3 ou 5 casas decimais, multiplica o passo de preço por 10, correspondendo ao tratamento de pips do programa MQL original.

## Lógica de entrada

Uma nova operação é avaliada no fechamento de cada vela concluída após ambos os indicadores produzirem valores válidos:

- **Configuração comprada**
  1. O valor do Parabolic SAR da barra anterior está abaixo do fechamento atual e o SAR atual aumentou em relação ao valor anterior.
  2. O RSI está acima do limiar neutro e está subindo em comparação com a leitura anterior.
  3. Se a conta já estiver líquida vendida, a estratégia primeiro compra volume suficiente para inverter a posição e então abre uma nova posição comprada dimensionada pelo parâmetro `Volume`, respeitando o limite `MaxPosition`.

- **Configuração vendida**
  1. O valor anterior do Parabolic SAR está acima do fechamento atual e o SAR atual diminuiu.
  2. O RSI está abaixo do limiar neutro e está caindo em comparação com o valor anterior.
  3. A exposição comprada existente é zerada antes de estabelecer a nova posição vendida. Posições vendidas adicionais são permitidas até que a posição absoluta atinja `MaxPosition`.

Todas as comparações usam a precisão do instrumento para que os testes de igualdade correspondam ao helper `CompareDoubles` original do MQL.

## Saída e gestão de risco

Os controles de risco são avaliados antes de verificar novas entradas em cada vela concluída:

- **Stop-loss fixo** em pips convertidos em unidades de preço e aplicado ao preço médio de entrada da posição líquida atual.
- **Take-profit fixo** em pips, tratado simetricamente ao stop-loss.
- **Trailing stop** que fica ativo somente após o lucro não realizado exceder `TrailingStop + TrailingStep`. O stop é movido em passos discretos, imitando a rotina "Trailing" da estratégia MQL.
- Se nenhum dos itens acima se aplicar, o estado do trailing é reiniciado sempre que a posição se tornar plana.

Todas as saídas fecham a posição líquida inteira (comprada ou vendida). Quando uma regra de proteção é acionada, a estratégia ignora a avaliação de sinais para a mesma barra, espelhando o comportamento de ordens stop do lado do broker na implementação original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `StopLossPips` | Distância do stop-loss expressa em pips. Um valor de `0` desabilita o stop de proteção. |
| `TakeProfitPips` | Distância do take-profit em pips. Desabilitado quando definido como `0`. |
| `TrailingStopPips` | Distância do trailing stop. Desabilitado quando definido como `0`. |
| `TrailingStepPips` | Melhoria mínima de preço necessária antes de avançar o trailing stop. |
| `SarStep` | Passo de aceleração para Parabolic SAR; também usado como fator de aceleração inicial. |
| `SarMax` | Fator de aceleração máximo para Parabolic SAR. |
| `RsiPeriod` | Período de lookback para o indicador RSI. |
| `RsiNeutralLevel` | Limiar RSI que separa a tendência de alta e de baixa (padrão 50). |
| `CandleType` | Assinatura de velas usada para cálculos (padrão 1 hora). |
| `MaxPosition` | Posição líquida absoluta máxima permitida pela estratégia. |

## Notas adicionais

- A configuração padrão reproduz as entradas originais do EA: stop de 10 pips, alvo de 40 pips, trailing stop de 15/5 pips, Parabolic SAR `0.05/0.5` e período RSI `14`.
- O volume é controlado pela propriedade base `Strategy.Volume`. O dimensionamento de posição respeita `MaxPosition` e lida automaticamente com reversões.
- As ligações de indicadores e o roteamento de ordens dependem inteiramente da API de alto nível do StockSharp sem acesso manual a séries, garantindo conformidade com as diretrizes do projeto.
