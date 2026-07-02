# Estratégia de escada de limite de envelope
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Envelope Limit Ladder Strategy** é uma porta C# do MetaTrader consultor especialista `E_2_12_5min.mq4` (ID 7671). Ele reconstrói a escada original de pedidos com limite em torno de um envelope EMA em velas de 5 minutos, enquanto mantém o modelo de gerenciamento multi-alvo e de rastreamento do robô legado.

## Conceito

1. **Filtro de envelope** – um envelope de média móvel (padrão EMA 144 com um desvio de 0,05%) calculado no período de tempo `EnvelopeCandleType` configurável fornece a linha média e as bandas superior/inferior.
2. **Vela de sinal** – os sinais de negociação são avaliados na assinatura `CandleType` (padrão 5 minutos). Quando a vela anterior fecha entre a linha média e a banda mais próxima, os braços estratégicos limitam as ordens na linha média.
3. **Escada de pedidos** – até três limites de compra e três limites de venda são colocados simultaneamente:
   - Preço de entrada: valor da linha média alinhado.
   - Stop-loss: banda oposta do envelope.
   - Take-profit: banda ± compensações definidas pelo usuário (8, 13 e 21 pontos por padrão).
4. **Janela de negociação** – ordens pendentes são criadas somente quando `TradingStartHour < Hour < TradingEndHour`. Todos os limites restantes serão cancelados quando o horário de funcionamento atingir `TradingEndHour`.
5. **Gerenciamento de posição** – cada ordem com limite preenchida imediatamente coloca sua própria ordem stop e take-profit. Um modo de rastreamento opcional move o stop para a média móvel (ou o mantém na banda oposta) quando o preço ultrapassa o envelope.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | 5 minutos | Tipo vela para detecção de sinal. |
| `EnvelopeCandleType` | 5 minutos | Tipo de vela usado para calcular o envelope. Use um período de tempo maior para imitar a entrada MT4 `EnvTimeFrame`. |
| `EnvelopePeriod` | 144 | Comprimento médio móvel do envelope. |
| `MaMethod` | EMA | Método de média móvel (`SMA`, `EMA`, `SMMA`, `LWMA`). |
| `EnvelopeDeviation` | 0,05 | Largura do envelope em porcentagem (0,05 = 0,05%). |
| `TradingStartHour` | 0 | Primeira hora em que podem aparecer ordens pendentes (verificação exclusiva, corresponde ao comportamento do MT4). |
| `TradingEndHour` | 17 | Hora em que todas as ordens pendentes são removidas (limite superior exclusivo). |
| `FirstTakeProfitPoints` | 8 | Deslocamento em pontos adicionados além do envelope para o primeiro degrau da escada. |
| `SecondTakeProfitPoints` | 13 | Deslocamento em pontos para o segundo degrau. |
| `ThirdTakeProfitPoints` | 21 | Deslocamento em pontos para o terceiro degrau. |
| `UseOppositeEnvelopeTrailing` | `true` | Mantém o stop na banda oposta (`true`) ou segue até a média móvel (`false`). Espelha a sinalização MT4 `MaElineTSL`. |
| `OrderVolume` | 0,1 | Volume por ordem pendente (substitui o dimensionamento de lote adaptativo do MT4). |

## Notas de comportamento

- A estratégia mantém um par stop/take separado para cada ordem com limite preenchida. As saídas não interferem nos demais degraus da escada.
- O trailing só é ativado após um rompimento além do envelope e apenas estreita o stop na direção lucrativa.
- Quando `EnvelopeCandleType` difere de `CandleType`, os valores de envelope mais recentes da assinatura secundária são reutilizados para velas de sinal, correspondendo de perto à pesquisa de envelope de período de tempo superior MT4.
- A rotina original de gerenciamento de dinheiro MT4 (`LotsOptimized`) é substituída pelo parâmetro explícito `OrderVolume` para manter a porta determinística dentro de StockSharp.

## Dicas de uso

- Combine o período do envelope com as entradas MT4 para reproduzir o comportamento original (por exemplo, mantenha `EnvelopeCandleType` em 5 minutos ou mude para 1 hora/4 horas conforme necessário).
- Defina `UseOppositeEnvelopeTrailing` como `false` se desejar que o trailing stop salte para a média móvel em vez da banda oposta quando o preço sair do envelope.
- Otimizar as compensações de take-profit e o desvio de envelope em conjunto; as distâncias da escada dependem da volatilidade capturada pelo envelope.
