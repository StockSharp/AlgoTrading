# Estratégia Vector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Vector é um sistema de seguidor de tendência multi-moeda convertido do expert "Vector" do MetaTrader 5. Opera quatro principais pares forex — EURUSD, GBPUSD, USDCHF e USDJPY — simultaneamente. A estratégia calcula médias móveis suavizadas sobre o preço mediano de cada par e abre posições sincronizadas quando a tendência combinada aponta na mesma direção. Um alvo de pips dinâmico baseado na volatilidade de quatro horas e limiares de lucro e perda em nível de portfólio controlam as saídas.

## Ideias centrais
- Usar médias móveis suavizadas (SMMA) construídas sobre preços medianos para medir a direção em cada par de moedas.
- Resumir as médias rápidas e lentas de todos os instrumentos para determinar um viés altista ou baixista comum.
- Inserir uma única ordem a mercado por par quando o viés global e o cruzamento rápido/lento local coincidem.
- Gerenciar posições com um alvo de pips flutuante derivado do alcance médio de 50 candles de 4 horas concluídos no EURUSD.
- Fechar todas as operações simultaneamente se o lucro ou perda flutuante atingir a porcentagem configurada do saldo inicial.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| **Fast MA** | Comprimento da média móvel suavizada usada para a tendência rápida em cada par. |
| **Slow MA** | Comprimento da média móvel suavizada usada para a tendência lenta em cada par. |
| **MA Shift** | Número adicional de candles finalizados exigidos antes de avaliar os sinais, espelhando a configuração de deslocamento no EA original. |
| **Equity Take Profit %** | Porcentagem de lucro flutuante que aciona o fechamento de todas as posições abertas. |
| **Equity Stop Loss %** | Porcentagem de perda flutuante que aciona uma saída de emergência para todas as operações. |
| **Signal Timeframe** | Período de candles usado para as médias móveis suavizadas (padrão 15 minutos). |
| **Range Timeframe** | Período de candles usado para a média de volatilidade (padrão 4 horas). |
| **Range Period** | Número de candles de período superior usados para calcular o alvo médio de pips. |
| **EURUSD / GBPUSD / USDCHF / USDJPY** | Ativos que correspondem a cada instrumento negociado. |

Todos os parâmetros suportam intervalos de otimização idênticos ao expert advisor original onde aplicável.

## Lógica de negociação
1. **Atualização do indicador** — Cada candle finalizado em um período de negociação atualiza as médias móveis suavizadas rápida e lenta para o par correspondente. Os valores só são considerados após a conclusão do aquecimento configurado (MA Shift).
2. **Cálculo do viés** — A estratégia soma as últimas médias rápidas de todos os pares e subtrai a soma das médias lentas. Um resultado positivo indica pressão altista, enquanto um negativo indica pressão baixista.
3. **Condições de entrada** — Quando não existe posição para um par, a estratégia entra com uma ordem de compra se o viés global for altista e a média rápida do par estiver acima da lenta. Abre uma ordem de venda no caso contrário.
4. **Saída por alvo de pips** — A assinatura de quatro horas do EURUSD calcula o alcance médio de candles ao longo do período configurado. O alvo de pips atual é o maior entre esta média e 13 pips. Comprados fecham assim que o preço ganha pelo menos o número alvo de pips, e vendidos fecham após um movimento favorável equivalente.
5. **Proteção de capital** — Sempre que o lucro flutuante exceder a porcentagem de take-profit, ou a perda flutuante ultrapassar a porcentagem de stop-loss, a estratégia fecha imediatamente todas as posições gerenciadas.

## Notas de uso
- Anexe a estratégia a um portfólio que forneça acesso a todos os quatro instrumentos forex e defina cada parâmetro de ativo explicitamente.
- O período de sinal padrão é 15 minutos; certifique-se de que candles correspondentes estejam disponíveis para cada par de moedas.
- Apenas uma posição aberta por par é mantida a qualquer momento. O parâmetro de volume da estratégia base é usado para cada entrada.
- Como as saídas dependem do L/P flutuante, a estratégia é destinada à operação contínua em vez de apenas backtesting barra a barra.
- O alvo de pips dinâmico usa a volatilidade do EURUSD em linha com a implementação original. Ajuste o período de range ou o período se preferir adaptar o alvo a um ambiente de mercado diferente.
