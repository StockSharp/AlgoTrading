# Estratégia de Alerta de Cruzamento de Linha de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do consultor especialista MetaTrader original que observava cruzamentos de preço de linhas horizontais e linhas de tendência desenhadas manualmente. Ela monitora continuamente candles finalizados, verifica se o corpo do candle cruzou algum nível registrado e gera alertas na primeira vez que um cruzamento ocorre. Nenhuma ordem automática é enviada por padrão; o módulo se concentra em rastrear níveis discricionários e informar o operador.

## Destaques da conversão
- Apenas linhas marcadas com o valor de *Cor de monitoramento* são consideradas, espelhando o EA original que filtrava objetos por cor.
- Uma vez que um cruzamento é detectado, a linha é marcada internamente para que candles subsequentes não disparem alertas duplicados. Isso espelha a recoloração do objeto para a cor `CrossedColor` no MetaTrader.
- Como o StockSharp não expõe objetos de gráfico do terminal, os níveis são definidos através de parâmetros de texto. Entradas horizontais são analisadas de blocos `Name|Color|Price`, enquanto linhas de tendência usam `Name|Color|StartTime|StartPrice|EndTime|EndPrice` e são avaliadas como linhas infinitas entre os dois pontos de âncora.
- As opções de alerta, notificação push e e-mail mapeiam para entradas de log informativas para que o fluxo de trabalho permaneça transparente mesmo sem canais de notificação específicos da plataforma.

## Parâmetros
| Parâmetro | Tipo | Descrição |
| --- | --- | --- |
| `MonitoringColor` | `string` | Rótulo de cor que as linhas devem corresponder para serem monitoradas. Não diferencia maiúsculas de minúsculas. |
| `CrossedColor` | `string` | Rótulo usado nas mensagens de alerta para indicar que a linha foi cruzada. |
| `HorizontalLevelsInput` | `string` | Lista de níveis horizontais separados por ponto e vírgula. Cada entrada é `Name|Color|Price`; se a cor for omitida, a cor de monitoramento é assumida. |
| `TrendlineDefinitions` | `string` | Lista de linhas de tendência separadas por ponto e vírgula. Cada entrada é `Name|Color|StartTime|StartPrice|EndTime|EndPrice`. Os horários devem estar no formato ISO 8601 e usar o fuso horário do calendário de negociação. |
| `EnableAlerts` | `bool` | Quando `true`, a estratégia escreve uma entrada de log informativa descrevendo o cruzamento. |
| `EnableNotifications` | `bool` | Adiciona uma segunda entrada de log que emula uma notificação push. |
| `EnableEmails` | `bool` | Adiciona uma terceira entrada de log que emula um alerta por e-mail. |
| `CandleType` | `DataType` | Série de candles usada para monitorar o mercado. |

## Formato de definição
1. Separar múltiplas entradas com ponto e vírgula (`;`).
2. Os níveis horizontais podem omitir o nome ou a cor:
   - `1.1050` → monitorado como `Horizontal 1` no preço `1.1050` usando a cor de monitoramento.
   - `Resistance|1.1180` → nome personalizado ainda usando a cor de monitoramento.
   - `Breakout|Blue|1.1225` → cor personalizada ainda deve corresponder a `MonitoringColor` para ser rastreada.
3. As linhas de tendência requerem dois pontos de âncora com carimbos de tempo ISO 8601 (`2024-03-15T10:00:00Z`). Valores de cor ausentes assumem a cor de monitoramento. As linhas são extrapoladas além das âncoras exatamente como as linhas de tendência do MetaTrader.

## Fluxo de execução
1. Durante `OnStarted`, as definições de texto são analisadas em estruturas fortemente tipadas e armazenadas na memória.
2. Candles finalizados da assinatura configurada acionam `ProcessCandle`.
3. O método verifica se o candle abriu em um lado de um nível e fechou no outro lado. Nesse caso, a linha é marcada como cruzada e uma mensagem é gerada.
4. As mensagens incluem a direção do cruzamento, o preço teórico da linha e o preço de fechamento para que traders discricionários possam reagir manualmente.

## Notificações
As estratégias do StockSharp emitem mensagens de log em vez de pop-ups de UI. Cada canal de notificação habilitado produz uma entrada de log separada, permitindo que a aplicação host as encaminhe para sistemas de alerta reais se necessário.

## Lista de verificação de uso
1. Selecionar o instrumento e o período, depois configurar `CandleType` adequadamente.
2. Preencher `HorizontalLevelsInput` e `TrendlineDefinitions` com as linhas desenhadas em seu espaço de trabalho MetaTrader (ou quaisquer valores personalizados).
3. Ajustar os booleanos de notificação para corresponder aos canais de alerta desejados.
4. Iniciar a estratégia. O subsistema de gráficos pode ser usado para plotar linhas manualmente se desejado; este módulo se concentra na detecção.

## Configuração de exemplo
```
MonitoringColor = "Yellow"
CrossedColor = "Green"
HorizontalLevelsInput = "DailyPivot|Yellow|1.1025;WeeklyHigh|Yellow|1.1100"
TrendlineDefinitions = "UpperChannel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080"
EnableAlerts = true
EnableNotifications = true
EnableEmails = false
CandleType = 15 minute candles
```
Esta configuração monitora dois níveis estáticos e uma linha de tendência ascendente. Uma mensagem como `Price crossed horizontal line 'DailyPivot' upward at 1.10250 ...` será escrita na primeira vez que um fechamento passar por cada nível.

## Gestão de risco e extensões
- A estratégia não modifica posições. Combine-a com lógica de execução separada se a negociação automática for necessária.
- Para redefinir alertas, parar e reiniciar a estratégia ou ajustar as strings de definição. A persistência do estado `HashSet` é intencionalmente evitada para permanecer próxima ao comportamento original do EA.
- Salvaguardas adicionais como filtros de sessão ou verificações de volatilidade podem ser sobrepostas estendendo o método `ProcessCandle`.
