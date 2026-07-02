# Estratégia diária de quadro de entrada STP (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de quadro de entrada de STP diário** replica o comportamento do consultor especialista MetaTrader original "Quadro de entrada de STP diário" usando o StockSharp API de alto nível. O sistema prepara ordens de breakout stop no início de cada novo dia de negociação. Os preços de entrada são derivados dos máximos e mínimos do dia anterior, com filtros adicionais para garantir que o mercado esteja posicionado próximo desses extremos antes de armar as ordens. A lógica é adaptada para instrumentos do tipo Forex, onde os “pontos base” correspondem a um décimo de pip para cotações de cinco dígitos.

## Fluxo de trabalho principal
1. **Acompanhamento de intervalo diário** – a estratégia assina velas diárias para lembrar as máximas e mínimas da sessão anterior.
2. **Monitoramento em tempo real** – Os dados do Nível 1 fornecem os preços atuais de compra, venda e última negociação para gerenciamento intradiário.
3. **Armação de ordem** – no início de um novo dia, se o último preço estiver a pelo menos `ThresholdPoints` de distância da máxima/mínima de ontem e a abertura do dia atual estiver no lado correto desse extremo, uma ordem stop será enviada:
   - Stop de compra em `High + SpreadPoints / 2` (convertido em unidades de preço).
   - Parada de venda em `Low - SpreadPoints / 2`.
4. **Validação de risco** – novas ordens são bloqueadas sempre que a redução do patrimônio excede `MaximumDrawdownPercent` ou os filtros de tempo não permitem a negociação (fins de semana, filtro de hora ou filtro de dia).
5. **Gerenciamento de posição** – uma vez que uma negociação está ativa, a estratégia impõe:
   - Distâncias estáticas de stop-loss e take-profit.
   - Saída opcional baseada em tempo após `CloseAfterSeconds`.
   - Trailing stop opcional emulando o parâmetro original "SL Slope".
6. **Higiene de final de dia** – os pedidos pendentes são cancelados após `NoNewOrdersHour` (ou o horário limite de sexta-feira) e sempre que o dia do calendário mudar.

## Regras de negociação
- **Entradas longas**
  - Permitido quando `SideFilter` é `0` (ambos) ou `1` (apenas longo).
  - Máxima do dia anterior menos preço atual ≥ `ThresholdPoints`.
  - O preço de abertura de hoje está abaixo da máxima de ontem.
  - O preço de entrada calculado deve respeitar a distância mínima do pedido atual.
- **Entradas curtas**
  - Permitido quando `SideFilter` é `0` (ambos) ou `-1` (apenas abreviado).
  - Preço atual menos a mínima do dia anterior ≥ `ThresholdPoints`.
  - O preço de abertura de hoje está acima do mínimo de ontem.
  - O preço de entrada calculado deverá respeitar a distância mínima da oferta atual.
- **Gerenciamento de dinheiro**
  - O dimensionamento dinâmico do volume usa uma porcentagem do lucro acumulado (`PercentOfProfit`).
  - O tamanho resultante é limitado por `MinVolume` e `MaxVolume` e alinhado com o `VolumeStep` do instrumento.
  - A negociação é pausada automaticamente quando o rebaixamento medido ultrapassa `MaximumDrawdownPercent`.
- **Lógica de proteção**
  - Os níveis de stop-loss e take-profit são expressos em pontos base e convertidos em compensações de preço usando o tamanho do pip do instrumento.
  - O stop móvel está ativo somente quando `TrailingSlope < 1`. Aproxima o limiar de protecção do preço à medida que o lucro não realizado aumenta.
  - As saídas vitalícias fecham qualquer posição aberta depois de decorrido o número configurado de segundos.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período de tempo utilizado para buscar as velas de referência (diariamente por padrão). |
| `StopLossPoints` | Distância de stop-loss em pontos base. |
| `TakeProfitPoints` | Distância de lucro em pontos base. |
| `TrailingSlope` | Parcela do lucro retida durante o trailing; ≥ 1 desativa o recurso. |
| `SideFilter` | -1 apenas curto, 0 em ambas as direções, 1 apenas longo. |
| `ThresholdPoints` | Diferença mínima entre o preço atual e o extremo anterior necessário para armar um stop. |
| `SpreadPoints` | Deslocamento adicional (metade usado acima/abaixo do extremo) para compensar o spread. |
| `SlippagePoints` | Buffer de segurança adicionado à verificação da distância mínima de parada. |
| `NoNewOrdersHour` | Hora (horário da plataforma) para cancelar pedidos pendentes em dias normais. |
| `NoNewOrdersHourFriday` | Horário de cancelamento específico para sexta-feira. |
| `EarliestOrderHour` | Primeira hora do dia em que novos pedidos podem ser criados. |
| `DayFilter` | 6 para todos os dias ou 0-5 para negociar apenas de domingo a sexta-feira. |
| `CloseAfterSeconds` | Saída opcional baseada em tempo (0 desabilita). |
| `PercentOfProfit` | Fração do lucro acumulado usada para dimensionar o tamanho da posição. |
| `MinVolume` / `MaxVolume` | Limites rígidos para o volume calculado. |
| `MaximumDrawdownPercent` | Limite de rebaixamento que bloqueia novos pedidos. |

## Notas de conversão
- A conversão de pip reflete a implementação MetaTrader: se o título expõe 3 ou 5 casas decimais, o ponto base se torna `PriceStep * 10`.
- A janela de cancelamento da ordem stop reproduz a limpeza noturna do especialista, incluindo o corte separado de sexta-feira.
- A lógica final segue a fórmula de inclinação original (`newStop = Bid - StopLoss - Slope * (Bid - Entry)` para longos).
- As notificações de patrimônio da versão MQL são substituídas por mensagens de registro de estratégia.
- A implementação StockSharp mantém as ordens pendentes ativas mesmo quando uma posição está aberta, correspondendo ao comportamento da origem.

## Dicas de uso
- Atribua um instrumento Forex com valores `PriceStep`, `StepPrice` e `VolumeStep` configurados corretamente para garantir um dimensionamento preciso.
- Combine a estratégia com StockSharp controles de risco (limites de portfólio, proteções em nível de conector) durante a execução ao vivo.
- Otimize `ThresholdPoints`, `TrailingSlope` e `PercentOfProfit` usando Designer ou Runner para adaptar a sensibilidade de rompimento a símbolos específicos.
