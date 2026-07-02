# EMA Estratégia Cruzada 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta StockSharp do MetaTrader 4 consultor especialista **"EMA_CROSS_2"** do repositório MQL. O EA original monitora duas médias móveis exponenciais (EMAs) e coloca ordens de mercado sempre que as médias trocam de ordem. A conversão mantém a natureza contrária do script - ela compra quando o longo EMA se move acima do curto EMA e vende quando o curto EMA sobe acima do longo EMA - enquanto envolve a lógica na infraestrutura estratégica de alto nível StockSharp.

A estratégia opera em velas completas fornecidas pelo tipo de dados de vela configurável. Os sinais são avaliados no fechamento da vela para evitar disparos repetidos dentro da mesma barra. O gerenciamento de risco imita o comportamento MetaTrader usando distâncias de take-profit, stop-loss e trailing stop expressas em pontos de corretor (etapas de preço).

## Lógica de negociação
1. **Cálculo do indicador**
   - Calcule os EMAs de curto e longo período em cada vela concluída.
   - Ignore a primeira atualização do indicador, correspondendo ao sinalizador `first_time` original que ignorou a primeira avaliação.
   - Depois, detecte uma mudança de direção quando a ordem relativa entre o EMA longo e curto mudar.
2. **Interpretação de sinal**
   - Quando o EMA longo se move acima do EMA curto, o EA original abriu uma negociação de compra. A porta StockSharp mantém esta regra contrária, embora se comporte de forma oposta a um sistema de crossover clássico.
   - Quando a posição curta EMA fecha acima da posição longa EMA, a estratégia abre uma negociação de venda.
   - Novas posições só são permitidas quando nenhuma exposição estiver aberta no momento, replicando a condição `OrdersTotal() < 1`.
3. **Execução de pedido**
   - As negociações são enviadas como ordens de mercado com volume fixo configurável.
   - Na entrada, a estratégia registra os preços de stop-loss e take-profit usando a distância do pip fornecida através dos parâmetros.
4. **Gerenciamento de riscos**
   - Em cada vela finalizada, a estratégia verifica se a ação do preço tocou os níveis armazenados de stop-loss ou take-profit. A violação de qualquer um dos níveis fecha toda a posição com uma ordem de mercado.
   - Um trailing stop (também definido nos pontos do corretor) é aplicado quando o preço se move favoravelmente mais do que a distância final. Para posições longas, o batente de proteção é deslocado para cima; para posições curtas, o preço segue para baixo.
   - Quando a posição se torna plana, os níveis de proteção armazenados são apagados.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas usada para cálculos de indicadores e detecção de sinal. | Período de 15 minutos |
| `OrderVolume` | Volume de cada ordem de mercado em lotes/contratos. | 2 |
| `TakeProfitPoints` | Distância até o nível de lucro expresso em pontos de corretor (etapas de preço). Um valor de `0` desativa o take-profit. | 20 |
| `StopLossPoints` | Distância até o nível de stop loss expressa em pontos da corretora. Um valor de `0` desativa o stop loss. | 30 |
| `TrailingStopPoints` | Distância usada ao rastrear a posição aberta. `0` desativa o trailing stop. | 50 |
| `ShortEmaPeriod` | Comprimento do EMA rápida. | 5 |
| `LongEmaPeriod` | Comprimento da lentidão EMA. | 60 |

## Notas de implementação
- A estratégia usa `SubscribeCandles().Bind(shortEma, longEma, ProcessCandle)` para conectar dados de velas com indicadores EMA, seguindo o padrão preferido de alto nível API.
- Os valores dos indicadores são recebidos como decimais prontos para uso no retorno de chamada de ligação, portanto, nenhuma indexação manual do buffer é necessária.
- As distâncias de proteção são convertidas de MetaTrader pontos em StockSharp preços multiplicando pelo instrumento `PriceStep`. Se o instrumento usar preços de pip fracionários (3 ou 5 casas decimais), o auxiliar calculará o tamanho do pip de acordo.
- O comportamento de stop-loss, take-profit e trailing são implementados internamente com saídas de mercado porque StockSharp não expõe o mesmo fluxo de trabalho `OrderModify` que MetaTrader 4. O gerenciamento comercial resultante reflete a lógica original: os níveis são verificados em cada vela e as saídas ocorrem imediatamente após a violação.
- A primeira avaliação cruzada é intencionalmente ignorada para reproduzir a proteção `first_time` que evitou negociações prematuras no script MQL.

## Diferenças da versão MetaTrader
- Gestão de dinheiro: o EA original sempre negociou o parâmetro `Lots`. A conversão expõe o mesmo conceito por meio de `OrderVolume` e também o atribui à propriedade de estratégia `Volume` para que designers e otimizadores possam reutilizá-lo.
- Colocação de pedidos: MetaTrader aplicou stop-loss e take-profit diretamente em `OrderSend`. Em StockSharp esses níveis são rastreados pela estratégia e fechados com ordens de mercado quando violados.
- Precisão do trailing stop: as paradas EA movidas usando dados de tick (`Bid`/`Ask`). A porta atualiza a lógica final no fechamento da vela, que é a granularidade mais fina disponível neste projeto de amostra. As regras de distância e ativação permanecem idênticas.
- O tratamento de erros e o registro em log foram simplificados; O registro StockSharp fornece informações detalhadas por meio do registro de estratégia padrão.

## Dicas de uso
- Alinhe `CandleType` com o período usado durante os backtests do EA original para manter um comportamento comparável do indicador.
- Ao negociar símbolos cotados com pips fracionários, certifique-se de que as distâncias dos pontos configuradas reflitam o número desejado de pips (por exemplo, em EURUSD `10` pontos são iguais a 1 pip).
- Defina `OrderVolume` com o tamanho do contrato esperado pelo seu local de execução. A estratégia não realiza escalonamento automático de volume.
- Use os alternadores de otimização integrados em cada parâmetro para explorar combinações de EMA períodos e distâncias de risco, assim como você otimizaria entradas em MetaTrader.

## Arquivos
- `CS/EmaCross2Strategy.cs` – StockSharp implementação da lógica de negociação.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – tradução chinesa.
- `README_ru.md` – Tradução russa.
