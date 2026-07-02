# Estratégia do Trader de Notícias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento do script **NewsTrader.mq4** original, armando ambos os lados do mercado pouco antes de um lançamento macroeconômico programado. Dez minutos antes do carimbo de data/hora da notícia configurado, o bot envia um par de ordens de stop breakout e anexa imediatamente saídas de proteção quando um lado é acionado.

## Lógica principal

- Usa uma assinatura de vela de 1 minuto (configurável) apenas como fonte de tempo.
- Calcula o momento de ativação como `news time - LeadMinutes` e espera até a primeira vela concluída cujo tempo de abertura esteja nesse ponto ou além dele.
- Coloca um stop de venda abaixo do preço atual e um stop de compra acima dele, compensado por `BiasPips` convertido por meio de `Security.PriceStep` (espelha a lógica `bias * Point` em MQL4).
- Assim que uma ordem pendente for preenchida, a ordem pendente oposta será cancelada; ordens dedicadas de stop-loss e take-profit são colocadas usando as distâncias de pip configuradas.
- Os preenchimentos de stop-loss ou take-profit cancelam a ordem de proteção restante e nivelam a estratégia.
- Chama `StartProtection()` no início para que a estratégia coopere com proteções StockSharp de nível superior.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Contratos enviados com cada pedido pendente. | `1` |
| `StopLossPips` | Distância de stop-loss em pips (0 desativa a ordem de stop). | `10` |
| `TakeProfitPips` | Distância de lucro em pips (0 desativa a ordem alvo). | `10` |
| `BiasPips` | Distância do preço de referência às ordens de breakout stop. | `20` |
| `LeadMinutes` | Minutos antes do carimbo de data/hora da notícia, quando as ordens de fuga são armadas. | `10` |
| `NewsYear`, `NewsMonth`, `NewsDay`, `NewsHour`, `NewsMinute` | Componentes do horário programado das notícias (relógio da plataforma). | `2010`, `3`, `8`, `1`, `30` |
| `CandleType` | Tipo de dados Candle usado para rastrear a progressão do tempo. | `1 Minute` |

## Notas de implementação

- A estratégia define `Volume` como `TradeVolume` durante `OnStarted`, garantindo que métodos auxiliares como `BuyStop` e `SellStop` usem o tamanho esperado.
- `Security.PriceStep` deve ser definido; caso contrário, a lógica lança uma exceção porque as distâncias baseadas em pip não podem ser traduzidas em preços.
- Os preços de fechamento das velas são usados como um proxy para o lance/venda mais recente ao calcular os níveis de stop – correspondendo à lógica MQL4 original que dependia da cotação mais recente no momento do acionamento.
- Os pedidos pendentes são feitos apenas uma vez; o algoritmo não se rearma após a passagem do evento de notícias configurado.
- As ordens de proteção são ignoradas quando a respectiva distância do pip é zero, o que mantém o comportamento configurável para intervenção manual.

## Arquivos

- `CS/NewsTraderStrategy.cs` — Implementação da estratégia em C#.

A versão Python foi omitida intencionalmente conforme solicitado.
