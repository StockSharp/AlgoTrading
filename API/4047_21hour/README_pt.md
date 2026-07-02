# Estratégia 21 horas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **21horas** reproduz o comportamento do MQL4 consultor especialista `21hour.mq4`. Ele opera em torno de uma janela de tempo diária: as ordens de breakout pendentes são criadas em uma hora de início configurável e toda a exposição é removida em uma hora de término configurável. A versão StockSharp mantém a mesma ideia de "duas ordens stop em torno do preço", ao mesmo tempo em que aproveita o API de alto nível para gerenciamento de pedidos, assinaturas de dados de mercado e tratamento protetor de take-profit.

## Lógica de negociação
- No início de cada dia de negociação, quando o horário do servidor corresponde a `StartHour:00`, a estratégia lê as últimas cotações de compra/venda e coloca uma ordem de compra e venda.
  - A distância do preço de venda atual até o gatilho de parada de compra é `StepPoints * PriceStep`.
  - A distância do preço de compra atual até o gatilho do stop de venda é o mesmo valor abaixo do mercado.
  - `TakeProfitPoints` é convertido em distância de preço por meio da etapa de preço do instrumento e passado para `StartProtection`, portanto, tanto as posições longas quanto as curtas recebem um take-profit protetor logo após a execução.
- Somente uma configuração pendente é permitida por dia. Se apenas uma das duas ordens stop permanecer ativa (por exemplo, após um lado ter sido preenchido), a estratégia cancela a ordem pendente sobrevivente para espelhar a lógica EA original.
- Quando o relógio chega a `StopHour:00`, a estratégia fecha qualquer posição aberta no mercado e cancela todas as ordens pendentes pendentes. Isto se aplica mesmo que não tenha ocorrido nenhuma ruptura.
- O fluxo de velas padrão são dados de um minuto. Ele é usado exclusivamente para acionar verificações horárias em velas concluídas, o que imita a proteção `prevtime` da versão MQL.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Volume` | Volume de pedidos em lotes para ambos os pedidos pendentes. | `0.1` |
| `StartHour` | Hora (0–23) em que o par de ordens pendentes é criado. | `10` |
| `StopHour` | Hora (0–23) em que a estratégia fecha posições e remove todas as ordens pendentes. | `22` |
| `StepPoints` | Distância em pontos do instrumento entre o preço atual de compra/venda e cada entrada de stop. Convertido em preço multiplicando por `PriceStep`. | `15` |
| `TakeProfitPoints` | Distância em pontos do preço de entrada até a meta de lucro gerenciada por `StartProtection`. Defina como `0` para desativar o alvo. | `200` |
| `CandleType` | Tipo de dados Candle usado para rastreamento de tempo. O padrão é o período de um minuto (`TimeSpan.FromMinutes(1).TimeFrame()`). | `1 minute` |

## Notas de implementação
- Usa `SubscribeCandles` para obter velas concluídas e avaliar a programação horária apenas uma vez por minuto.
- Assina cotações de nível 1 via `SubscribeLevel1()` para manter os valores de compra/venda mais recentes para posicionamento de stop preciso.
- Depende de `StartProtection` com uma unidade de lucro para emular o lucro da ordem pendente do EA original em vez de anexar pedidos manualmente.
- Mantém o controle das ordens stop de compra e venda ativas e chama `CancelOrder` se apenas um lado permanecer, garantindo que o sistema nunca seja executado com uma ordem pendente não emparelhada.
- Invoca auxiliares `BuyMarket` / `SellMarket` para posições planas na hora de parada, usando estritamente a estratégia de alto nível API.

## Notas de comportamento
- A estratégia espera que a conexão do corretor forneça informações sobre as etapas do preço. Se `PriceStep` estiver ausente, os preços não serão arredondados.
- Os pedidos pendentes são gerados apenas uma vez por dia corrido. Eles serão recriados no próximo dia de negociação na hora de início configurada, mesmo que o rompimento do dia anterior não tenha sido acionado.
- Quando `TakeProfitPoints` é zero, a estratégia ainda coloca ordens pendentes, mas nenhum lucro protetor é gerenciado.
