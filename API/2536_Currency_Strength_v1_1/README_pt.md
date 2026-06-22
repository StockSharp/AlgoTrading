# Estratégia de Força de Moedas v1.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Força de Moedas v1.1 replica o consultor especialista do MetaTrader *Currency Strength v1.1*. Ela mede a força relativa das oito principais moedas (USD, EUR, JPY, CAD, AUD, NZD, GBP, CHF) usando mudanças percentuais diárias de 26 pares FX líquidos. Sempre que a força de duas moedas diverge além de um limiar configurável, a estratégia abre uma posição no par de moedas correspondente na direção da moeda mais forte.

## Mercado e dados
- **Universo de instrumentos:** 26 pares FX principais e cruzados (USDJPY, USDCAD, AUDUSD, USDCHF, GBPUSD, EURUSD, NZDUSD, EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURNZD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, CHFJPY, GBPCHF, GBPAUD, GBPCAD, GBPJPY, CADJPY, NZDJPY, GBPNZD, CADCHF).
- **Frequência de dados:** Velas diárias (D1). Apenas velas concluídas são processadas para manter cálculos consistentes.
- **Campos necessários:** Preços de abertura, máxima, mínima e fechamento de cada vela.

## Cálculo da força das moedas
A mudança percentual diária para cada par é calculada como:

```
(change) = (Close − Open) / Open × 100
```

Essas mudanças específicas de par são então combinadas em índices de força de moedas:

- **Força EUR** = média de EURJPY, EURCAD, EURGBP, EURCHF, EURAUD, EURUSD, EURNZD
- **Força USD** = média de USDJPY, USDCAD, –AUDUSD, USDCHF, –GBPUSD, –EURUSD, –NZDUSD
- **Força JPY** = média negativa de USDJPY, EURJPY, AUDJPY, CHFJPY, GBPJPY, CADJPY, NZDJPY
- **Força CAD** = média de CADCHF, CADJPY, –GBPCAD, –AUDCAD, –EURCAD, –USDCAD
- **Força AUD** = média de AUDUSD, AUDNZD, AUDCAD, AUDCHF, AUDJPY, –EURAUD, –GBPAUD
- **Força NZD** = média de NZDUSD, NZDJPY, –EURNZD, –AUDNZD, –GBPNZD
- **Força GBP** = média de GBPUSD, –EURGBP, GBPCHF, GBPAUD, GBPCAD, GBPJPY, GBPNZD
- **Força CHF** = média de CHFJPY, –USDCHF, –EURCHF, –AUDCHF, –GBPCHF, –CADCHF

Cada média usa o mesmo número de componentes que no consultor especialista original para preservar o esquema de ponderação.

## Lógica de trading
1. Após todos os 26 pares reportarem uma nova vela diária finalizada, as forças são recalculadas.
2. Para cada par a estratégia compara as duas forças de moedas relevantes. Se a diferença absoluta exceder o parâmetro `DifferenceThreshold`, um sinal de operação é gerado.
3. A direção do sinal segue a moeda mais forte:
   - Se força da moeda base > força da moeda cotada → comprar o par.
   - Se força da moeda base < força da moeda cotada → vender o par.
4. As operações só são permitidas quando a vela diária do par concorda com o sinal (fechamento acima da abertura para compras, fechamento abaixo para vendas), refletindo o filtro de tendência do EA original.
5. As posições líquidas existentes são respeitadas. Se um sinal de reversão aparece enquanto uma posição contrária está aberta, a estratégia fecha a posição atual e inverte para a nova direção com uma única ordem a mercado.
6. Quando `TradeOncePerDay` está habilitado, cada par pode entrar comprado no máximo uma vez por dia de trading e entrar vendido no máximo uma vez por dia de trading.

## Gestão de risco e saídas
- O indicador opcional `UseSlTp` habilita a lógica de stop-loss e take-profit executada na vela diária de cada par. As distâncias são definidas em pips (`StopLossPips`, `TakeProfitPips`).
- A lógica protetora avalia a máxima/mínima diária da vela mais recente. Se esses extremos atingirem os alvos respectivos, a posição é fechada ao preço de mercado na próxima etapa de avaliação.
- Sem SL/TP, as posições permanecem abertas até que um sinal oposto force uma reversão ou a estratégia seja parada manualmente, refletindo o comportamento do EA fonte.

## Parâmetros da estratégia
| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Período para velas (padrão: diário). |
| `DifferenceThreshold` | Diferença mínima de força (em pontos percentuais) necessária para acionar uma operação. |
| `TradeOncePerDay` | Se `true`, limita cada par a uma entrada comprada e uma vendida por dia. |
| `UseSlTp` | Habilita a avaliação diária dos níveis de stop-loss e take-profit. |
| `TakeProfitPips` | Distância do take-profit medida em pips. |
| `StopLossPips` | Distância do stop-loss medida em pips. |
| Parâmetros de pares | Entradas individuais `Security` para os 26 pares FX. Cada um deve ser atribuído antes de iniciar a estratégia. |
| `Volume` | Propriedade da classe base que define o tamanho da operação (padrão 0.01 lotes). |

## Notas de implementação
- A estratégia se inscreve em cada par separadamente usando a API de subscrição de velas de alto nível (`SubscribeCandles`).
- O tratamento de velas ignora estritamente velas incompletas, satisfazendo as diretrizes de conversão do StockSharp.
- Os cálculos de força e a geração de sinais só funcionam quando todos os pares reportam a mesma data de trading, garantindo cestas de moedas sincronizadas.
- Dicionários internos rastreiam as últimas datas de operação por direção e armazenam informações de entrada para saídas protetoras.

## Dicas de uso
1. Atribuir todos os 26 instrumentos antes de iniciar a estratégia; entradas ausentes lançam uma exceção para evitar cálculos parciais.
2. Garantir que o provedor de dados forneça velas diárias para cada par configurado para que as forças das moedas permaneçam sincronizadas.
3. Ajustar `DifferenceThreshold` para controlar a frequência de sinais. Limiares menores levam a operações mais frequentes, mas também mais reversões.
4. Calibrar os stops baseados em pip para a precisão de cotação do seu corretor; o padrão assume preços de pip fracionário.
