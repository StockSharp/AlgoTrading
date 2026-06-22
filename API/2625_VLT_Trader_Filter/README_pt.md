# Estratégia VLT Trader com Filtro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia VLT Trader com Filtro** é um sistema de rompimento por contração de volatilidade convertido da implementação MQL original. Monitora intervalos de candles recentes e prepara ordens stop sempre que o candle mais recente completado se tornar o menor intervalo dentro de uma janela histórica configurável. O objetivo é capturar movimentos explosivos após um período de consolidação estreita.

## Lógica de negociação

1. **Processamento de nova barra** – a estratégia avalia condições apenas uma vez por novo candle. O candle atual deve abrir abaixo da máxima do candle anterior para evitar negociar gaps que saltam pelo nível de rompimento.
2. **Filtro de volatilidade** – o intervalo do candle finalizado mais recente é comparado com o menor intervalo entre os últimos `CandleCount` candles finalizados cujo intervalo está abaixo de `MaxCandleSizePips`. Se o candle mais recente for estritamente menor, a configuração é válida.
3. **Colocação de entradas** – quando a configuração é válida, duas ordens stop são preparadas:
   - Um **buy stop** `10` pips acima da máxima anterior quando a posição líquida não é comprada.
   - Um **sell stop** `10` pips abaixo da mínima anterior quando a posição líquida não é vendida.
   Ordens pendentes existentes do mesmo tipo são canceladas antes de registrar novas.
4. **Gestão de risco** – uma vez que uma ordem stop é acionada e abre uma posição, ordens de proteção são anexadas automaticamente:
   - Take-profit em `TakeProfitPips` acima/abaixo do preço de entrada.
   - Stop-loss em `StopLossPips` abaixo/acima do preço de entrada.
   Ordens de proteção são canceladas quando a posição retorna a zero.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume da ordem enviado com cada ordem stop. |
| `TakeProfitPips` | Distância em pips usada para a ordem take-profit após a entrada. |
| `StopLossPips` | Distância em pips usada para o stop de proteção após a entrada. |
| `MaxCandleSizePips` | Limite superior para os intervalos de candles históricos considerados no filtro de volatilidade. |
| `CandleCount` | Número de candles históricos usados para encontrar o intervalo mínimo aceitável. |
| `CandleType` | Período de candles usado para a análise. |

## Notas de implementação

- O tamanho do pip é derivado do passo de preço do instrumento. Quando o passo é menor ou igual a `0.001`, é multiplicado por `10` para emular a definição de pip do MetaTrader para instrumentos de 3 ou 5 decimais.
- Os intervalos de candles são armazenados em uma fila FIFO limitada a `CandleCount` elementos, correspondendo ao escaneamento histórico realizado no Expert Advisor original.
- Todas as ordens são criadas através da API de alto nível do StockSharp (sem registro manual de ordens) e são canceladas automaticamente quando ficam desatualizadas ou quando a posição fecha.
- Os comentários dentro do código estão escritos em inglês, enquanto os arquivos README fornecem documentação multilíngue detalhada.
