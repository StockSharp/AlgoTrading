# Exemplo de estratégia de detecção de calendário econômico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Calendário Econômico de Detecção de Amostras** replica o comportamento do consultor especialista MetaTrader original `SampleDetectEconomicCalendar.mq5`. A estratégia observa uma lista fornecida manualmente de eventos do calendário econômico e, quando um evento de alto impacto se aproxima para a moeda configurada, coloca um par simétrico de ordens stop em torno dos preços atuais de compra/venda. Paradas protetoras, níveis de lucro opcionais e uma saída final replicam a lógica de gerenciamento de dinheiro do código-fonte.

Ao contrário da versão MQL, a porta StockSharp não tem acesso ao serviço de calendário MetaTrader. Em vez disso, os eventos são fornecidos pelo usuário por meio do parâmetro `CalendarDefinition`.

## Como funciona
1. A estratégia assina os dados do Nível 1 para rastrear os preços de compra/venda.
2. As linhas do calendário definidas em `CalendarDefinition` são analisadas na inicialização.
3. Para cada evento de alta importância correspondente a `BaseCurrency`, a estratégia:
   - Aguarda até `LeadMinutes` antes do lançamento.
   - Calcula o volume do pedido (fixo ou baseado em risco).
   - Coloca ordens stop de compra/venda a `BuyDistancePoints` e `SellDistancePoints` dos preços atuais.
4. Após a liberação, os pedidos pendentes serão cancelados após `PostMinutes` decorridos ou após o tempo limite total de `ExpiryMinutes`.
5. Quando um lado é acionado, a ordem oposta é cancelada. A posição aberta é gerenciada com stop loss, take-profit opcional e distâncias de trailing stop expressas em pontos.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeNews` | Permite colocar pedidos pendentes em torno de eventos noticiosos programados. |
| `OrderVolume` | Volume de pedido fixo usado quando o gerenciamento de dinheiro está desativado. |
| `StopLossPoints` | Distância de stop-loss em pontos do instrumento. Defina como 0 para desativar. |
| `TakeProfitPoints` | Distância de lucro em pontos. Defina como 0 para desativar. |
| `TrailingStopPoints` | Distância de parada final em pontos. Defina como 0 para desativar o rastreamento. |
| `ExpiryMinutes` | Vida útil máxima dos pedidos pendentes após a liberação. |
| `UseMoneyManagement` | Se ativado, o volume é calculado a partir do risco de saldo. |
| `RiskPercent` | Percentagem do capital da carteira arriscado por negociação (utilizada apenas quando a gestão de dinheiro está ativa). |
| `BuyDistancePoints` | Deslocamento acima do pedido para a entrada de parada de compra. |
| `SellDistancePoints` | Compensação abaixo do lance para a entrada de stop de venda. |
| `LeadMinutes` | Minutos antes do lançamento, quando os pedidos pendentes são enviados. |
| `PostMinutes` | Minutos após o lançamento, antes que os pedidos não atendidos sejam cancelados. |
| `BaseCurrency` | Código da moeda que deve aparecer na entrada do calendário (padrão `USD`). |
| `CalendarDefinition` | String multilinha contendo eventos do calendário. |

## Formato de definição de calendário
Forneça um evento por linha no seguinte formato:

```
aaaa-MM-dd HH:mm;CUR;Alto;Título do evento
```

* `yyyy-MM-dd HH:mm` — carimbo de data/hora em UTC. Os segundos são opcionais. Vários formatos de data (`yyyy/MM/dd`, `dd.MM.yyyy`) também são suportados.
* `CUR` — código da moeda (por exemplo, `USD`). Somente eventos correspondentes a `BaseCurrency` são negociados.
* `High` — palavra-chave de importância (`High`, `Medium`, `Low` ou `Nfp`). Apenas `High` aciona negociações.
* `Event title` — texto livre para registro.

Exemplo:

```
12/06/2024 18:00;USD;Alta;Declaração FOMC
2024-07-05 12:30;USD;Nfp;Folhas de pagamento não agrícolas
```

## Gestão de risco
* Quando `UseMoneyManagement` está **desligado**, os pedidos são feitos usando o parâmetro `OrderVolume`.
* Quando `UseMoneyManagement` está **ativado**, a estratégia arrisca `RiskPercent` do valor do portfólio usando o `StopLossPoints` configurado. Os limites de volume de troca (passo mínimo/máximo) são respeitados.
* A lógica de trailing reflete o EA original: as saídas de stop-loss e take-profit são aplicadas e, uma vez que o preço se move favoravelmente em `TrailingStopPoints`, o trailing stop protege a negociação.

## Diferenças do consultor especialista MQL
* Os eventos do calendário econômico devem ser fornecidos manualmente em `CalendarDefinition`.
* Apenas um par instrumento/moeda é processado por instância de estratégia.
* A expiração da ordem pendente é tratada internamente com temporizadores `PostMinutes`/`ExpiryMinutes` porque as ordens stop StockSharp não expõem sinalizadores MetaTrader no estilo `ORDER_TIME_SPECIFIED`.

## Notas de uso
1. Configure as linhas `CalendarDefinition` antes de iniciar a estratégia.
2. Ative `TradeNews` e defina os parâmetros de risco desejados.
3. Certifique-se de que os dados do Nível 1 estejam disponíveis para que as atualizações de compra/venda cheguem antes da janela de notícias.
4. Revise os registros para confirmar se os pedidos foram feitos e cancelados conforme esperado em cada evento.
