# Estratégia TradeEATemplateNews
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia TradeEATemplateNews é uma conversão em C# do consultor especializado MetaTrader 4 "Trade EA Template for News". O sistema original pausava o trading em torno de eventos econômicos programados baixados de sites externos. Este port do StockSharp mantém as ideias principais enquanto as adapta à API de alto nível:

- Usa velas completadas do período configurado (H1 por padrão).
- Opera somente quando a conta está flat, exatamente como o modelo MQL que exigia zero ordens abertas.
- Aplica um bloqueio manual de notícias econômicas que impede entradas antes e depois dos eventos dependendo de sua importância.
- Cria automaticamente brackets protetores de stop-loss e take-profit a 100 pontos do preço de execução (convertidos pelo passo do instrumento).

## Lógica de trading
1. Cada vela completada aciona um recálculo do calendário de notícias. A estratégia armazena o preço de abertura da vela anterior para que a próxima barra possa comparar seu fechamento com a abertura anterior.
2. Se o tempo atual cair dentro de qualquer janela de bloqueio configurada, a estratégia cancela ordens pendentes e não abre novas operações.
3. Quando nenhuma posição está aberta e o trading é permitido:
   - Uma posição comprada é aberta se a última vela fechar acima do preço de abertura da vela anterior.
   - Uma posição vendida é aberta se a última vela fechar abaixo do preço de abertura da vela anterior.
4. Os níveis de stop-loss e take-profit são expressos em pontos (`TakeProfitPoints` e `StopLossPoints`) e convertidos em deslocamentos de preço absolutos usando o valor `Step` do instrumento.

## Calendário de notícias manual
O especialista original baixava dados de investing.com ou DailyFX. Para portabilidade, a versão StockSharp espera um calendário curado manualmente fornecido através do parâmetro `NewsEventsDefinition`. O formato aceita uma lista de entradas separadas por ponto-e-vírgula ou quebras de linha. Cada entrada deve conter pelo menos três campos separados por vírgulas:

```
AAAA-MM-DD HH:MM,MOEDAS,IMPORTÂNCIA[,TÍTULO]
```

- `AAAA-MM-DD HH:MM` — início do evento em UTC. O parâmetro opcional `TimeZoneOffsetHours` desloca todos os tempos processados pelo valor solicitado (por exemplo, defina `3` para UTC+3).
- `MOEDAS` — códigos de moeda ou identificadores de instrumentos como `USD`, `EUR`, `EUR/USD`. Múltiplos códigos podem ser separados com `/`, `,`, `;`, `|` ou espaços.
- `IMPORTÂNCIA` — palavra-chave de importância. Valores reconhecidos: `Low`, `Medium`, `Mid`, `Midle`, `Moderate`, `High`, `NFP`, strings contendo `Nonfarm` ou `Non-farm`.
- `TÍTULO` — descrição de texto livre opcional que será impressa nas mensagens de registro.

Exemplo:

```
2024-03-01 13:30,USD,High,Nonfarm Payrolls;2024-03-01 15:00,USD,Low,Factory Orders
```

### Janelas de bloqueio
- `UseLowNews`, `UseMediumNews`, `UseHighNews` e `UseNfpNews` alternam quais eventos são considerados.
- `LowMinutesBefore/After`, `MediumMinutesBefore/After`, `HighMinutesBefore/After` e `NfpMinutesBefore/After` determinam quantos minutos em torno do evento o trading deve ser desabilitado.
- `OnlySymbolNews` restringe o bloqueio a entradas cujos códigos de moeda correspondam ao instrumento atual (por exemplo, `EURUSD` resulta no par `{EUR, USD}`). Desative-o para pausar o trading em cada evento.
- A estratégia mantém apenas o evento de maior importância ativo em qualquer momento. Mensagens de registro informativas anunciam a razão do estado atual e a próxima publicação programada.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Tipo de dados de velas para assinar. Padrão de 1 hora. | `1h` |
| `UseLowNews` | Habilitar eventos de baixa importância. | `true` |
| `LowMinutesBefore` / `LowMinutesAfter` | Minutos antes/depois de notícias de baixo impacto para bloquear entradas. | `15 / 15` |
| `UseMediumNews` | Habilitar eventos de importância média. | `true` |
| `MediumMinutesBefore` / `MediumMinutesAfter` | Minutos antes/depois de notícias de impacto médio. | `30 / 30` |
| `UseHighNews` | Habilitar eventos de alta importância. | `true` |
| `HighMinutesBefore` / `HighMinutesAfter` | Minutos antes/depois de notícias de alto impacto. | `60 / 60` |
| `UseNfpNews` | Habilitar o indicador de Non-farm Payrolls. | `true` |
| `NfpMinutesBefore` / `NfpMinutesAfter` | Minutos antes/depois de eventos NFP. | `180 / 180` |
| `OnlySymbolNews` | Filtrar o calendário pelos códigos de moeda do instrumento atual. | `true` |
| `NewsEventsDefinition` | String de descrição do calendário econômico manual. | vazio |
| `TimeZoneOffsetHours` | Deslocamento aplicado a cada evento processado (UTC por padrão). | `0` |
| `TakeProfitPoints` | Distância em pontos para a ordem protetora de take-profit. | `100` |
| `StopLossPoints` | Distância em pontos para a ordem protetora de stop-loss. | `100` |

`Volume` é herdado de `Strategy` e deve ser configurado de acordo com o tamanho de posição desejado.

## Diferenças da versão MQL
- Sem download HTTP automático — o usuário fornece a lista de notícias manualmente, o que evita dependências externas e mantém a conversão determinista.
- Os rótulos de gráfico e linhas verticais são substituídos por mensagens de registro que descrevem o evento ativo ou próximo.
- O especialista MQL abria ordens com tamanho de lote fixo `0.01`; no StockSharp o tamanho de posição vem da propriedade `Volume`.
- Toda a lógica é implementada com a API de assinatura de velas de alto nível preservando o comportamento consciente de notícias do modelo.

## Notas de implantação
1. Preencha `NewsEventsDefinition` antes de iniciar a estratégia ou atualize-o, pare e reinicie para recarregar o calendário.
2. Ajuste `TimeZoneOffsetHours` e os parâmetros de minutos antes/depois para corresponder à sua sessão de trading.
3. Defina `Volume`, portfólio e instrumento na interface ou no código, então inicie a estratégia.
4. Observe o log da estratégia para mensagens como "Trading paused due to high news" ou "Next scheduled news" para confirmar a lógica de bloqueio.

A tradução para Python é intencionalmente omitida conforme solicitado.
