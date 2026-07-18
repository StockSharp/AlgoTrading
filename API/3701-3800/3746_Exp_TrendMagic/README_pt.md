# Estratégia Exp TrendMagic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Exp TrendMagic é uma porta direta do MetaTrader 5 consultor especialista "Exp_TrendMagic". Ele monitora as mudanças de cor do indicador TrendMagic, que combina um índice de canal de commodities (CCI) com um canal Average True Range (ATR). Quando o indicador muda de cor, a estratégia fecha posições do lado oposto e opcionalmente abre uma nova negociação na direção da nova tendência.

A conversão mantém as opções originais de gerenciamento de dinheiro, compensação de sinal configurável (`Signal Bar`) e os mesmos alternadores de permissão para entrar ou sair de negociações longas e curtas.

## Lógica de negociação
1. **Entradas de indicadores**
   - `CCI` (Commodity Channel Index) com período configurável e preço aplicado.
   - `ATR` (Average True Range) com período configurável.
   - O valor TrendMagic é calculado como:
     - Quando CCI ≥ 0: `TrendMagic = Low - ATR`, fixado para evitar diminuir a linha de suporte.
     - Quando CCI <0: `TrendMagic = High + ATR`, fixado para evitar aumentar a linha de resistência.
   - A cor da linha resultante é **0** para alta (suporte abaixo do preço) e **1** para baixa (resistência acima do preço).

2. **Avaliação de sinal**
   - A estratégia armazena as cores do indicador em ordem cronológica para emular o buffer MetaTrader e usa o deslocamento `Signal Bar` para ler a barra concluída mais recentemente.
   - Se a cor anterior (`Signal Bar + 1`) for **0** e a cor atual (`Signal Bar`) for **1**, o algoritmo trata isso como uma mudança de alta: ele fecha qualquer posição curta e, se permitido, abre uma negociação longa.
   - Se a cor anterior for **1** e a cor atual for **0**, o algoritmo fecha qualquer posição longa aberta e, se permitido, entra em uma negociação curta.
   - Os sinalizadores de permissão de negociação (`Allow Buy Entry`, `Allow Sell Entry`, `Allow Buy Exit`, `Allow Sell Exit`) seguem a semântica exata da versão MT5.

3. **Gerenciamento de dinheiro**
   - `Money Management` determina quanto capital deve ser alocado por negociação. Valores negativos são interpretados como um tamanho de lote fixo.
   - `Margin Mode` seleciona a interpretação do valor de gerenciamento de dinheiro:
     - `FreeMargin` / `Balance`: invista uma parcela do patrimônio da conta dividido pelo preço.
     - `LossFreeMargin` / `LossBalance`: arriscar uma parcela do capital em relação à distância do stop-loss.
     - `Lot`: trate o valor como um volume fixo.
   - Os volumes são alinhados com `VolumeStep`, `MinVolume` e `MaxVolume` da segurança selecionada.

4. **Gerenciamento de riscos**
   - Quando uma nova negociação é colocada, a estratégia registra o preço de entrada e impõe as distâncias originais de stop-loss e take-profit (expressas em pontos, ou seja, múltiplos de `PriceStep`).
   - Atingir o stop-loss ou o take-profit fecha imediatamente a posição e limpa o preço de entrada salvo.
   - Um acelerador impede a reabertura de uma posição na mesma direção antes da abertura da próxima vela, reproduzindo a proteção de MQL "nível de tempo".

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `Money Management` | Fração de capital utilizada para dimensionamento (valores negativos passam a volume fixo). |
| `Margin Mode` | Modo de conversão para gerenciamento de dinheiro em volume. |
| `Stop Loss` | Distância de parada protetora em faixas de preço. |
| `Take Profit` | Meta de lucro em faixas de preço. |
| `Deviation` | Reservado para compatibilidade com a entrada MT5 (não utilizado diretamente). |
| `Allow Buy Entry` / `Allow Sell Entry` | Alternar entradas longas/curtas. |
| `Allow Buy Exit` / `Allow Sell Exit` | Alternar o fechamento de negociações curtas/longas. |
| `Candle Type` | Principal prazo utilizado para indicadores e avaliação de sinais. |
| `CCI Period` / `CCI Price` | CCI comprimento e fonte de preço aplicada. |
| `ATR Period` | ATR comprimento. |
| `Signal Bar` | Índice da barra finalizada utilizada para sinais (0 = atual, 1 = anterior, etc.). |

## Notas
- A estratégia depende apenas de velas finalizadas (`CandleStates.Finished`) para imitar a implementação baseada em ticks do MT5.
- Todos os valores dos indicadores e variáveis de estado são redefinidos quando a estratégia é reiniciada, garantindo a execução da otimização determinística.
- O parâmetro `Deviation` é fornecido para total compatibilidade, mesmo que as ordens de mercado StockSharp não usem um parâmetro de deslizamento explícito.
