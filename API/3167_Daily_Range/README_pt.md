# Estratégia de Daily Range
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão StockSharp do assessor especialista do MetaTrader 5 `MQL/23334/Daily range.mq5`. O EA original rastreia os preços mais altos e mais baixos alcançados nos últimos dias, desloca esses níveis por uma porcentagem configurável do intervalo diário e negocia rompimentos. O port em C# preserva o comportamento adotando a API de estratégia de alto nível do StockSharp.

## Lógica da estratégia
### Cálculo do intervalo
* A estratégia armazena estatísticas agregadas para cada dia de negociação (máxima, mínima, último fechamento).
* Uma janela deslizante de `SlidingWindowDays` dias recentes (incluindo o atual) é mantida.
* `RangeMode` seleciona como o intervalo de referência é calculado:
  * **HighestLowest** – a distância entre a máxima mais alta e a mínima mais baixa na janela.
  * **CloseToClose** – a variação absoluta média entre preços de fechamento diários consecutivos dentro da janela.
* Assim que o `StartTime` configurado é alcançado em um novo dia, a estratégia reconstrói os níveis de rompimento superior e inferior:
  * `Upper = Highest + Range × OffsetCoefficient`
  * `Lower = Lowest − Range × OffsetCoefficient`
* Até que `StartTime` seja alcançado, os níveis de rompimento do dia anterior permanecem ativos (espelhando a implementação MQL).

### Regras de entrada
* Uma entrada comprada é acionada quando o preço de fechamento da vela processada é maior ou igual ao nível superior atual e menos de `MaxPositionsPerDay` entradas compradas foram abertas no mesmo dia.
* Uma entrada vendida é acionada quando o preço de fechamento cai para ou abaixo do nível inferior e o limite diário de entradas vendidas não foi atingido.
* Ao mudar de uma posição existente para o lado oposto, a estratégia primeiro compensa o volume pendente e então adiciona o novo `Volume` por cima, correspondendo ao comportamento de netting do EA original.
* Os sinais são avaliados apenas em velas concluídas entregues pela subscrição `CandleType` configurada e apenas quando `IsFormedAndOnlineAndAllowTrading()` indica que a negociação é permitida.

### Regras de saída
* As distâncias de stop-loss e take-profit são derivadas do intervalo atual: `Range × StopLossCoefficient` e `Range × TakeProfitCoefficient` respectivamente.
* Para posições compradas, uma ordem de fechamento é enviada se a mínima da vela tocar o nível de stop ou a máxima exceder o nível de take-profit.
* Para posições vendidas, uma ordem de fechamento é enviada se a máxima da vela atingir o nível de stop ou a mínima cruzar o nível de take-profit.
* Definir qualquer coeficiente como zero desativa a proteção correspondente.

### Controles de risco e limites
* Contadores diários separados são mantidos para entradas compradas e vendidas. Eles são redefinidos sempre que um novo dia de negociação começa.
* A propriedade `Volume` da `Strategy` base controla o tamanho das entradas adicionais.
* Nenhuma ordem pendente é registrada; as saídas são executadas com ordens de mercado na próxima iteração da estratégia após a condição ser detectada.

## Parâmetros
| Nome | Descrição | Valor padrão |
| --- | --- | --- |
| `RangeMode` | Determina como o intervalo diário é calculado (`HighestLowest` ou `CloseToClose`). | `HighestLowest` |
| `SlidingWindowDays` | Número de dias calendário incluídos na janela deslizante para o cálculo do intervalo. | `3` |
| `StopLossCoefficient` | Multiplicador aplicado ao intervalo atual para definir a distância do stop-loss. | `0.03` |
| `TakeProfitCoefficient` | Multiplicador aplicado ao intervalo atual para definir a distância do take-profit. | `0.05` |
| `OffsetCoefficient` | Deslocamento adicional aplicado aos níveis de rompimento acima da máxima e abaixo da mínima. | `0.01` |
| `MaxPositionsPerDay` | Número máximo de entradas permitidas por direção durante um único dia de negociação. | `3` |
| `StartTime` | Hora do dia quando um intervalo fresco é calculado para a sessão atual. | `10:05` |
| `CandleType` | Subscrição de velas usada para cálculo do intervalo e avaliação de sinais. | `Período de 15 minutos` |

## Notas de implementação
* A estratégia depende exclusivamente da infraestrutura de alto nível `Strategy` do StockSharp (`SubscribeCandles`, `WhenNew` e ordens de mercado) e não manipula livros de ordens brutos.
* As estatísticas de intervalo são armazenadas sem usar pesquisas de valores de indicadores; todos os cálculos ocorrem dentro da estratégia, de acordo com as diretrizes do repositório.
* As ordens protetoras são simuladas monitorando os extremos das velas em vez de registrar ordens stop/limite separadas, o que mantém a implementação portável entre diferentes adaptadores.
* O suporte ao Python é intencionalmente omitido conforme solicitado. Apenas a versão em C# é fornecida nesta pasta.
* Para negociação ao vivo, certifique-se de que haja velas históricas suficientes disponíveis para que o primeiro cálculo do intervalo tenha dados suficientes para trabalhar.
