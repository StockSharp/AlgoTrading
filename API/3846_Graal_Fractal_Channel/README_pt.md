# Estratégia de Canal Graal Fractal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Canal Fractal Graal** é uma versão StockSharp do MetaTrader 4 consultor especialista "Graal-003". O algoritmo observa padrões fractais de cinco velas e confirma rompimentos usando canais de preços adaptativos. Quando um fractal válido de alta ou baixa aparece, a estratégia avalia vários filtros (túnel fractal, envelope de preço de fechamento e supressão opcional de mercado estável) antes de entrar na direção de rompimento. Uma sobreposição %R Williams opcional replica a lógica de saída manual do robô original, enquanto ordens de parada de hedge podem ser preparadas para emular a proteção contra tendência do EA.

## Fluxo de dados e indicadores
* Assina o `CandleType` configurado (velas horárias por padrão).
* Cria uma fila contínua das últimas `ChannelPeriod` velas para calcular um canal de preço de fechamento semelhante a Donchian usado para filtros planos e verificações de orientação.
* Detecta máximos e mínimos fractais de cinco barras diretamente do fluxo da vela.
* Alimenta o indicador `WilliamsPercentRange` integrado para monitorar sinais de saída opcionais.

## Fluxo de trabalho de negociação
1. **Detecção fractal** – a estratégia rastreia cinco velas concluídas consecutivas. Quando a máxima/mínima da barra intermediária é o extremo em comparação com seus dois antecessores e dois seguidores, ela registra um fractal superior ou inferior e marca um sinal curto ou longo pendente.
2. **Envelhecimento do sinal** – cada nova vela aumenta a idade fractal. Se `SignalAgeLimit` barras passarem sem execução, o sinal pendente expira.
3. **Avaliação de canal** – o canal de fechamento contínuo fornece três filtros:
   - *Túnel fractal*: quando `UseFractalChannel` está ativado, o preço de fechamento deve permanecer dentro de uma porcentagem da distância entre a última máxima e mínima fractal (`DepthPercent`).
   - *Orientação alta/baixa*: com `UseHighLowChannel`, o fechamento deve penetrar apenas uma parte limitada do envelope (`OrientationPercent`).
   - *Bloqueio plano*: se `AllowFlatTrading` estiver desativado, as negociações serão suspensas enquanto a largura do canal permanecer abaixo de `FlatThresholdPips`.
4. **Execução da ordem** – uma vez aprovados os filtros, a estratégia normaliza o `OrderVolume` desejado em relação às restrições do instrumento e envia uma ordem de mercado na direção fractal.
5. **Paradas de hedge** – quando `UseCounterOrders` está ativo, o algoritmo coloca a ordem de stop oposta no preço fractal mais/menos `OffsetPips`, espelhando o teste de contratendência de EA.
6. **Williams sai** – se `UseWilliamsExit` estiver ativado, o valor mais recente de Williams %R fecha posições longas quando subir acima de `-WilliamsThreshold` e posições curtas quando cair abaixo de `-100 + WilliamsThreshold`.

As distâncias Stop Loss e Take Profit são opcionais. Sempre que `StopLossPips` ou `TakeProfitPips` for positivo, a estratégia converte a distância do pip em uma compensação de preço absoluto usando o tamanho do tick do instrumento (com o ajuste de 3/5 dígitos do EA) e delega o gerenciamento de ordens de proteção para `StartProtection`.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | `0.1` | Tamanho base da ordem de mercado antes da normalização em relação aos limites do instrumento. |
| `StopLossPips` | `500` | Distância de parada protetora em pips. Convertido em preço e aplicado via `StartProtection`. |
| `TakeProfitPips` | `500` | Tire a distância do lucro em pips. Convertido em preço e aplicado via `StartProtection`. |
| `OffsetPips` | `5` | Distância extra usada ao preparar ordens de stop contra-tendência. |
| `ChannelPeriod` | `14` | Número de velas recentes armazenadas para o canal de preço de fechamento. |
| `UseFractalChannel` | `false` | Requer que o preço permaneça dentro do corredor fractal interno antes das entradas. |
| `DepthPercent` | `25` | Porcentagem do intervalo fractal que define o corredor interno. |
| `UseHighLowChannel` | `false` | Ativa o filtro de orientação de canal fechado estilo Donchian. |
| `OrientationPercent` | `20` | Penetração permitida no canal próximo quando `UseHighLowChannel` é verdadeiro. |
| `AllowFlatTrading` | `true` | Permite a negociação mesmo quando o mercado está estável de acordo com a largura do canal próximo. |
| `FlatThresholdPips` | `20` | Largura mínima do canal (em pips) necessária quando a negociação plana está desativada. |
| `UseWilliamsExit` | `false` | Ativa regras de saída baseadas em Williams %R. |
| `WilliamsPeriod` | `14` | Período de lookback para o indicador Williams %R. |
| `WilliamsThreshold` | `30` | Limite de sensibilidade (pontos percentuais) para Williams%R saídas. |
| `UseCounterOrders` | `false` | Coloca a ordem stop oposta após uma entrada no mercado. |
| `SinglePosition` | `false` | Bloqueia entradas adicionais na mesma direção enquanto uma posição está aberta. |
| `SignalAgeLimit` | `3` | Número máximo de novas barras durante as quais um sinal fractal permanece válido. |
| `CandleType` | `H1` | Série de dados de velas usada para análise (o padrão é o período de uma hora). |

## Notas de uso
* A estratégia espera instrumentos com `PriceStep`, `MinVolume` e `VolumeStep` válidos para que a normalização de volume e a conversão de pip funcionem corretamente.
* As ordens contra-tendência são canceladas automaticamente quando a posição é fechada, quando a estratégia é interrompida ou quando o recurso é desativado.
* Williams As saídas %R atuam como uma rede de segurança e podem fechar posições mesmo se o sinal fractal original ainda estiver ativo.
* O algoritmo redefine todos os estados armazenados em cache (buffers fractais, histórico de Williams, pedidos preparados) sempre que `OnReseted` é acionado.

## Diferenças da versão MetaTrader
* A implementação StockSharp usa assinaturas `SubscribeCandles().Bind(...)` de alto nível em vez de loops de indicadores manuais.
* As paradas de proteção dependem de `StartProtection`, portanto, nenhuma contabilidade direta de ordens de parada/limite é necessária.
* O volume é normalizado em relação aos limites da exchange antes do envio dos pedidos, correspondendo às convenções StockSharp.
