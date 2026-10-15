# Estratégia de Breakout da Faixa Hans123Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
## Visão geral
A **Estratégia Hans123Trader** recria o consultor especialista MetaTrader "Hans123Trader v1" usando o StockSharp API de alto nível. O sistema arma ordens de parada duas vezes por dia com base na quebra da faixa de negociação de 5 minutos mais recente. É adaptado para símbolos do estilo Forex, onde as etapas de preço correspondem a pips fracionários. As ordens pendentes são atualizadas a cada dia de negociação e qualquer posição aberta é fechada à força quando o calendário termina.

## Fluxo de trabalho principal
1. **Rastreamento de faixa** – uma janela contínua de 80 barras de velas de 5 minutos é mantida por meio dos indicadores `Highest` e `Lowest`. A máxima e a mínima mais recentes definem os níveis de rompimento.
2. **Agendamento de sessões** – duas janelas de negociação independentes são controladas por `EndSession1` e `EndSession2`. Quando o relógio atinge a hora configurada (minuto `00`), a estratégia calcula novas ordens de stop pendentes.
3. **Colocação de ordem** – um stop de compra é enviado `5` pontos acima da máxima detectada e um stop de venda `5` pontos abaixo da mínima detectada. Os pedidos são removidos no momento em que um novo dia começa para imitar a expiração de MetaTrader às 23h59.
4. **Gerenciamento de posição** – após a entrada a estratégia aplica o stop-loss inicial solicitado, o take-profit opcional e o trailing stop. Os níveis de proteção são expressos em pontos e convertidos em preço usando o `PriceStep` do instrumento.
5. **Higiene diária** – se uma posição permanecer aberta no início de um novo dia de negociação, ela será fechada no mercado. Todos os pedidos pendentes do dia anterior são cancelados antes que novos sejam preparados.

## Regras de negociação
- **Sinais de entrada**
  - Duas tentativas de breakout por dia: uma às `EndSession1`, outra às `EndSession2` (o horário é o horário do corretor/servidor).
  - Preço de parada de compra = `HighestHigh + 5 points`. Preço de parada de venda = `LowestLow − 5 points`.
  - Ambos os pedidos usam o parâmetro `Volume` atual (padrão `1`).
  - Os pedidos serão ignorados se o volume não for positivo.
- **Lógica de saída**
  - Stop-loss inicial = preço de entrada ± `InitialStopLoss` pontos (abaixo para posições compradas, acima para posições vendidas).
  - Take-profit = preço de entrada ± `TakeProfit` pontos (acima para posições compradas, abaixo para posições vendidas).
  - O trailing stop estreita o nível de proteção sempre que o fechamento avança em direção ao lucro em pelo menos `TrailingStop` pontos.
  - Qualquer posição que sobreviva até o dia seguinte é fechada imediatamente no mercado.
- **Manutenção de pedidos**
  - As ordens stop pendentes são canceladas no início de cada dia corrido.
  - Depois que uma ordem stop é acionada (ou cancelada/falha), as referências internas são apagadas automaticamente.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `BeginSession1` / `BeginSession2` | Preservado para compatibilidade com a interface do usuário (dicas de horário de início). A implementação atual depende dos gatilhos de fim de hora. |
| `EndSession1` / `EndSession2` | Horas (0–23) em que novas ordens de parada são armadas; os minutos devem ser exatamente zero. |
| `TrailingStop` | Distância final em pontos. `0` desativa o rastreamento. |
| `TakeProfit` | Distância de lucro em pontos. `0` desativa o lucro. |
| `InitialStopLoss` | Distância inicial de stop-loss em pontos. `0` sai da negociação sem um stop de proteção, a menos que o trailing seja ativado. |
| `CandleType` | Série de velas usada para o intervalo de 80 barras (padrão `TimeSpan.FromMinutes(5)`). |
| `Volume` | Volume base da estratégia herdado de `Strategy`. |

## Notas de conversão
- As MetaTrader funções auxiliares `OrderSendExtended` e o bloqueio de variável global não são necessários; StockSharp gerencia a simultaneidade internamente.
- Os números mágicos são substituídos por referências de ordem explícitas (`_session*` campos). Os eventos do ciclo de vida do pedido limpam essas referências quando o pedido é concluído.
- Os pedidos pendentes que expiram às 23h59 são emulados cancelando-os quando um novo dia começa.
- A lógica de trailing stop usa preços de fechamento de velas como substituto para as cotações de compra/venda de MetaTrader.
- Todas as distâncias baseadas em pontos são multiplicadas por `Security.PriceStep`. Quando `PriceStep` não está definido, os valores dos pontos brutos são tratados como distâncias absolutas de preços.

## Dicas de uso
- Atribua instrumentos com `PriceStep`, `StepPrice` e `VolumeStep` configurados corretamente para que a conversão ponto-preço e o arredondamento de volume sejam precisos.
- Verifique se os dados históricos de 5 minutos estão disponíveis; os níveis de rompimento dependem das 80 velas mais recentes.
- Ajuste `EndSession1`/`EndSession2` para corresponder às sessões de mercado desejadas (por exemplo, intervalos pré-Londres e pré-Nova York).
- Use Designer ou Runner para otimizar `InitialStopLoss`, `TakeProfit` e `TrailingStop` para o instrumento escolhido antes da implantação ativa.
- Combine a estratégia com StockSharp controles de risco se várias estratégias compartilharem o mesmo portfólio.
