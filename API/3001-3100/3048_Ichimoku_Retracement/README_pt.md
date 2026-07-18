# Estratégia de Retração Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão StockSharp do expert advisor MetaTrader **"ICHMOKU RETRACEMENT"**. Mantém a ideia original de operar retrações de Ichimoku que ocorrem dentro de uma tendência de período superior enquanto são filtradas por leituras de momentum a longo prazo e MACD. A implementação StockSharp foca em clareza, reutilização de indicadores e controle de risco através da API de alto nível.

## Ideia de trading

1. **Filtro de tendência** – a estratégia busca um viés altista ou baixista usando um par de Médias Móveis Linealmente Ponderadas (LWMA). Um contexto altista requer que a LWMA rápida esteja acima da LWMA lenta, enquanto um contexto baixista requer a relação oposta.
2. **Retração Ichimoku** – após detectar uma tendência, o candle anterior deve tocar qualquer uma das linhas de Ichimoku (Tenkan-sen, Kijun-sen ou os dois spans à frente). O candle atual deve abrir de volta no lado da tendência da linha tocada, sinalizando uma retração de momentum.
3. **Confirmação de momentum** – a relação de momentum de fechamento para fechamento deve desviar do seu valor neutro (100) por pelo menos um limiar configurável. A relação é calculada no mesmo período usado para o indicador Ichimoku.
4. **Filtro macro** – um MACD mensal (12/26/9) confirma a direção dominante de longo prazo. Operações compradas requerem a linha principal do MACD acima da linha de sinal, as vendidas requerem o oposto.
5. **Gerenciamento de ordens** – a estratégia mantém no máximo uma posição líquida. Níveis de stop-loss e take-profit de proteção são colocados em pips e avaliados em cada candle terminado.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `Signal Candle Type` | Período usado para os cálculos de LWMA, Ichimoku e momentum. | Candles de 1 hora |
| `Macro Candle Type` | Período superior usado para o filtro de tendência MACD. | Candles de 30 dias |
| `Fast LWMA` | Período para a média móvel linealmente ponderada rápida. | 6 |
| `Slow LWMA` | Período para a média móvel linealmente ponderada lenta. | 85 |
| `Tenkan Period` | Período do Ichimoku Tenkan-sen. | 9 |
| `Kijun Period` | Período do Ichimoku Kijun-sen. | 26 |
| `Span B Period` | Período do Ichimoku Senkou Span B. | 52 |
| `Momentum Period` | Lookback para a relação de momentum de fechamento para fechamento. | 14 |
| `Momentum Threshold` | Desvio absoluto mínimo de 100 exigido pela relação de momentum. | 0.3 |
| `Take Profit (pips)` | Distância do take-profit expressa em pips. | 50 |
| `Stop Loss (pips)` | Distância do stop-loss expressa em pips. | 20 |

O parâmetro base `Volume` controla o tamanho das novas ordens. Quando um sinal de reversão aparece, a estratégia fecha a posição atual (se houver) e abre uma nova posição na direção oposta usando contratos `Volume + |Position|`.

## Regras de trading

### Entradas compradas
- LWMA rápida > LWMA lenta.
- Linha principal MACD > linha de sinal MACD no período macro.
- Desvio da relação de momentum ≥ limiar.
- A mínima do candle anterior tocou pelo menos um nível de Ichimoku e o candle atual abriu de volta acima desse nível.
- A posição líquida deve ser plana ou vendida.

### Entradas vendidas
- LWMA rápida < LWMA lenta.
- Linha principal MACD < linha de sinal MACD no período macro.
- Desvio da relação de momentum ≥ limiar.
- A máxima do candle anterior tocou pelo menos um nível de Ichimoku e o candle atual abriu de volta abaixo desse nível.
- A posição líquida deve ser plana ou comprada.

### Saídas
- Uma posição comprada fecha quando a mínima do candle atinge o stop-loss ou a máxima atinge o nível de take-profit.
- Uma posição vendida fecha quando a máxima do candle atinge o stop-loss ou a mínima atinge o nível de take-profit.

## Diferenças vs. EA original

- As escalas de gestão de dinheiro, movimentos de break-even e recursos de trailing da versão MQL não são replicados; o controle de risco é limitado a níveis fixos de stop-loss e take-profit.
- O StockSharp trabalha com uma única posição líquida, então a pilha de ordens martingale é substituída por uma entrada por direção.
- Alertas, e-mail e notificações push do ambiente MetaTrader são omitidos.

## Notas de uso

1. Adicionar a estratégia a um projeto StockSharp Designer ou Shell.
2. Selecionar o instrumento desejado e ajustar o `Signal Candle Type` para corresponder ao período alvo.
3. Garantir que o `Macro Candle Type` possa ser sintetizado a partir dos dados disponíveis (a assinatura usa `allowBuildFromSmallerTimeFrame`).
4. Ajustar stop-loss, take-profit e o limiar de momentum de acordo com a volatilidade do instrumento.

Os comentários incluídos descrevem cada etapa de decisão para que a lógica possa ser facilmente adaptada ou estendida.
