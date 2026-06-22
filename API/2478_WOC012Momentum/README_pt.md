# Estratégia de Momentum WOC 0.1.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp de alto nível do Expert Advisor do MetaTrader "WOC.0.1.2". Ela ouve atualizações de melhor bid/ask de Nível 1 e procura por séries rápidas de preço no lado do ask. Quando o preço do ask imprime um número configurável de ticks consecutivamente mais altos ou mais baixos dentro de uma janela de tempo limitada, a estratégia abre uma posição de mercado na direção do rompimento. Apenas uma posição pode estar aberta a qualquer momento, o que espelha o comportamento de posição única do código original.

## Dados e execução
- **Dados de mercado**: Melhor bid e melhor ask de Nível 1. O algoritmo não requer velas ou indicadores.
- **Execução**: Ordens a mercado. As saídas de proteção são emuladas dentro da estratégia verificando as atualizações de bid/ask.

## Lógica de sinal
1. Rastrear o último preço do ask e medir quantos novos máximos consecutivos (série de alta) ou novos mínimos (série de baixa) foram impressos.
2. Quando uma série de alta ou baixa atinge `SequenceLength`, verificar se a duração da série é menor ou igual a `SequenceTimeoutSeconds` segundos.
3. Se a série de baixa for mais longa que a de alta, enviar uma ordem de venda; caso contrário, enviar uma ordem de compra. A verificação reproduz a lógica original do MetaTrader onde a série com o contador mais alto define a direção.
4. Redefinir todos os contadores de série após cada tentativa de entrada para garantir que o próximo sinal comece do zero.

## Gerenciamento de posição
- **Stop inicial**: Após uma entrada, a estratégia registra imediatamente um preço de stop-loss que está `StopLossTicks` passos de preço afastado do bid atual (para comprados) ou do ask (para vendidos).
- **Stop móvel**: Quando o preço se move a favor do trade mais de `TrailingStopTicks` passos de preço, o stop é ajustado para `TrailingStopTicks` atrás do último bid/ask, desde que o stop permaneça pelo menos o dobro da distância de trailing afastado do preço atual. Isso reproduz a condição de trailing de dois passos do Expert MQL.
- **Execução de saída**: Quando o bid/ask rastreado cruza o nível de stop armazenado, a posição é fechada via uma ordem a mercado. Após a saída, o estado interno é redefinido para aceitar novas séries.

## Gerenciamento de volume
Dois modos de dimensionamento de posição são suportados:
- **Lote fixo**: Usar o parâmetro `LotSize` como volume de ordem absoluto.
- **Lotes automáticos**: Habilitar `UseAutoLotSizing` para mapear o saldo da conta para níveis de volume. O saldo é obtido de `Portfolio.CurrentValue` e recorre a `Portfolio.BeginValue` se o valor atual não estiver disponível.

| Saldo (maior que) | Volume |
| ------------------- | ------ |
| 0 (padrão)          | `LotSize`
| 200                 | 0.04
| 300                 | 0.05
| 400                 | 0.06
| 500                 | 0.07
| 600                 | 0.08
| 700                 | 0.09
| 800                 | 0.10
| 900                 | 0.20
| 1 000               | 0.30
| 2 000               | 0.40
| 3 000               | 0.50
| 4 000               | 0.60
| 5 000               | 0.70
| 6 000               | 0.80
| 7 000               | 0.90
| 8 000               | 1.00
| 9 000               | 2.00
| 10 000              | 3.00
| 11 000              | 4.00
| 12 000              | 5.00
| 13 000              | 6.00
| 14 000              | 7.00
| 15 000              | 8.00
| 20 000              | 9.00
| 30 000              | 10.00
| 40 000              | 11.00
| 50 000              | 12.00
| 60 000              | 13.00
| 70 000              | 14.00
| 80 000              | 15.00
| 90 000              | 16.00
| 100 000             | 17.00
| 110 000             | 18.00
| 120 000             | 19.00
| 130 000             | 20.00

## Parâmetros
- `StopLossTicks` – distância do stop-loss medida em passos de preço.
- `TrailingStopTicks` – distância de trailing medida em passos de preço (pode ser zero para desabilitar o trailing).
- `SequenceLength` – número de movimentos consecutivos do ask necessários antes de entrar em um trade.
- `SequenceTimeoutSeconds` – duração máxima da série em segundos.
- `LotSize` – tamanho de ordem fixo usado quando o dimensionamento automático está desabilitado.
- `UseAutoLotSizing` – habilita a tabela de volume baseada em saldo mostrada acima.

## Notas de uso
- Funciona melhor em instrumentos rápidos onde o melhor ask se atualiza frequentemente; considere testar em feeds de dados em nível de tick.
- A estratégia requer contas de hedge porque nunca mantém posições opostas simultaneamente.
- Certifique-se de que `Security.PriceStep` está configurado; caso contrário, os cálculos de stop-loss e trailing recorrem a uma distância de 1 unidade monetária por tick.
- Apenas uma posição aberta é suportada por vez, espelhando o comportamento MQL original.
