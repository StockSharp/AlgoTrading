# Estratégia Simples iVIDyA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp de alto nível do especialista MetaTrader **"iVIDyA Simple"**. Ele negocia um único símbolo rastreando uma Média Dinâmica de Índice Variável (VIDYA) que se adapta à dinâmica do mercado por meio do Chande Momentum Oscillator (CMO). Sempre que a vela finalizada mais recente cruza a linha VIDYA deslocada, a estratégia abre uma posição de mercado na direção do rompimento e, opcionalmente, anexa ordens protetoras de stop-loss e take-profit.

## Lógica de negociação
1. Os dados da vela são lidos no período configurado (`CandleType`).
2. O CMO com período `CmoPeriod` está vinculado à série de velas. Seu valor absoluto dimensiona dinamicamente o fator de suavização do VIDYA. O fator base é igual a `2 / (EmaPeriod + 1)` assim como a implementação original de MQL.
3. Um valor VIDYA contínuo é mantido. Em cada vela finalizada, o algoritmo:
   - Seleciona o preço aplicado (`AppliedPrice`) da vela (fechamento, abertura, mediana, etc.).
   - Atualiza o VIDYA com o coeficiente de suavização adaptativo.
   - Armazena valores históricos para emular a opção `MA shift` de MetaTrader.
4. A vela é comparada com o valor VIDYA deslocado (`MaShift` barras atrás):
   - Se a vela abrir abaixo de VIDYA e fechar acima dela, um sinal de **compra** será gerado.
   - Se a vela abrir acima de VIDYA e fechar abaixo dela, um sinal de **venda** será gerado.
5. Antes de abrir uma nova posição, a estratégia nivela qualquer exposição oposta, negociando o volume total necessário para reverter.
6. Após cada entrada, `SetStopLoss` e `SetTakeProfit` são chamados quando as respectivas distâncias são positivas.

Isso reflete o consultor especialista original, que acionou pedidos estritamente em novas barras, usou um VIDYA calculado a partir de períodos CMO e EMA e anexou paradas opcionais expressas em pontos.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `Volume` | `1` | Volume base de negociação usado para pedidos. A exposição existente é compensada automaticamente ao inverter posições. |
| `StopLossPoints` | `150` | Distância de stop-loss em etapas de preço. Defina como `0` para desativar. |
| `TakeProfitPoints` | `460` | Distância de lucro em etapas de preço. Defina como `0` para desativar. |
| `CmoPeriod` | `15` | Comprimento do oscilador Chande Momentum que determina o peso adaptativo do VIDYA. |
| `EmaPeriod` | `12` | Comprimento EMA que define o coeficiente de suavização base na fórmula VIDYA. |
| `MaShift` | `1` | Número de velas concluídas usadas para deslocar a linha VIDYA para frente, correspondendo à entrada `ma_shift` do indicador MetaTrader. |
| `AppliedPrice` | `Close` | Fonte de preço passada para o cálculo VIDYA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `CandleType` | `TimeSpan.FromMinutes(5)` | Tipo de vela e prazo usado para todos os cálculos e sinais. |

## Notas adicionais
- As ordens de proteção são gerenciadas por meio do API integrado de alto nível (`SetStopLoss`/`SetTakeProfit`), enquanto o código MQL original realizava verificações manuais em relação aos níveis de congelamento.
- A estratégia assina apenas velas finalizadas, replicando a restrição de execução da "nova barra" de MetaTrader.
- O histórico do VIDYA é cortado automaticamente para que o consumo de memória permaneça pequeno mesmo quando `MaShift` for grande.
- Todos os comentários dentro do código são escritos em inglês para atender aos requisitos do projeto.
