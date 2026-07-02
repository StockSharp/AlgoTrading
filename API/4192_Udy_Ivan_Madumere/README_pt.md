# Estratégia de Udy Ivan Madumere
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O consultor especialista Udy Ivan Madumere abre uma posição única no mercado uma vez por dia quando uma vela horária específica aparece. A porta StockSharp mantém esse comportamento intacto observando a série de velas configuradas, comparando preços históricos de abertura e reagindo imediatamente após o fechamento da barra alvo. Todas as decisões de execução, gerenciamento de posição e manipulação de volume são reproduzidas para que a estratégia se comporte como o MetaTrader 4 original dentro do ambiente StockSharp.

Características principais:

- Avalia uma vela finalizada por dia em `TradeHour` e nunca envia mais de uma posição simultânea.
- Mede a diferença entre os preços de abertura `Open[FirstLookback]` e `Open[SecondLookback]` para decidir se deve operar vendido ou comprado.
- Espelha a escada de equilíbrio MetaTrader para ajustar o tamanho do lote base automaticamente quando `UseAutoVolume` está ativado.
- Aplica distâncias assimétricas de stop-loss e take-profit (separadas para longas e curtas) e um trailing stop que afeta apenas as posições curtas.
- Força o fechamento de todas as negociações após um número configurável de horas, mesmo que os níveis de proteção não tenham sido atingidos.

## Fluxo de trabalho de negociação
1. Assine o tipo de vela selecionado (`CandleType`) e aguarde as barras totalmente finalizadas para evitar sinais prematuros.
2. Acompanhe o histórico de preços de abertura para que as diferenças `Open[FirstLookback] - Open[SecondLookback]` (configuração curta) e `Open[SecondLookback] - Open[FirstLookback]` (configuração longa) possam ser avaliadas exatamente como em MetaTrader.
3. Quando a vela mais recente abre em `TradeHour`:
   - Se a diferença de baixa for maior que `ShortDeltaPoints * PriceStep`, envie uma ordem de venda a mercado.
   - Caso contrário, se a diferença de alta exceder `LongDeltaPoints * PriceStep`, envie uma ordem de compra a mercado.
4. Só é permitido um pedido por dia. O sinalizador `canTrade` é redefinido após a hora configurada ter passado para permitir outra tentativa na próxima sessão.
5. Após a entrada do pedido a estratégia recalcula o lote base:
   - `UseAutoVolume = true` ativa a escada herdada que aumenta o tamanho do lote quando o saldo da conta ultrapassa limites predefinidos.
   - Se o saldo atual estiver abaixo do instantâneo da negociação anterior, o resultado será multiplicado por `BigLotMultiplier`, correspondendo ao comportamento de recuperação de “grande lote” do EA.
6. Enquanto a posição está aberta, a seguinte lógica de saída é executada em cada vela concluída:
   - O take-profit e o stop-loss rígidos são avaliados em relação ao preço de entrada registrado.
   - As negociações curtas também seguem o stop quando o melhor preço melhora em pelo menos `TrailingStopPoints`.
   - A posição é fechada à força quando estiver ativa há `MaxHoldingHours`.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H1` | Série de velas processada pela estratégia. |
| `TradeHour` | `int` | `18` | Hora do dia (0-23) em que o sinal diário é avaliado. |
| `FirstLookback` | `int` | `6` | Número de velas concluídas referenciadas como `Open[FirstLookback]`. |
| `SecondLookback` | `int` | `2` | Número de velas concluídas referenciadas como `Open[SecondLookback]`. |
| `LongDeltaPoints` | `decimal` | `6` | Diferença mínima de preço de abertura de alta (em MetaTrader pontos) necessária para entrar em uma posição comprada. |
| `ShortDeltaPoints` | `decimal` | `21` | Diferença mínima de preço de abertura de baixa (em MetaTrader pontos) necessária para entrar em uma venda. |
| `TakeProfitLongPoints` | `decimal` | `39` | Distância de take-profit, expressa em pontos, para posições longas. |
| `StopLossLongPoints` | `decimal` | `147` | Distância stop-loss, em pontos, para posições longas. |
| `TakeProfitShortPoints` | `decimal` | `200` | Distância de take-profit, em pontos, para posições vendidas. |
| `StopLossShortPoints` | `decimal` | `267` | Distância stop-loss, em pontos, para posições curtas. |
| `TrailingStopPoints` | `decimal` | `30` | Distância de trailing-stop (pontos) aplicada apenas a posições curtas. |
| `BaseVolume` | `decimal` | `0.01` | Tamanho inicial do lote antes dos ajustes de gestão de dinheiro. |
| `UseAutoVolume` | `bool` | `true` | Ative a escada de equilíbrio MetaTrader que substitui `BaseVolume`. |
| `BigLotMultiplier` | `decimal` | `1` | Multiplicador extra aplicado quando o saldo caiu desde a negociação anterior. |
| `MaxHoldingHours` | `int` | `504` | Tempo máximo de retenção em horas. Zero desativa o temporizador. |

## Notas de implementação
- Os limites de preço são convertidos de MetaTrader “pontos” em distâncias de preço reais usando o `PriceStep` do instrumento.
- O buffer de preço aberto é reduzido para `max(FirstLookback, SecondLookback) + 1` entradas, evitando alocações desnecessárias e mantendo o histórico necessário.
- O trailing stop para negociações curtas armazena o melhor mínimo alcançado e atualiza o nível de proteção somente quando o novo candidato estiver mais próximo do preço atual.
- Os instantâneos do saldo da conta dependem de `Portfolio.CurrentValue` (retrocedendo para `BeginValue`) para que os ambientes de demonstração, ao vivo e de backtest se comportem de forma consistente.
- Cada comentário dentro do código é escrito em inglês conforme solicitado, facilitando a auditoria ou extensão da lógica.

## Dicas de uso
- Combine `CandleType` com o período usado pelo histórico EA (o modelo original espera velas de uma hora).
- Ao executar símbolos que usam microlotes, ajuste `BaseVolume` e os valores da escada do lote automático de acordo com as especificações do contrato do local.
- Combine a estratégia com gráficos StockSharp por meio dos auxiliares integrados (`DrawCandles`, `DrawOwnTrades`) para verificar se os pedidos aparecem apenas uma vez por dia na hora configurada.
