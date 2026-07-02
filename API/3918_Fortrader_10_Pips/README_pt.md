# Estratégia Fortrader 10 Pips
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Fortrader 10 Pips** é uma versão StockSharp do MetaTrader 4 consultor especialista `10pips.mq4` (ID de estratégia 8074). O robô mantém simultaneamente uma posição longa e uma posição curta abertas. Cada perna usa distâncias fixas de take-profit, stop-loss e trailing-stop medidas em pontos de símbolo.

Esta conversão recria o comportamento de hedge dentro do API de alto nível de StockSharp. Imediatamente após o início da estratégia, ela envia uma ordem de compra e venda a mercado. Sempre que uma ordem de proteção fecha uma perna, a estratégia abre instantaneamente uma nova ordem na mesma direção, mantendo sempre vivas duas posições opostas.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Take Profit Buy` | Distância de take-profit para a perna longa, em pontos. |
| `Stop Loss Buy` | Distância stop-loss para a perna longa, em pontos. |
| `Trailing Stop Buy` | Distância do trailing-stop para a perna longa, em pontos. Defina como zero para desativar o rastreamento. |
| `Take Profit Sell` | Distância de take-profit para a perna curta, em pontos. |
| `Stop Loss Sell` | Distância stop-loss para a perna curta, em pontos. |
| `Trailing Stop Sell` | Distância do trailing-stop para a perna curta, em pontos. Defina como zero para desativar o rastreamento. |
| `Volume` | Volume de cada ordem de mercado em lotes. |

Todas as distâncias são multiplicadas pelo `PriceStep` do instrumento para converter de pontos em valores de preço absolutos. Cada parâmetro é exposto por meio de `StrategyParam<T>` para que a estratégia possa ser ajustada ou otimizada por meio da GUI.

## Lógica de negociação
1. **Startup** – `OnStarted` assina dados de Nível 1 para rastrear os melhores preços de compra e venda atuais. A estratégia envia imediatamente uma ordem de compra a mercado e uma ordem de venda a mercado.
2. **Ordens de proteção** – Após cada preenchimento de entrada (`OnNewMyTrade`) a estratégia cria as ordens de stop-loss e take-profit associadas se as distâncias forem maiores que zero. Os pedidos são arredondados para a etapa de preço mais próxima.
3. **Reentrada** – Quando uma ordem stop-loss ou take-profit é executada, a perna fechada é reaberta instantaneamente com uma nova ordem de mercado para que a exposição bidirecional persista.
4. **Trailing stops** – As atualizações de nível 1 acionam `UpdateTrailingStops`, que ajusta as ordens de stop-loss sempre que o lance/ask atual ultrapassa a distância final configurada do preço de entrada. A lógica reflete o original EA: o trailing começa quando o lucro excede a distância do trailing e as paradas são movidas apenas na direção do lucro.

## Notas de implementação
- O código MT4 original esperou 10 segundos entre as ordens iniciais de compra e venda. StockSharp não requer esse atraso, portanto ambos os pedidos são enviados imediatamente.
- Como StockSharp usa posições líquidas por padrão, o verdadeiro hedge pode depender do corretor/conector que suporta posições opostas. A estratégia monitora cada etapa de forma independente e as restabelece após cada saída.
- `StartProtection()` é chamado uma vez durante `OnStarted` para que as proteções globais contra riscos estejam ativas se configuradas nas configurações da estrutura.

## Dicas de uso
- Certifique-se de que o conector selecionado suporta posições longas e curtas simultâneas se o comportamento de hedge for necessário.
- Defina as distâncias finais como zero para desativar o rastreamento da perna correspondente.
- Otimize os parâmetros de risco (`Take Profit`, `Stop Loss`, `Trailing Stop`) em dados históricos para ajustar o símbolo negociado e o período de tempo.
