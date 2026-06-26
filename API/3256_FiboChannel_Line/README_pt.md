# Estratégia de FiboChannel Line
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia FiboChannel Line** é uma conversão do consultor especialista MetaTrader "FIBOCHANNEL". O robô original dependia da direção de um canal de Fibonacci desenhado manualmente, oscilações de Momentum em um período superior e uma combinação de médias móvias ponderadas linearmente e sinais MACD. O porte StockSharp mantém o mesmo espírito aproveitando vínculos de indicadores de alto nível e gestão de risco integrada.

Ideias-chave:

- Seguir a tendência dominante usando um par de médias móvias ponderadas linearmente (LWMA).
- Confirmar picos de Momentum ao redor do nível neutro do oscilador Momentum.
- Filtrar trades com a relação linha MACD vs. linha de sinal.
- Verificar a inclinação de um canal de regressão linear em vez de ler objetos do gráfico.
- Gerenciar posições via proteção percentual automática.

A estratégia funciona em qualquer instrumento que suporte agregação de candles. O período padrão são candles de 30 minutos, que fornecem um equilíbrio entre responsividade e estabilidade do indicador.

## Lógica de trading
1. **Filtro de tendência** – quando a LWMA rápida fecha acima da LWMA lenta, o mercado é considerado altista e apenas trades longos são avaliados. Quando está abaixo, apenas os curtos são considerados.
2. **Requisito de Momentum** – uma janela deslizante das três leituras de Momentum mais recentes deve mostrar que pelo menos um valor se desviou do nível neutro 100 pelo limiar configurado. Isso replica as verificações de força de Momentum de múltiplas barras da versão MQL.
3. **Filtro MACD** – trades longos requerem que a linha MACD esteja acima da linha de sinal; trades curtos requerem o oposto.
4. **Direção do canal** – a inclinação da regressão linear deve ser positiva (para longos) ou negativa (para curtos) além do `Slope Threshold`. Isso imita a validação de canal ascendente/descendente do especialista original.
5. **Entradas e reversões** – se todas as condições se alinham e não há posição existente nessa direção, a estratégia cancela ordens ativas e envia uma ordem de mercado com tamanho `Volume + |Position|`. Isso permite reversões suaves.
6. **Saídas** – se a direção do canal ou o filtro MACD parar de suportar o trade aberto, a posição é fechada após cancelar ordens pendentes. Adicionalmente, as regras de stop-loss protetor, take-profit e drawdown máximo são configuradas através de `StartProtection`.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Candle Type` | Agregação de candles usada para todos os indicadores. | Período de 30 minutos |
| `Fast LWMA` | Comprimento da média móvel ponderada linearmente rápida. | 6 |
| `Slow LWMA` | Comprimento da média móvel ponderada linearmente lenta. | 85 |
| `Momentum Period` | Número de barras para o indicador Momentum. | 14 |
| `Momentum Threshold` | Desvio absoluto mínimo de 100 necessário dentro do buffer de Momentum. | 0.3 |
| `Channel Length` | Barras usadas para calcular a inclinação da regressão linear. | 50 |
| `Slope Threshold` | Valor mínimo de inclinação absoluta para confirmar a direção da tendência. | 0.0 |
| `MACD Fast` | Período EMA rápida dentro do cálculo MACD. | 12 |
| `MACD Slow` | Período EMA lenta dentro do cálculo MACD. | 26 |
| `MACD Signal` | Período da linha de sinal do MACD. | 9 |
| `Take Profit %` | Distância do take-profit protetor em percentual. | 2 |
| `Stop Loss %` | Distância do stop-loss protetor em percentual. | 1 |
| `Equity Risk %` | Drawdown máximo de capital da conta permitido antes de fechar todas as posições. | 3 |

Todos os parâmetros numéricos expõem dicas de otimização que refletem os intervalos típicos das entradas MQL.

## Gestão de risco
`StartProtection` é configurado para aplicar:

- Stop-loss e take-profit baseados em percentual relativos ao preço de entrada.
- Proteção de drawdown de capital que fecha a estratégia se a perda exceder o percentual configurado.

Essas proteções substituem as numerosas rotinas de balanço, trailing e ponto de equilíbrio do especialista original, fornecendo um comportamento mais claro e seguro dentro do StockSharp.

## Diferenças da versão MetaTrader
- As leituras de objetos do gráfico foram substituídas por um filtro de inclinação de regressão porque as estratégias StockSharp não interagem com canais de Fibonacci manuais.
- Em vez de uma mistura de lógica de trailing baseada em dinheiro, a estratégia depende de `StartProtection`.
- O conjunto de indicadores permanece o mesmo (LWMA, Momentum, MACD), mas é implementado usando vínculos de alto nível e sem sondagem direta de valores de indicadores.
- Alertas, e-mails e notificações push foram removidos, pois o ambiente StockSharp já fornece logging consolidado.

## Notas de uso
1. Anexe a estratégia a um portfólio e ativo, configure o tamanho do lote através da propriedade `Volume` e ajuste os parâmetros conforme necessário.
2. Certifique-se de que os dados históricos estejam disponíveis para o tipo de candle selecionado para que o buffer de Momentum e o indicador de inclinação possam se formar corretamente.
3. Execute primeiro em paper trading para ajustar o limiar de Momentum e os parâmetros de risco de acordo com a volatilidade do instrumento negociado.
