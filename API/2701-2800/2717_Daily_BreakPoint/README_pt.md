# Estratégia de Ponto de Ruptura Diário
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Ponto de Ruptura Diário** é uma portagem do StockSharp do expert advisor MetaTrader 5 «Daily BreakPoint» (build 19498). O algoritmo monitora a distância entre o preço atual e a abertura diária. Quando o movimento a partir da abertura diária excede um limiar configurável e a vela anterior atende a rigorosos requisitos de tamanho do corpo, a estratégia entra na direção do rompimento ou reverte a exposição existente dependendo do sinalizador `CloseBySignal`.

A estratégia trabalha com dois fluxos de dados ao mesmo tempo:

1. Velas intradiárias definidas pelo parâmetro `CandleType` para geração de sinais.
2. Velas diárias usadas para rastrear o preço de abertura da sessão mais recente.

## Lógica de negociação
1. Quando uma nova vela intradiária termina, a estratégia lê o último preço de abertura diária e calcula os níveis de rompimento usando `BreakPointPips` (convertido em preços absolutos via tamanho do tick do instrumento).
2. O tamanho do corpo da vela recém-fechada deve estar dentro do intervalo `[LastBarSizeMinPips, LastBarSizeMaxPips]`.
3. **Configuração de alta**
   - A vela deve fechar acima de sua abertura (`Close > Open`).
   - O fechamento deve ser pelo menos `BreakPointPips` acima da abertura diária.
   - O preço de rompimento (abertura diária + ponto de ruptura) deve estar dentro do corpo da vela.
   - Se `CloseBySignal = false`, a estratégia abre uma posição comprada. Caso contrário, fecha qualquer exposição comprada aberta e estabelece uma posição vendida.
4. **Configuração de baixa** espelha o caso de alta: uma vela baixista cujo fechamento está pelo menos `BreakPointPips` abaixo da abertura diária e cujo corpo contém o nível de rompimento aciona uma entrada vendida (`CloseBySignal = false`) ou uma reversão para uma posição comprada (`CloseBySignal = true`).
5. As ordens são enviadas como ordens a mercado usando o `OrderVolume` configurado. O tamanho da posição é cumulativo, portanto múltiplos sinais podem escalar a posição em qualquer direção.

## Gestão de riscos
- **Stop Loss / Take Profit**: Alvos fixos opcionais definidos em pips (`StopLossPips`, `TakeProfitPips`). Quando definido como zero, o nível correspondente é desabilitado. A estratégia avalia máximos e mínimos de velas para detectar toques.
- **Stop Trailing**: Habilitado quando `TrailingStopPips > 0`. Uma vez que o lucro aberto excede `TrailingStopPips + TrailingStepPips`, o stop é arrastado atrás do preço por `TrailingStopPips`. O parâmetro de passo evita ajustes frequentes de stop em mercados laterais.
- Todas as distâncias de preço são convertidas de pips usando o `PriceStep` do instrumento. Para cotações de 3 ou 5 decimais, o pip equivale a dez passos de preço, replicando o comportamento do expert advisor original.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `OrderVolume` | Volume base usado para cada ordem a mercado. |
| `CloseBySignal` | Se `true`, a estratégia fecha posições existentes e abre a direção oposta quando um sinal de rompimento aparece. |
| `BreakPointPips` | Distância da abertura diária necessária para confirmar um rompimento. |
| `LastBarSizeMinPips` / `LastBarSizeMaxPips` | Tamanho mínimo e máximo do corpo da vela gatilho. |
| `TrailingStopPips` | Distância do stop trailing. Defina como `0` para desabilitar o trailing. |
| `TrailingStepPips` | Movimento adicional necessário antes de cada ajuste de trailing. |
| `StopLossPips` | Stop loss fixo opcional. `0` desabilita. |
| `TakeProfitPips` | Take profit fixo opcional. `0` desabilita. |
| `CandleType` | Série de velas intradiária usada para geração de sinais. |

## Notas de uso
- A estratégia subscreve automaticamente tanto velas intradiárias quanto diárias. Certifique-se de que o provedor de dados suporte os períodos solicitados.
- Como a lógica avalia velas concluídas, as ordens são enviadas no preço de fechamento da barra de sinal.
- A conversão de pip pressupõe preços no estilo Forex. Revise os padrões ao aplicar a estratégia a instrumentos com tamanhos de tick não convencionais.
