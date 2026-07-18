# Regularidades da Estratégia de Taxas de Câmbio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia StockSharp é uma conversão C# fiel do consultor especialista MetaTrader 4 **Strategy_of_Regularities_of_Exchange_Rates.mq4**. O sistema foi projetado como um breakout straddle diário: ele coloca o mercado entre ordens de stop quando chega uma hora específica e mantém essas ordens ativas até a hora de fechamento noturno. Qualquer posição preenchida é supervisionada por um stop-loss do lado da corretora e um watchdog intradiário de take-profit para que as negociações não se prolonguem além da sessão de negociação definida.

Ao contrário dos sistemas baseados em indicadores, a lógica centra-se apenas no tempo e na distância. Quando o cronograma diz que o mercado deve estar pronto, a estratégia mede um deslocamento fixo em pontos de corretagem (pips) da oferta atual e da oferta de venda e coloca um par de ordens de stop simétricas. O código adapta automaticamente o cálculo do ponto para símbolos com aspas de 3 ou 5 dígitos, correspondendo ao comportamento da versão original MQL.

## Lógica de negociação

1. **Horário de abertura** – assim que uma vela concluída reporta `OpeningHour`, a estratégia cancela quaisquer ordens pendentes restantes e envia um *stop de compra* acima do pedido atual e um *stop de venda* abaixo do lance atual. A distância é `EntryOffsetPoints * point`, onde o valor `point` é derivado do instrumento `PriceStep` e ajustado para cotações fracionárias.
2. **Ordens de proteção** – imediatamente após a inicialização a estratégia habilita `StartProtection` com o `StopLossPoints` configurado. Qualquer negociação executada recebe, portanto, um stop loss do lado da corretora idêntico ao EA original.
3. **Supervisão de lucro** – em cada vela concluída, o algoritmo verifica se o lucro atual excede `TakeProfitPoints * point`. Nesse caso, fecha a posição no mercado. Isso reflete o loop `OrderClose` original que saiu quando o lucro atingiu o limite.
4. **Hora de fechamento** – quando o relógio chega a `ClosingHour`, a estratégia fecha à força todas as posições abertas e cancela as ordens de stop, garantindo que o livro esteja estável para a próxima sessão.
5. **Reinicialização diária** – um novo lote de ordens pendentes é enviado apenas uma vez por dia de negociação, evitando duplicatas e ainda respeitando a intenção original de uma única configuração por sessão.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OpeningHour` | `9` | Hora (0–23) em que o par de ordens stop é colocado. |
| `ClosingHour` | `2` | Hora (0–23) em que as ordens pendentes são removidas e quaisquer negociações abertas são estabilizadas. |
| `EntryOffsetPoints` | `20` | Distância em pontos da corretora desde o bid/ask atual até as ordens stop. |
| `TakeProfitPoints` | `20` | Meta de lucro em pontos de corretagem que desencadeia uma saída do mercado. Defina como `0` para desativar o lucro manual. |
| `StopLossPoints` | `500` | Distância em pontos intermediários para a parada de proteção anexada via `StartProtection`. |
| `OrderVolume` | `0.1` | Volume de cada ordem de parada. |
| `CandleType` | `30 minute time frame` | Série de velas usada para avaliar o cronograma. Qualquer período ≤ 1 hora mantém o comportamento consistente com o script MQL. |

## Notas de conversão

- O consultor especialista original trabalhou em eventos de tick e referenciou `Hour()` diretamente. Em StockSharp a estratégia escuta as velas finalizadas e usa seu horário de abertura, o que preserva a lógica de uma vez por hora enquanto permanece dentro das diretrizes do repositório sobre os estados das velas.
- As ordens pendentes são normalizadas com `Security.ShrinkPrice` para que os preços gerados sempre correspondam ao tamanho do tick do instrumento.
- O gerenciamento de parada delega para `StartProtection`, recriando o stop-loss gerado pela plataforma que MetaTrader anexou durante `OrderSend`.
- O código rastreia a última data de negociação para evitar o reenvio da mesma faixa várias vezes no mesmo dia, algo que poderia acontecer em períodos inferiores a uma hora no EA original.
- Extensos comentários embutidos esclarecem cada etapa do fluxo de trabalho para manutenção ou experimentação futura.
