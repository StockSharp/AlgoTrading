# Estratégia de CrossoverMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um porte StockSharp do consultor especialista MetaTrader 5 **CrossoverMA.mq5**. O robô original aguarda um candle cruzar uma média móvel e só abre uma posição quando a média está inclinada na mesma direção que o rompimento. A versão StockSharp mantém o mesmo comportamento aproveitando a API de alto nível para assinaturas de candles, gerenciamento de indicadores e renderização automática de gráficos.

## Lógica de trading

1. Subscrever a série de candles configurada e calcular uma média móvel simples (SMA) sobre o preço de fechamento do candle.
2. Quando um candle terminado é recebido, medir:
   - As distâncias de abertura e fechamento do candle da SMA.
   - A inclinação da SMA comparando o valor atual com o anterior.
3. Gerar sinais:
   - **Rompimento altista** – o candle abre abaixo da SMA, fecha acima dela, e a SMA está subindo. A estratégia fecha qualquer exposição curta e abre/estende uma posição longa.
   - **Rompimento baixista** – o candle abre acima da SMA, fecha abaixo dela, e a SMA está caindo. A estratégia fecha qualquer exposição longa e abre/estende uma posição curta.
4. Ignorar sinais duplicados que não mudem o lado da posição atual.

O porte mantém a regra do MetaTrader de que apenas candles terminados são processados e que um candle extra é necessário antes do primeiro trade (para medir a inclinação da SMA).

## Parâmetros

| Nome | Descrição | Padrão | Notas |
| ---- | ----------- | ------- | ----- |
| `Candle Type` | Período usado para construir candles. | Período de 1 minuto | Qualquer tipo de dado de candle suportado pelo StockSharp pode ser selecionado. |
| `MA Length` | Número de candles completados incluídos na SMA. | 12 | Corresponde ao período padrão do especialista MetaTrader. |
| `Trade Volume` | Volume de ordem de mercado para entradas. | 1 | A estratégia fecha a exposição oposta antes de abrir uma nova posição. |

Todos os parâmetros estão disponíveis para otimização no StockSharp Designer ou Runner.

## Notas de implementação

- A estratégia depende de `SubscribeCandles` e `Bind` para que os valores do indicador sejam transmitidos diretamente ao método de processamento sem gerenciamento manual do histórico.
- A SMA é armazenada em um campo privado para desenhá-la na área do gráfico quando uma está disponível.
- Os sinais são processados apenas quando `IsFormedAndOnlineAndAllowTrading()` retorna `true`, garantindo que a estratégia respeite o estado global de trading.
- As reversões de posição seguem o modelo do MetaTrader: fechar a exposição atual primeiro, depois abrir o novo lado com o volume de trade configurado.

## Arquivos

- `CS/CrossoverMaStrategy.cs` – implementação em C# da estratégia convertida.
- `README.md` – documentação em inglês.
- `README_zh.md` – documentação em chinês.
- `README_ru.md` – documentação em russo.

## Diferenças de portabilidade

- As classes de gerenciamento de dinheiro, trailing stop e outros frameworks do MetaTrader são omitidas porque o StockSharp gerencia o dimensionamento de posições e o risco externamente. O parâmetro `Trade Volume` substitui as configurações de lote fixo do especialista original.
- O MetaTrader usava séries de dados separadas para os preços de abertura e fechamento de candles. Os candles do StockSharp já incluem ambos os preços, então nenhum indicador adicional é necessário.
- A inicialização, validação e gerenciamento do ciclo de vida do indicador são tratados automaticamente pelo StockSharp, removendo o extenso código boilerplate da versão MQL.
