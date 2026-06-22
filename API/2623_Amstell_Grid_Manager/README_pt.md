# Estratégia Amstell Gerenciador de Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port de alto nível do expert MetaTrader "exp_Amstell-SL" que executa um grid de médias bidirecional. A estratégia rastreia o preço de execução mais recente em cada lado e emite ordens de mercado adicionais quando o preço se afasta o suficiente, enquanto liquida o lote aberto assim que uma distância fixa de take-profit ou stop-loss é atingida. A implementação usa as assinaturas de candles e os helpers de ordens de alto nível do StockSharp, portanto pode ser conectada a qualquer ambiente que forneça dados de candles para um único instrumento.

A lógica traduzida está ligeiramente adaptada para o modelo de portfólio líquido do StockSharp: os grids comprado e vendido ainda são gerenciados separadamente, mas não são mantidos ao mesmo tempo. O grid comprado está ativo enquanto a posição líquida é não negativa, e o grid vendido assume o controle somente após toda a exposição comprada ter sido zerada.

## Como funciona

### Fluxo de dados e execução
- Assina o `CandleType` configurado (padrão: candles de período de 1 minuto) e processa apenas candles finalizados.
- Calcula offsets baseados em pips a partir do `PriceStep` do instrumento. Se o passo tem 3 ou 5 casas decimais, é multiplicado por 10 para imitar o ajuste de pip de 3/5 dígitos do MetaTrader.
- Todas as operações são colocadas através dos helpers `BuyMarket`/`SellMarket`; nenhuma ordem pendente é usada.

### Gestão do lado comprado
- Abre a primeira posição comprada (`OrderVolume`) assim que não há exposição comprada existente e a estratégia não está no processo de fechar vendidos.
- Rastreia o preço de execução comprado mais recente e o preço de entrada ponderado por volume para o lote comprado ativo.
- Coloca ordens compradas adicionais de tamanho `OrderVolume` sempre que o preço de fechamento caiu pelo menos `BuyDistancePips` (convertidos em unidades de preço) abaixo da última execução comprada.

### Gestão do lado vendido
- Assim que o lote comprado está completamente fechado e a posição líquida é não positiva, a estratégia permite entradas vendidas.
- Coloca a ordem vendida inicial quando não há exposição vendida; vendidos adicionais são abertos depois que o preço sobe `BuyDistancePips * SellDistanceMultiplier` acima da execução vendida anterior.
- Mantém o preço de execução vendido mais recente e o preço de entrada ponderado por volume para o lote vendido ativo.

### Regras de saída
- Para cada direção, calcula o lucro não realizado relativo à execução média.
- Fecha todo o lote comprado com uma venda a mercado quando o lucro atinge `TakeProfitPips` pips ou o drawdown atinge `StopLossPips` pips.
- Fecha todo o lote vendido com uma compra a mercado quando o lucro atinge `TakeProfitPips` pips ou o movimento adverso atinge `StopLossPips` pips.
- Após a liquidação, todos os preços e volumes em cache são redefinidos para que um novo grid possa começar no próximo candle.

### Diferenças em relação ao expert MQL original
- A versão StockSharp opera em fechamentos de candles em vez de ticks individuais.
- Os grids comprado e vendido são executados sequencialmente em vez de simultaneamente, correspondendo ao modo de netting padrão do StockSharp.
- Todas as distâncias de proteção são verificadas contra o preço de entrada médio em vez de cada ticket individualmente, o que reflete o comportamento da posição líquida agregada.

## Parâmetros

| Parâmetro | Padrão | Faixa de otimização | Descrição |
|-----------|---------|--------------------|-------------|
| `OrderVolume` | `0.01` | `0.01` – `0.10` (passo `0.01`) | Quantidade enviada com cada ordem de grid. Deve ser positiva. |
| `TakeProfitPips` | `30` | `10` – `150` (passo `10`) | Alvo de lucro para o lote ativo expresso em pips. |
| `StopLossPips` | `30` | `10` – `150` (passo `10`) | Movimento adverso máximo antes de abandonar o lote. |
| `BuyDistancePips` | `10` | `5` – `60` (passo `5`) | Queda mínima da última execução comprada para adicionar outra compra. Deve ser menor que TP e SL. |
| `SellDistanceMultiplier` | `10` | `2` – `15` (passo `1`) | Multiplicador aplicado à distância comprada ao espaçar entradas vendidas. |
| `CandleType` | Período de 1 minuto | — | Série de candles usada para geração de sinais. |

## Notas de implementação
- `BuyDistancePips` deve ser estritamente menor que `TakeProfitPips` e `StopLossPips`; a estratégia lança uma exceção na inicialização caso contrário, reproduzindo a validação do MetaTrader.
- O tamanho do pip é derivado do `PriceStep` do instrumento. Ajuste os parâmetros se o instrumento usar um tamanho de tick não padrão.
- Todo o estado interno é limpo em `OnReseted`, permitindo reiniciar a estratégia sem dados residuais do grid.
- Nenhuma personalização de cor ou registro manual de indicadores é usado, correspondendo às diretrizes de API de alto nível neste repositório.
