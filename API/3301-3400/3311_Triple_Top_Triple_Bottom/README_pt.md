# Estratégia Triple Top Triple Bottom
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Triple Top Triple Bottom** é um port do Expert Advisor MetaTrader com o mesmo nome. O sistema original combina várias camadas de confirmação (direção da tendência, força do momentum e filtro MACD) antes de entrar no mercado. Esta implementação StockSharp mantém as mesmas ideias centrais enquanto expõe os limiares importantes como parâmetros de estratégia.

## Lógica central

1. **Filtro de tendência:** duas médias móveis linearmente ponderadas (LWMA) calculadas sobre o preço típico (H+L+C)/3 definem a direção da negociação. A LWMA rápida deve estar acima da lenta para permitir compras e abaixo da lenta para permitir vendas.
2. **Confirmação de momentum:** o indicador momentum integrado, com comprimento de retrospectiva configurável, deve se desviar do nível neutro 100 pelo menos pelo limiar definido pelo usuário dentro dos três últimos candles concluídos. O EA exigia o mesmo comportamento analisando valores anteriores de momentum, e espelhamos essa validação para evitar entradas em mercados laterais.
3. **Filtro MACD:** um filtro clássico de linha de sinal MACD 12/26/9 evita operar contra uma tendência forte. A estratégia só compra quando a linha MACD está acima da linha de sinal e vende quando está abaixo.
4. **Gestão de risco:** ordens a mercado são protegidas com alvos de stop-loss e take-profit medidos em unidades absolutas de preço. Os parâmetros são opcionais; defini-los como zero desabilita a respectiva ordem. O código também fecha a posição se o limiar de risco oposto for atingido durante o processamento de candles.

## Parâmetros

- **Entry Candle:** `DataType` que define o timeframe dos candles de trabalho.
- **Fast LWMA / Slow LWMA:** comprimentos dos filtros de tendência rápido e lento.
- **Momentum Period / Momentum Threshold:** retrospectiva do indicador momentum e desvio mínimo de 100 que confirma uma ideia de operação.
- **Stop Loss / Take Profit:** distâncias protetoras em unidades absolutas de preço; elas também são enviadas como ordens protetoras nativas via `SetStopLoss` e `SetTakeProfit` para que o controle de risco seja aplicado mesmo se a sessão da estratégia parar.

## Diferenças em relação à versão MQL

- Todos os extras de gestão monetária (multiplicadores de lote, proteção de patrimônio, trailing por candle, break-even e checagens manuais de linha de tendência) foram omitidos porque a API de alto nível do StockSharp já oferece utilitários de dimensionamento de posição e porque os objetos gráficos usados no EA original são específicos do MetaTrader.
- Limiares de risco são expressos em unidades absolutas de preço em vez de pips. Isso mantém a implementação neutra em relação à corretora; usuários podem converter facilmente sua distância preferida em pips multiplicando o tamanho de pip da corretora pelo número desejado de pips.
- A saída gráfica usa áreas StockSharp para candles de preço, médias móveis, momentum e indicadores MACD.

## Notas de uso

1. Anexe a estratégia a um instrumento e configure o tipo de candle desejado antes de iniciar.
2. Ajuste o limiar de momentum e as distâncias de stop de acordo com a volatilidade do instrumento.
3. A estratégia negocia uma única posição líquida. Quando um sinal oposto aparece, a exposição atual é fechada primeiro, evitando operações sobrepostas.

O código é totalmente comentado em inglês e segue as diretrizes da API StockSharp de alto nível fornecidas no repositório.
