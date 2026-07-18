# Exemplo de Estratégia MACD Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia replica o consultor especialista "Example of MACD Automated" do MetaTrader 4 usando a API de alto nível do StockSharp. Ela monitora a linha principal do MACD em dois períodos de tempo e abre uma única posição quando ambos os filtros de tendência concordam. As distâncias de stop-loss e take-profit são aplicadas em passos de preço, e o tamanho da posição segue a lógica original do AdvancedMM que acumula o volume dos trades perdedores recentes.

## Lógica de trading

1. **Filtro de período de tempo superior** – um MACD (12, 26, 9) calculado no período de tempo superior (padrão: velas diárias) deve ter uma linha principal positiva para sinais de compra ou negativa para sinais de venda.
2. **Confirmação do período de tempo de entrada** – as mesmas configurações de MACD no período de tempo de entrada (padrão: velas de 15 minutos) devem apontar na mesma direção que o filtro de período de tempo superior.
3. **Posição única** – a estratégia opera uma posição de cada vez. Novas entradas são ignoradas até que a posição existente seja fechada pelos níveis protetores.
4. **Ordens protetoras** – os níveis de stop-loss e take-profit são medidos em múltiplos do passo de preço do instrumento, espelhando as entradas `StopLoss` e `TakeProfit` do MT4 original. Um valor de `0` desativa a proteção correspondente.
5. **Gestão monetária avançada** – o volume da operação aumenta após trades perdedores consecutivos somando o tamanho dos lotes das perdas, e reverte ao volume base após trades lucrativos, emulando a função `AdvancedMM()` do EA fonte.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `BaseVolume` | Volume base da ordem usado pela lógica do AdvancedMM. | `0.01` |
| `StopLossPoints` | Distância do stop-loss expressa em passos de preço. `0` desativa o stop. | `50` |
| `TakeProfitPoints` | Distância do take-profit expressa em passos de preço. `0` desativa o alvo. | `30` |
| `MacdFastLength` | Período da EMA rápida do MACD em ambos os períodos de tempo. | `12` |
| `MacdSlowLength` | Período da EMA lenta do MACD. | `26` |
| `MacdSignalLength` | Período da EMA da linha de sinal. | `9` |
| `EntryCandleType` | Período de tempo para execução de operações. | Velas de `15m` |
| `FilterCandleType` | Período de tempo superior usado como filtro de tendência. | Velas de `1d` |

## Gerenciamento de posição

- Os preços de stop-loss e take-profit são recalculados em cada nova posição com base no passo de preço do instrumento.
- Quando qualquer nível protetor é tocado dentro de uma barra, a estratégia assume que a ordem é executada naquele nível e registra o lucro ou prejuízo realizado.
- Após cada trade fechado, a lógica do AdvancedMM atualiza o próximo tamanho de ordem:
  - Menos de dois trades históricos → usar o volume base.
  - O trade mais recente foi uma perda → repetir seu volume.
  - Perdas consecutivas antes do último ganho → somar seus volumes para recuperar.
  - Caso contrário → reverter ao volume base.

## Notas

- A conversão mantém o comportamento original de manter uma posição até que um nível protetor seja atingido; não há saída em cruzamentos do MACD.
- Certifique-se de que o instrumento tenha informações válidas de `PriceStep` para que as distâncias de stop e alvo baseadas em pontos sejam calculadas corretamente.
- A estratégia depende de velas concluídas e deve ser usada com dados históricos ou feeds ao vivo que forneçam atualizações de velas terminadas.
